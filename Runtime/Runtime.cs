﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rosi.Compiler;
using Rosi.Core;
using Rosi.Scriban;

namespace Rosi
{
    public class Runtime : IRuntime, ILogger
    {
        public static Version RuntimeVersion => typeof(Runtime).Assembly.GetName().Version;
        public static Version CSScriptVersion => typeof(CSScriptLib.CSScript).Assembly.GetName().Version;
        public static Version ScribanVersion => typeof(global::Scriban.Template).Assembly.GetName().Version;
        public static Version NetCoreVersion => Environment.Version;

        readonly Type _debugMainType;
        readonly FileInfo _mainScript;
        ScribanRuntime _scriban;
        readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public DirectoryInfo RootPath { get; private set; }
        public Config Config { get; private set; }
        public Compiler.Compiler Compiler { get; private set; }

        internal bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal bool IsMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        internal bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public bool Debugging => Debugger.IsAttached && _debugMainType != null;

        public ScribanRuntime Scriban
        {
            get
            {
                if (_scriban == null)
                    _scriban = new ScribanRuntime(Path.Combine(RootPath.FullName, Config.ScribanScriptPath), true);

                return _scriban;
            }
        }

        static Runtime()
        {
            // force inclusion of bundled assemblies
            typeof(YamlDotNet.Core.Events.MappingStart).GetType();
            typeof(System.Net.IPNetwork).GetType();
            typeof(Newtonsoft.Json.ConstructorHandling).GetType();

            Tr.Init(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        }

        public Runtime(Type debugMainType = null, params string[] args)
        {
            _debugMainType = debugMainType;

            Config = new Config(args);

            if (args.Length == 0)
            {
                if (Debugging)
                {
                    Log.Warn(Tr.Get("Runtime.DebugNoMainScript"));
                }
                else
                {
                    Log.Fatal(Tr.Get("Runtime.NoArgs"), this);
                    return;
                }
            }
            else
            {
                try
                {
                    _mainScript = new FileInfo(args[0]);
                } catch { }

                if (_mainScript != null && _mainScript.Exists)
                {
                    RootPath = _mainScript.Directory;
                    Directory.SetCurrentDirectory(RootPath.FullName);

                    ScriptParser.ParseOptions(_mainScript, this);
                }
                else
                { 
                    if (Debugging)
                    {
                        Log.Warn(Tr.Get("Runtime.DebugNoMainScript"));
                    }
                    else
                    {
                        Log.Fatal(Tr.Get("Runtime.MainScriptMissing", args[0]), this);
                        return;
                    }
                }
            }

            if (Config.LogToFile && Log.SetLogFile(new FileInfo(Config.LogFilename), Config.LogAppend))
                Log.ShowConsoleOutput = Config.LogToConsole;

            if (Enum.TryParse(Config.ConsoleLogLevel, true, out LogLevels logLevel))
                Log.ConsoleLogLevel = logLevel;
            else
                Log.Warn(Tr.Get("Runtime.UnknownLogLevel", Config.ConsoleLogLevel));

            if (Enum.TryParse(Config.FileLogLevel, true, out logLevel))
                Log.FileLogLevel = logLevel;
            else
                Log.Warn(Tr.Get("Runtime.UnknownLogLevel", Config.FileLogLevel));

            Tr.LoadFiles(new DirectoryInfo(Config.TranslationPath));
            Compiler = new Compiler.Compiler(this);
        }

        async ValueTask DisposeRosi(object rosi)
        {
            if (rosi is IDisposable disposable)
                disposable.Dispose();
            if (rosi is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
        }

        public async Task<int> RunAsync()
        {
            if (Compiler == null)
                return -1;

            try
            {
                if(Debugging)
                {
                    var @class = Activator.CreateInstance(_debugMainType);
                    if (@class is IAsyncRosi asyncRosi)
                    {
                        var result = await asyncRosi.Run(this);
                        await DisposeRosi(asyncRosi);
                        return result;
                    }
                    else if (@class is IRosi rosi)
                    {
                        var result = rosi.Run(this);
                        await DisposeRosi(rosi);
                        return result;
                    }
                }
                else
                {
                    var compilerResult = await Compiler.Compile(_mainScript.Name, File.ReadAllText(_mainScript.FullName));
                    if (compilerResult.Result != CompilerResultType.Ok)
                        return -1;

                    var assembly = compilerResult?.Assemby;
                    if (assembly != null)
                    {
                        Type[] types = null;
                        try
                        {
                            types = assembly.GetTypes();
                        }
                        catch(ReflectionTypeLoadException ex)
                        {
                            types = ex.Types;
                            Log.HandleException(ex);
                        }

                        foreach (var type in types)
                        {
                            if (typeof(IAsyncRosi).IsAssignableFrom(type))
                            {
                                var asyncRosi = (IAsyncRosi)assembly.CreateInstance(type.FullName);
                                if (asyncRosi != null)
                                {
                                    var result = await asyncRosi.Run(this);
                                    await DisposeRosi(asyncRosi);
                                    return result;
                                }
                            }
                            else if (typeof(IRosi).IsAssignableFrom(type))
                            {
                                var rosi = (IRosi)assembly.CreateInstance(type.FullName);
                                if (rosi != null)
                                {
                                    var result = rosi.Run(this);
                                    await DisposeRosi(rosi);
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(Tr.Get("Runtime.RuntimeError", ex), this);
                return -1;
            }

            Log.Fatal(Tr.Get("Runtime.NoMainFound", _mainScript.Name), this);
            return -1;
        }

        T IRuntime.GetValue<T>(string name)
        {
            if(_values.TryGetValue(name, out var value))
                return (T)value;

            return default;
        }

        bool IRuntime.TryGetValue<T>(string name, out T value)
        {
            if (_values.TryGetValue(name, out var stored))
            {
                value = (T)stored;
                return true;
            }

            value = default;
            return false;
        }

        void IRuntime.SetValue(string name, object value)
        {
            SetValue(name, value);
        }

        public Runtime SetValue(string name, object value)
        {
            _values[name] = value;
            return this;
        }

        public Runtime SetConfigValue(string key, string value)
        {
            Config[key] = value;
            return this;
        }
    }
}
