namespace Rosi.Runtime.Compiler
{
    public class CompilerOptions : ScriptParserOptions
    {
        public bool Debugging = false;
        public bool UseCachedAssemblies = true;
        public bool LogScriptOnError = false;
        public string ScriptOutputPath = null;
    }
}
