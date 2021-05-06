using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSScriptLib;
using Rosi.Core;

namespace Rosi.Compiler
{
    public sealed class Compiler : ILogger
    {
        readonly Runtime _runtime;

        readonly DirectoryInfo _assemblyDirectory;
        readonly HashSet<string> _loadedAssemblies = new HashSet<string>();

        readonly Dictionary<string, CompilerResult> _results = new Dictionary<string, CompilerResult>();

        internal readonly IEvaluator Evaluator;

        internal Compiler(Runtime runtime)
        {
            _runtime = runtime;
            _assemblyDirectory = new DirectoryInfo(_runtime.Config.AssemblyPath);

            CSScript.EvaluatorConfig.DebugBuild = true;
            CSScript.EvaluatorConfig.RefernceDomainAsemblies = !_runtime.Debugging;
            CSScript.GlobalSettings.AddSearchDir(_assemblyDirectory.FullName);

            Evaluator = CSScript.Evaluator;

            Evaluator.ReferenceAssembly(GetType().Assembly);
            Evaluator.ReferenceAssembly(typeof(System.Net.IPNetwork).Assembly);

            Evaluator.DisableReferencingFromCode = true;
        }

        public async Task<CompilerResult> Compile(string name, string content)
        {
            var result = new CompilerResult(name, null);
            await Compile(name, content, result, true);
            return result;
        }

        public void ReferenceAssembly(Assembly assembly)
        {
            Evaluator.ReferenceAssembly(assembly);
        }

        async Task<bool> Compile(string name, string content, CompilerResult parentResult, bool isRootScript)
        {
            var result = isRootScript ? parentResult : new CompilerResult(name, parentResult);
            if (!isRootScript)
                parentResult._more.Add(result);

            var rootClass = result.RootClass;

            var parsedScript = new ScriptParser(_runtime, name, content);
            result.ParsedScript = parsedScript;

            if (_results.ContainsKey(rootClass))
            {
                var error = Tr.Get("Compiler.AlreadyCompiled", name);
                Log.Error(error, this);

                result.SetError(CompilerResultType.AlreadyCompiled, error);
                return false;
            }

            _results.Add(rootClass, result);

            foreach (var assemblyName in parsedScript.AssemblyFiles)
            {
                var ass = assemblyName;
                // ignore during debugging
                if (ass.StartsWith('!'))
                {
                    if (_runtime.Debugging)
                        continue;

                    ass = assemblyName.Substring(1);
                }

                if (_loadedAssemblies.Contains(ass))
                    continue;

                try
                {
                    Evaluator.ReferenceAssembly(Path.Combine(_assemblyDirectory.FullName, ass));
                    //Assembly.LoadFrom(Path.Combine(_assemblyDirectory.FullName, ass));
                    _loadedAssemblies.Add(ass);
                }
                catch(Exception ex)
                {
                    var error = Tr.Get("Compiler.AssemblyError", ass, ex.Message);
                    Log.Error(error, this);

                    result.SetError(CompilerResultType.AssemblyError, error);
                    return false;
                }
            }

            foreach (var file in parsedScript.CompileFiles)
            {
                var (fileInfo, error) = parsedScript.GetScriptFileInfo(file);
                if (fileInfo == null || !fileInfo.Exists)
                {
                    result.SetError(CompilerResultType.ParserError, error);
                    return false;
                }

                if (!await Compile(fileInfo.Name, File.ReadAllText(fileInfo.FullName), result, false))
                {
                    return false;
                }
            }

            var scriptBuilder = new StringBuilder();
            foreach (var item in _results.Values)
            {
                if (item.Assemby != null)
                    scriptBuilder.AppendLine($"using static {item.RootClass};");
            }

            scriptBuilder.AppendLine(parsedScript.Script);

            var script = scriptBuilder.ToString();
            if (string.IsNullOrWhiteSpace(script))
            {
                var error = Tr.Get("Compiler.ScriptEmpty", name);
                Log.Error(error, this);
                result.SetError(CompilerResultType.CompileError, error);
                return false;
            }

            var useCachedAssemblies = _runtime.Config.CacheAssemblies;

            var baseFileName = $"{(useCachedAssemblies ? Sha1.Compute(script).Replace("-", "") : "rosi")}.{rootClass}";
            var pdbFilePath = Path.Combine(Path.GetTempPath(), $"{baseFileName}.pdb");
            var assemblyFilPath = Path.Combine(Path.GetTempPath(), $"{baseFileName}.dll");

            var loadedFromFile = false;
            if(useCachedAssemblies && File.Exists(assemblyFilPath) && File.Exists(pdbFilePath))
            {
                try
                {
                    var assemblyData = await File.ReadAllBytesAsync(assemblyFilPath);
                    var pdbData = await File.ReadAllBytesAsync(pdbFilePath);

                    result.Assemby = AppDomain.CurrentDomain.Load(assemblyData, pdbData);
                    loadedFromFile = true;
                }
                catch { }
            }

            try
            {
                if (!loadedFromFile)
                {
                    result.Assemby = Evaluator.CompileCode(script, new CompileInfo { PreferLoadingFromFile = useCachedAssemblies, RootClass = rootClass, AssemblyFile = assemblyFilPath, PdbFile = pdbFilePath });

                    // required for debug information
                    var assemblyData = await File.ReadAllBytesAsync(assemblyFilPath);
                    var pdbData = await File.ReadAllBytesAsync(pdbFilePath);
                    result.Assemby = AppDomain.CurrentDomain.Load(assemblyData, pdbData);
                }
            }
            catch(Exception ex)
            {
                var error = Tr.Get("Compiler.Error", name, ex.Message);

                if (_runtime.Config.LogScript)
                    error += script + "\n";

                Log.Error(error, this);

                result.SetError(CompilerResultType.CompileError, error);
                return false;
            }

            result.FinalScript = script;

            var outputPath = _runtime.Config.ScriptOutputPath;
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                try
                {
                    var scriptPath = Path.Combine(outputPath, name);
                    if (!loadedFromFile || !File.Exists(scriptPath))
                    {
                        Directory.CreateDirectory(outputPath);
                        await File.WriteAllTextAsync(scriptPath, script);
                    }
                }
                catch
                { }
            }

            foreach (var file in parsedScript.PostCompileFiles)
            {
                var (fileInfo, error) = parsedScript.GetScriptFileInfo(file);
                if (!fileInfo.Exists)
                {
                    result.SetError(CompilerResultType.ParserError, error);
                    return false;
                }

                if (!await Compile(fileInfo.Name, File.ReadAllText(fileInfo.FullName), result, false))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
