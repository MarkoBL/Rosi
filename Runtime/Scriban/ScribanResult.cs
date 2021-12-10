using Scriban.Runtime;
using System;

namespace Rosi.Runtime.Scriban
{
    public enum ScribanResultType
    {
        Success,
        TemplateError,
        RenderError,
        ScriptError
    }

    public sealed class ScribanResult
    {
        public readonly ScribanResultType ResultType;
        public readonly ScribanRuntime Runtime;
        public readonly ScribanTemplate Template;

        public bool HasError => !IsValid;
        public bool IsValid => ResultType == ScribanResultType.Success;

        public readonly string Output;
        public readonly string Error;

        ScribanResult(ScribanResultType resultType, ScribanRuntime runtime, ScribanTemplate template)
        {
            ResultType = resultType;
            Runtime = runtime;
            Template = template;
        }

        internal ScribanResult(ScribanResultType resultType, ScribanRuntime runtime, ScribanTemplate template, string message) : this(resultType, runtime, template)
        {
            if(resultType == ScribanResultType.Success)
            {
                Output = message;
            }
            else
            {
                Error = message;
            }
        }
    }
}
