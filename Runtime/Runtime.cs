using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rosi.Runtime.Compiler;
using Rosi.Runtime.Core;
using Rosi.Runtime.Scriban;

namespace Rosi.Runtime
{
    public class Runtime : IRuntime, ILogger
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);


        public static Version RuntimeVersion => typeof(Runtime).Assembly.GetName().Version;
        public static Version ScribanVersion => typeof(global::Scriban.Template).Assembly.GetName().Version;
        public static Version NetCoreVersion => Environment.Version;

        public static readonly Assembly MsCoreLib = 1961.GetType().Assembly;

        readonly Type _debugMainType;
        readonly FileInfo _mainScript;
        ScribanRuntime _scriban;
        readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        readonly List<DirectoryInfo> _assemblyDirectories = new List<DirectoryInfo>();
        readonly List<Assembly> _referencedAssemblies = new List<Assembly>();

        public DirectoryInfo RootPath { get; private set; }
        public Config Config { get; private set; }

        public bool Debugging => Debugger.IsAttached && _debugMainType != null;

        public IReadOnlyList<DirectoryInfo> AssemblyDirectories => _assemblyDirectories;
        public IReadOnlyList<Assembly> ReferencedAssemblies => _referencedAssemblies;

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
                    ScriptParser.ParseSetDirectives(_mainScript, (key, value) => Config[key] = value);
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

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolve;

            var pathList = Config.AssemblyPath.Split(',');
            foreach (var path in pathList)
            {
                var di = new DirectoryInfo(path.Trim());
                if(di.Exists)
                    _assemblyDirectories.Add(di);
            }
            _assemblyDirectories.Add(new DirectoryInfo(Path.GetDirectoryName(MsCoreLib.Location)));

            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in domainAssemblies)
            {
                if (assembly != MsCoreLib)
                    _referencedAssemblies.Add(assembly);
            }

            ReferenceAssembly(GetType().Assembly);
            ReferenceAssembly(typeof(System.Net.IPNetwork).Assembly);
            ReferenceAssembly(typeof(YamlDotNet.Core.IEmitter).Assembly);
        }

        Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (Config.AssemblyResolveInfo)
                Log.Warn(Tr.Get("Runtime.AssemblyResolve", args.Name, args.RequestingAssembly));

            try
            {
                var assemblyName = args.Name.Split(',')[0];
                foreach(var di in _assemblyDirectories)
                {
                    var assemblyPath = $"{di.FullName}/{assemblyName}.dll";
                    if(File.Exists(assemblyPath))
                        return Assembly.LoadFrom(assemblyPath);
                }

            }
            catch (Exception ex)
            {
                Log.Error(Tr.Get("Runtime.AssemblyResolveException", args.Name, args.RequestingAssembly, ex.Message));
                return null;
            }

            Log.Error(Tr.Get("Runtime.AssemblyResolveFailed", args.Name, args.RequestingAssembly));
            return null;
        }

        public void AddAssemblyDirectory(DirectoryInfo directoryInfo)
        {
            if(directoryInfo.Exists)
                _assemblyDirectories.Add(directoryInfo);
        }

        public void ReferenceAssembly(Assembly assembly)
        {
            if (!_referencedAssemblies.Contains(assembly))
            {
                _referencedAssemblies.Add(assembly);
                assembly.GetTypes();
            }
        }

        public async Task<int> RunAsync()
        {
            var compiler = new Compiler.Compiler(this);

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
                    var compilerOptions = new CompilerOptions
                    {
                        Debugging = Debugging,
                        ScriptPath = Config.ScriptPath,
                        ScriptNamespace = Config.ScriptNamespace,
                        UseCachedAssemblies = Config.UseCachedAssemblies,
                        ScriptOutputPath = Config.ScriptOutputPath,
                        LogScriptOnError = Config.LogScriptOnError,
                        SetDirective = (key, value) => Config[key] = value
                    };

                    var compilerResult = await CompileScript(_mainScript, compilerOptions);
                    if (compilerResult.Result != CompilerResultType.Ok)
                        return -1;

                    var assembly = compilerResult?.Assembly;
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

        async ValueTask DisposeRosi(object rosi)
        {
            if (rosi is IDisposable disposable)
                disposable.Dispose();
            if (rosi is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
        }

        public async Task<CompilerResult> CompileScript(FileInfo fileInfo, CompilerOptions compilerOptions)
        {
            var compiler = new Compiler.Compiler(this);
            return await compiler.Compile(fileInfo.Name, File.ReadAllText(fileInfo.FullName), compilerOptions);
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
