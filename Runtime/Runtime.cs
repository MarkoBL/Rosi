using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        public bool Debugging => Debugger.IsAttached && _debugMainType != null;

        public ScribanRuntime Scriban
        {
            get
            {
                if (_scriban == null)
                    _scriban = new ScribanRuntime(new DirectoryInfo(Path.Combine(RootPath.FullName, Config.ScribanScriptPath)));

                return _scriban;
            }
        }

        static Runtime()
        {
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

            Tr.LoadFiles(new DirectoryInfo(Config.TranslationPath));
            Compiler = new Compiler.Compiler(this);
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
                    if (@class is IAsyncRosi asyncMain)
                        return await asyncMain.Run(this);
                    else if (@class is IRosi rosi)
                        return rosi.Run(this);
                }
                else
                {
                    var result = await Compiler.Compile(_mainScript.Name, File.ReadAllText(_mainScript.FullName));
                    if (result.Result != CompilerResultType.Ok)
                        return -1;

                    var assembly = result?.Assemby;
                    if (assembly != null)
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (typeof(IAsyncRosi).IsAssignableFrom(type))
                            {
                                var asyncMain = (IAsyncRosi)assembly.CreateInstance(type.FullName);
                                if (asyncMain != null)
                                    return await asyncMain.Run(this);
                            }
                            else if (typeof(IRosi).IsAssignableFrom(type))
                            {
                                var main = (IRosi)assembly.CreateInstance(type.FullName);
                                if (main != null)
                                    return main.Run(this);
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
