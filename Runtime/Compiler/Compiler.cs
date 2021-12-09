using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Rosi.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rosi.Compiler
{
    public sealed class Compiler : ILogger
    {
        readonly Runtime _runtime;

        readonly HashSet<string> _loadedAssemblies = new HashSet<string>();

        readonly Dictionary<string, CompilerResult> _results = new Dictionary<string, CompilerResult>();

        readonly List<Assembly> _referencedAssemblies = new List<Assembly>();



        internal Compiler(Runtime runtime)
        {
            _runtime = runtime;

            var mscorelib = 333.GetType().Assembly;
            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in domainAssemblies)
            {
                if(assembly != mscorelib)
                    _referencedAssemblies.Add(assembly);
            }

            ReferenceAssembly(GetType().Assembly);
            ReferenceAssembly(typeof(System.Net.IPNetwork).Assembly);
            ReferenceAssembly(typeof(YamlDotNet.Core.IEmitter).Assembly);
        }

        public async Task<CompilerResult> Compile(string name, string content)
        {
            var result = new CompilerResult(name, null);
            await Compile(name, content, result, true);
            return result;
        }

        public void ReferenceAssembly(Assembly assembly)
        {
            if (!_referencedAssemblies.Contains(assembly))
                _referencedAssemblies.Add(assembly);
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
                    var found = false;
                    foreach(var assemblyDirectory in _runtime.AssemblyDirectories)
                    {
                        var assemblyPath = Path.Combine(assemblyDirectory.FullName, ass);
                        if(File.Exists(assemblyPath))
                        {
                            var assembly = Assembly.LoadFrom(assemblyPath);
                            ReferenceAssembly(assembly);
                            _loadedAssemblies.Add(ass);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        throw new FileNotFoundException("Assembly not found.", ass);
                }
                catch (Exception ex)
                {
                    var error = Tr.Get("Compiler.AssemblyError", ass, ex.Message);
                    Log.Error(error, this);

                    result.SetError(CompilerResultType.AssemblyError, error);
                    return false;
                }
            }

            string assemblyErrName = null;
            try
            {
                foreach (var assembly in _referencedAssemblies)
                {
                    assemblyErrName = assembly.FullName;
                    assembly.GetTypes();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var error = new StringBuilder();
                error.AppendLine(assemblyErrName);
                foreach (Exception loaderEx in ex.LoaderExceptions)
                {
                    error.AppendLine(loaderEx.Message);
                }

                var e = error.ToString();
                Log.Error(e, this);
                result.SetError(CompilerResultType.AssemblyError, e);
                return false;
            }

            var scriptBuilder = new StringBuilder();
            foreach (var item in _results.Values)
            {
                if (item.Assembly != null)
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

            var baseFileName = $"{Sha1.Compute($"{Sha1.Compute(script)}|{Environment.UserName}").Replace("-", "")}-{rootClass}-rosi";
            var pdbFilePath = Path.Combine(Path.GetTempPath(), $"{baseFileName}.pdb");
            var assemblyFilPath = Path.Combine(Path.GetTempPath(), $"{baseFileName}.dll");

            var loadedFromFile = false;
            if(useCachedAssemblies && File.Exists(assemblyFilPath) && File.Exists(pdbFilePath))
            {
                try
                {
                    var assemblyData = await File.ReadAllBytesAsync(assemblyFilPath);
                    var pdbData = await File.ReadAllBytesAsync(pdbFilePath);

                    result.Assembly = AppDomain.CurrentDomain.Load(assemblyData, pdbData);
                    loadedFromFile = true;
                }
                catch { }
            }

            try
            {
                if (!loadedFromFile)
                {
                    var compilerSettings = ScriptOptions.Default.AddReferences(_referencedAssemblies);
                    var compilation = CSharpScript.Create(script, compilerSettings).GetCompilation();

                    compilation = compilation.WithOptions(compilation.Options
                        .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                        .WithOptimizationLevel(OptimizationLevel.Release)
                        .WithScriptClassName(rootClass)
                        );

                    using var pdb = new MemoryStream();
                    using var asm = new MemoryStream();
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                    var emitResult = compilation.Emit(asm, pdb, options: emitOptions);

                    if (!emitResult.Success)
                    {
                        IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

                        var message = new StringBuilder();
                        foreach (Diagnostic diagnostic in failures)
                        {
                            string error_location = "";
                            if (diagnostic.Location.IsInSource)
                            {
                                var error_pos = diagnostic.Location.GetLineSpan().StartLinePosition;

                                int error_line = error_pos.Line + 1;
                                int error_column = error_pos.Character + 1;

                                var source = "<script>";
                                //if (mapping.Any())
                                //    (source, error_line) = mapping.Translate(error_line);
                                //else
                                //    error_line--; // no mapping as it was a single file so translation is minimal

                                // the actual source contains an injected '#line' directive of compiled with debug symbols so increment line after formatting
                                //error_location = $"{(source.HasText() ? source : "<script>")}({error_line},{ error_column}): ";
                            }
                            message.AppendLine($"{error_location}error {diagnostic.Id}: {diagnostic.GetMessage()}");
                        }

                        var errors = message.ToString();
                        throw new Exception(errors);
                    }
                    else
                    {
                        asm.Seek(0, SeekOrigin.Begin);
                        var assemblyData = asm.GetBuffer();

                        pdb.Seek(0, SeekOrigin.Begin);
                        var pdbData = pdb.GetBuffer();

                        await File.WriteAllBytesAsync(assemblyFilPath, assemblyData);
                        await File.WriteAllBytesAsync(pdbFilePath, pdbData);

                        result.Assembly = AppDomain.CurrentDomain.Load(assemblyData, pdbData);
                    }
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

            return true;
        }
    }
}
