using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Rosi.Runtime.Compiler;
using Rosi.Runtime;
using Rosi.Runtime.Scriban;

public interface IRuntime
{
    Config Config { get; }
    ScribanRuntime Scriban { get; }

    void AddAssemblyDirectory(DirectoryInfo directoryInfo);
    void ReferenceAssembly(Assembly assembly);
    Task<CompilerResult> CompileScript(FileInfo fileInfo, CompilerOptions compilerOptions);

    T GetValue<T>(string name);
    bool TryGetValue<T>(string name, out T value);
    void SetValue(string name, object value);
}