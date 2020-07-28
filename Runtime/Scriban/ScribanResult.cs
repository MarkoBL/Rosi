using Scriban.Runtime;

namespace Rosi.Scriban
{
    public sealed class ScribanResult
    {
        public readonly bool HasError;
        public readonly string Error;

        public readonly string Output;
        public readonly string Filename;

        public readonly bool Valid;

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

        public ScribanResult(string error, string output, ScriptObject result)
        {
            HasError = !string.IsNullOrWhiteSpace(error);
            Error = error;

            Valid = (bool)result["valid"];
            Filename = (string)result["filename"];

            Output = output;
            _result = result;
        }
    }
}
