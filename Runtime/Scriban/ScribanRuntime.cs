using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Rosi.Core;

namespace Rosi.Scriban
{
    public sealed class ScribanRuntime : ITemplateLoader, ILogger
    {
        const string _initScript = @"
{{-
func setfilename
 Result.filename = $0
end

func setvalid
 $valid = $0
 $error = $1

 Result.valid = $valid
 if !$valid && $error
  Result.error = $error
 end
end

func setresult
 Result[$0] = $1
end

-}}";
        readonly TemplateContext _context;
        readonly DirectoryInfo _scribanPath;

        readonly ScriptObject _globals = new ScriptObject();

        public ScribanRuntime(DirectoryInfo scribanPath)
        {
            _scribanPath = scribanPath;

            _context = new TemplateContext
            {
                TemplateLoader = this,
                MemberRenamer = (memeber) => memeber.Name,
                NewLine = "\n"
            };

            _context.PushGlobal(_globals);
            Parse(_initScript);
        }

        public void ImportClass(Type type, string name = null)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import(type, null, (member) => member.Name);

            _globals[name ?? type.Name] = scriptObject;
        }

        public void ImportObject(string name, object value)
        {
            _globals[name] = value;
        }

        string ITemplateLoader.GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return $"{templateName}.scriban";
        }

        string ITemplateLoader.Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return File.ReadAllText(Path.Combine(_scribanPath.FullName, templatePath));
        }

        ValueTask<string> ITemplateLoader.LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return new ValueTask<string>(((ITemplateLoader)this).Load(context, callerSpan, templatePath));
        }

        public ScribanResult Render(string include, params string[] parameters)
        {
            var template = new StringBuilder($"{{{{- include '{include}' ");

            foreach (var parameter in parameters)
                template.Append($" '{parameter}' ");

            template.Append(" -}}");

            return Parse(template.ToString());
        }

        public ScribanResult Parse(string template)
        {
            var parsed = Template.Parse(template);

            var result = new ScriptObject
            {
                { "error", null },
                { "filename", null },
                { "valid", false}
            };

            _globals["Result"] = result;

            if (parsed.HasErrors)
            {
                Log.Error(parsed.ToString(), this);

                foreach (var error in parsed.Messages)
                {
                    Log.Error(error.ToString(), this);
                }

                return new ScribanResult(parsed.ToString(), null, result);
            }

            try
            {
                var output = parsed.Render(_context);
                var r = new ScribanResult((string)result["error"], output, result);

                if (r.HasError)
                    Log.Warn($"Scriban script error: {r.Error}.", this);

                return r;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), this);
                return new ScribanResult(ex.ToString(), null, result);
            }
        }
    }
}
