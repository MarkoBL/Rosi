using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Rosi.Core;

namespace Rosi.Compiler
{
    public sealed class ScriptParser : ILogger
    {
        readonly Runtime _runtime;
        readonly StringBuilder _header = new StringBuilder();
        readonly StringBuilder _body = new StringBuilder();
        readonly List<string> _compile = new List<string>();
        readonly List<string> _postCompile = new List<string>();
        readonly List<string> _assemblies = new List<string>();
        readonly HashSet<string> _includes = new HashSet<string>();

        public ScriptParserResult Result { get; private set; }
        public string Error { get; private set; }

        public readonly string Script;

        public IReadOnlyList<string> CompileFiles => _compile;
        public IReadOnlyList<string> PostCompileFiles => _postCompile;
        public IReadOnlyList<string> AssemblyFiles => _assemblies;

        public ScriptParser(Runtime runtime, string name, string content)
        {
            _runtime = runtime;
            if (ParseScript(name, content))
            {
                Script = _header.ToString() + _body.ToString();
            }
        }

        public static void ParseOptions(FileInfo fileInfo, Runtime runtime)
        {
            var lines = File.ReadAllLines(fileInfo.FullName);
            foreach(var line in lines)
            {
                ParseOptions(line.Trim(), runtime);
            }
        }

        static bool ParseOptions(string line, Runtime runtime)
        {
            var config = runtime.Config;
            if (line.StartsWith("// set:", StringComparison.Ordinal))
            {
                var option = line.Substring(8).Trim();
                var idx = option.IndexOf(' ');
                if (idx > 0)
                {
                    var key = option.Substring(0, idx).Trim();
                    var value = option.Substring(idx).Trim();

                    config[key] = value;
                }
                return true;
            }
            else if (line.StartsWith("// debugset:", StringComparison.Ordinal))
            {
                var option = line.Substring(13).Trim();
                var idx = option.IndexOf(' ');
                if (idx > 0)
                {
                    var key = option.Substring(0, idx).Trim();
                    var value = option.Substring(idx).Trim();

                    if (runtime.Debugging)
                        config[key] = value;
                }
                return true;
            }

            return false;
        }

        List<FileInfo> GetFiles(string path, bool sub)
        {
            var result = new List<FileInfo>();

            var dir = new DirectoryInfo(path);

            if (sub)
            {
                var subDirs = dir.GetDirectories();
                foreach(var subDir in subDirs)
                {
                    result.AddRange(GetFiles(Path.Combine(path, subDir.Name), sub));
                }
            }

            var files = dir.GetFiles("*.rosi");
            foreach (var file in files)
            {
                result.Add(file);
            }

            files = dir.GetFiles("*.cs");
            foreach (var file in files)
            {
                result.Add(file);
            }

            return result;
        }

        bool ParseScript(string name, string content)
        {
            var header = new StringBuilder($"\n// {name}\n");
            var body = new StringBuilder($"\n\n// {name}\n");

            var includes = new List<string>();

            var skipNamespaceOpen = false;
            var skipNamespaceClose = false;

            var inHeader = true;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines)
            {
                var line = l.Trim();
                var scriptNamespace = _runtime.Config.ScriptNamespace;

                if (line.StartsWith("#!/", StringComparison.Ordinal)) // shebang
                {
                    continue;
                }
                else if (line.StartsWith("// compile:", StringComparison.Ordinal))
                {
                    var scriptFiles = line.Substring(12).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var scriptFile in scriptFiles)
                    {
                        var file = scriptFile.Trim();
                        if (!_compile.Contains(file))
                            _compile.Add(file);
                    }
                    continue;
                }
                else if (line.StartsWith("// postcompile:", StringComparison.Ordinal))
                {
                    var scriptFiles = line.Substring(16).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var scriptFile in scriptFiles)
                    {
                        var file = scriptFile.Trim();
                        if (!_compile.Contains(file))
                            _postCompile.Add(file);
                    }
                    continue;
                }
                else if (line.StartsWith("// include:", StringComparison.Ordinal))
                {
                    var scriptFiles = line.Substring(12).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var scriptFile in scriptFiles)
                    {
                        if (scriptFile.EndsWith("*"))
                        {
                            var p = Path.Combine(_runtime.Config.ScriptPath, scriptFile.Replace("*", ""));
                            var files = GetFiles(p, scriptFile.EndsWith("**"));
                            var scriptPath = new DirectoryInfo(_runtime.Config.ScriptPath).FullName;
                            if (!scriptPath.EndsWith(Path.DirectorySeparatorChar))
                                scriptPath += Path.DirectorySeparatorChar;

                            foreach (var file in files)
                            {
                                var path = file.FullName.Replace(scriptPath, "");

                                if (path == name)
                                    continue;

                                includes.Add(path);
                            }
                        }
                        else
                        {
                            if (name == scriptFile)
                            {
                                Error = Tr.Get("ScriptParser.CyclingInclude", name, scriptFile);
                                Log.Error(Error);
                                Result = ScriptParserResult.CyclingInclude;

                                return false;
                            }
                            includes.Add(scriptFile.Trim());
                        }
                    }
                    continue;
                }
                else if (ParseOptions(line, _runtime))
                {
                    continue;
                }
                else if (line.StartsWith("// assembly:", StringComparison.Ordinal))
                {
                    var assemblyFiles = line.Substring(13).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var assembly in assemblyFiles)
                    {
                        var file = assembly.Trim();
                        if (!file.EndsWith(".dll"))
                            file += ".dll";

                        if (!_assemblies.Contains(file))
                            _assemblies.Add(file);
                    }
                    continue;
                }
                else if (line.StartsWith("/", StringComparison.Ordinal) || line.StartsWith("*", StringComparison.Ordinal))
                {
                    if (inHeader)
                    {
                        header.AppendLine(l);
                        continue;
                    }
                }
                else if (line.StartsWith($"using {scriptNamespace};") || line.StartsWith($"using {scriptNamespace}."))
                {
                    continue;
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    if (inHeader)
                        continue;
                }

                if (inHeader)
                {
                    if (!(line.StartsWith("using", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal)))
                        inHeader = false;
                }

                if (line.StartsWith("namespace", StringComparison.Ordinal))
                {
                    body.AppendLine($"// {line}");
                    skipNamespaceOpen = !line.Contains('{');
                    skipNamespaceClose = true;
                    continue;
                }

                if (skipNamespaceOpen)
                {
                    if (line.Contains('{'))
                    {
                        body.AppendLine($"// {line}");
                        skipNamespaceOpen = false;
                    }
                    continue;
                }

                if (inHeader)
                    header.AppendLine(l);
                else
                    body.AppendLine(l);
            }

            var b = body.ToString();
            if (skipNamespaceClose)
            {
                var idx = b.LastIndexOf('}');
                b = $"{b.Substring(0, idx)}// }}";
            }

            var h = header.ToString();

            _header.Append(h);
            _body.Append(b);

            foreach (var include in includes)
            {
                if (!_includes.Add(include))
                {
                    Log.Error(Tr.Get("ScriptParser.AlreadyIncluded", name, include), this);
                    return false;
                }

                var (fileinfo, error) = GetScriptFileInfo(include);
                if (fileinfo == null)
                {
                    Error = error;
                    Result = ScriptParserResult.IncludeNotFound;
                    return false;
                }

                if (!ParseScript(include, File.ReadAllText(fileinfo.FullName)))
                {
                    return false;
                }
            }

            return true;
        }

        public (FileInfo, string) GetScriptFileInfo(string name)
        {
            var path = _runtime.Config.ScriptPath;
            if (!string.IsNullOrWhiteSpace(name))
            {
                var fileInfo = new FileInfo(Path.Combine(path, name));
                if (fileInfo.Exists)
                    return (fileInfo, null);

                fileInfo = new FileInfo(Path.Combine(path, $"{name}.rosi"));
                if (fileInfo.Exists)
                    return (fileInfo, null);

                fileInfo = new FileInfo(Path.Combine(path, $"{name}.cs"));
                if (fileInfo.Exists)
                    return (fileInfo, null);
            }

            var error = Tr.Get("ScriptParser.ScriptNotFound", Path.Combine(path, name));
            Log.Error(error, this);
            return (null, error);
        }
    }
}
