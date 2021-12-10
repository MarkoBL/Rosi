using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Rosi.Runtime.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rosi.Runtime.Compiler
{
    public sealed class Compiler : ILogger
    {
        readonly Runtime _runtime;
        static readonly HashSet<string> _loadedAssemblies = new HashSet<string>();

        internal Compiler(Runtime runtime)
        {
            _runtime = runtime;
        }

        public async Task<CompilerResult> Compile(string name, string script, CompilerOptions compilerOptions)
        {
            var result = new CompilerResult(name);
            var rootClass = result.RootClass;
            var parsedScript = new ScriptParser(compilerOptions, name, script);
            result.ParsedScript = parsedScript;

            foreach (var assemblyName in parsedScript.AssemblyFiles)
            {
                var ass = assemblyName;
                // ignore during debugging
                if (ass.StartsWith('!'))
                {
                    if (compilerOptions.Debugging)
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
                            _runtime.ReferenceAssembly(assembly);
                            _loadedAssemblies.Add(ass);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        throw new FileNotFoundException("Assembly not found.", ass);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var error = new StringBuilder();
                    error.AppendLine(ass);
                    foreach (Exception loaderEx in ex.LoaderExceptions)
                    {
                        error.AppendLine(loaderEx.Message);
                    }

                    var e = error.ToString();
                    Log.Error(e, this);
                    result.SetError(CompilerResultType.AssemblyError, e);
                    return result;
                }
                catch (Exception ex)
                {
                    var error = Tr.Get("Compiler.AssemblyError", ass, ex.Message);
                    Log.Error(error, this);

                    result.SetError(CompilerResultType.AssemblyError, error);
                    return result;
                }
            }

            var finalScript = parsedScript.Script;
            if (string.IsNullOrWhiteSpace(finalScript))
            {
                var error = Tr.Get("Compiler.ScriptEmpty", name);
                Log.Error(error, this);
                result.SetError(CompilerResultType.CompileError, error);
                return result;
            }

            var useCachedAssemblies = compilerOptions.UseCachedAssemblies;

            var baseFileName = $"{Sha1.Compute($"{Sha1.Compute(finalScript)}|{Environment.UserName}").Replace("-", "")}-{rootClass}-rosi";

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
                    var compilerSettings = ScriptOptions.Default.AddReferences(_runtime.ReferencedAssemblies).WithEmitDebugInformation(compilerOptions.Debugging);
                    var compilation = CSharpScript.Create(finalScript, compilerSettings).GetCompilation();

                    compilation = compilation.WithOptions(compilation.Options
                        .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                        .WithOptimizationLevel(compilerOptions.Debugging ? OptimizationLevel.Debug : OptimizationLevel.Release)
                        .WithScriptClassName(rootClass)
                        );

                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

                    using var pdbStream = new MemoryStream();
                    using var assemblyStream = new MemoryStream();
                    var emitResult = compilation.Emit(assemblyStream, pdbStream, options: emitOptions);

                    if (emitResult.Success)
                    {
                        assemblyStream.Seek(0, SeekOrigin.Begin);
                        var assemblyData = assemblyStream.GetBuffer();

                        pdbStream.Seek(0, SeekOrigin.Begin);
                        var pdbData = pdbStream.GetBuffer();

                        await File.WriteAllBytesAsync(assemblyFilPath, assemblyData);
                        await File.WriteAllBytesAsync(pdbFilePath, pdbData);

                        result.Assembly = AppDomain.CurrentDomain.Load(assemblyData, pdbData);
                    }
                    else
                    {
                        var diagnostics = emitResult.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

                        var message = new StringBuilder();
                        foreach (var diagnostic in diagnostics)
                        {
                            var errorLocation = string.Empty;
                            if (diagnostic.Location.IsInSource)
                            {
                                var errorPosition = diagnostic.Location.GetLineSpan().StartLinePosition;
                                errorLocation = $" ({errorPosition.Line},{errorPosition.Character})";
                            }
                            message.AppendLine($"{diagnostic.Id}{errorLocation}: {diagnostic.GetMessage()}");
                        }

                        throw new Exception(message.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                var error = Tr.Get("Compiler.Error", name, ex.Message);

                if (compilerOptions.LogScriptOnError)
                    error += finalScript + "\n";

                Log.Error(error, this);

                result.SetError(CompilerResultType.CompileError, error);
                return result;
            }

            var outputPath = compilerOptions.ScriptOutputPath;
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                try
                {
                    var scriptPath = Path.Combine(outputPath, name);
                    if (!loadedFromFile || !File.Exists(scriptPath))
                    {
                        Directory.CreateDirectory(outputPath);
                        await File.WriteAllTextAsync(scriptPath, finalScript);
                    }
                }
                catch
                { }
            }

            result.Result = CompilerResultType.Ok;
            return result;
        }
    }
}
