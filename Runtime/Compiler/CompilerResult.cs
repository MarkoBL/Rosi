using System.Collections.Generic;
using System.Reflection;

namespace Rosi.Compiler
{
    public sealed class CompilerResult
    {
        static string CleanupName(string value)
        {
            var result = string.Empty;

            if (value != null)
            {
                value = value.ToLower();
                foreach (var c in value)
                {
                    if (c >= 97 && c <= 122 || c >= 48 && c <= 57)
                        result += c;
                }
            }
            return result;
        }

        internal void SetError(CompilerResultType resultType, string error)
        {
            Result = resultType;
            Error = error;

            Root.ErrorRoot = this;
        }

        public CompilerResult ErrorRoot { get; internal set; }
        public CompilerResultType Result { get; internal set; }
        public string Error { get; internal set; }

        public ScriptParser ParsedScript { get; internal set; }
        public readonly string RootClass;
        public Assembly Assembly { get; internal set; }
        public string FinalScript { get; internal set; }

        public readonly CompilerResult Root;

        public IReadOnlyList<CompilerResult> More => _more;
        internal List<CompilerResult> _more = new List<CompilerResult>();

        public CompilerResult(string rootClass, CompilerResult root)
        {
            RootClass = CleanupName(rootClass);
            Root = root?.Root;
            if (Root == null)
                Root = this;
        }
    }
}
