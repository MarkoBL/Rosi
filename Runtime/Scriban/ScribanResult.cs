using Scriban.Runtime;
using System;

namespace Rosi.Scriban
{
    public sealed class ScribanResult
    {
        public readonly ScribanTemplate Template;
        public bool HasTemplateError => !Template.IsValid;
        public string TemplateError => Template.ErrorMessage;

        public readonly bool HasScriptError;
        public readonly string ScriptError;

        public readonly bool HasRenderError;
        public readonly string RenderError;

        public string Error => HasScriptError ? ScriptError : (HasTemplateError ? TemplateError : (HasRenderError ? RenderError : null));

        public bool HasError => HasTemplateError || HasScriptError || HasRenderError;
        public bool IsValid => !HasError;

        public readonly string Output;
        public readonly string Filename;

        readonly ScriptObject _result;

        public object GetResult(string name)
        {
            if (_result == null)
                return null;

            _result.TryGetValue(name, out var value);
            return value;
        }

        public T GetResult<T>(string name)
        {
            return (T)GetResult(name);
        }

        internal ScribanResult(ScribanTemplate template, ScriptObject result, string output, string renderError)
        {
            _result = result;
            Template = template;

            if(result != null)
            {
                ScriptError = (string)result["ScriptError"];
                HasScriptError = !string.IsNullOrWhiteSpace(ScriptError);

                Filename = (string)result["Filename"];
            }

            HasRenderError = !string.IsNullOrWhiteSpace(renderError);
            RenderError = renderError;

            Output = output;
        }
    }
}
