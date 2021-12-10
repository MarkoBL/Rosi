using System;

namespace Rosi.Runtime.Compiler
{
    public class ScriptParserOptions
    {
        public string ScriptPath = ".";
        public string ScriptNamespace = "Script";
        public Action<string, string> SetDirective;
    }
}
