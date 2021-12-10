using System.Collections.Generic;
using System.Reflection;

namespace Rosi.Runtime.Compiler
{
    public sealed class CompilerResult
    {
        internal static string CleanupName(string value)
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
        }

        public CompilerResultType Result { get; internal set; }
        public string Error { get; internal set; }

        public ScriptParser ParsedScript { get; internal set; }
        public readonly string RootClass;
        public Assembly Assembly { get; internal set; }

        public CompilerResult(string rootClass)
        {
            RootClass = CleanupName(rootClass);
        }
    }
}
