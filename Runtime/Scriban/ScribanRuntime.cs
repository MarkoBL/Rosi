using System;
using System.IO;
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
func setfilename (filename)
 Result.Filename = filename
end

func seterror (error)
  Result.Error = $error
end

func setresult (key, value)
 Result[key] = value
end

-}}";
        readonly TemplateContext _context;
        readonly DirectoryInfo _scribanPath;

        readonly ScriptObject _globals = new ScriptObject();
        readonly bool _forceLinefeed;

        public ScribanRuntime(DirectoryInfo scribanPath, bool forceLineFeed = true)
        {
            _scribanPath = scribanPath;

            _context = new TemplateContext
            {
                TemplateLoader = this,
                MemberRenamer = (memeber) => memeber.Name,
            };

            _forceLinefeed = forceLineFeed;
            if (_forceLinefeed)
                _context.NewLine = "\n";

            _context.PushGlobal(_globals);
            Render(ScribanTemplate.Parse(_initScript));
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

        public ScribanResult Render(ScribanTemplate template)
        {
            try
            {
                if (!template.IsValid)
                    return new ScribanResult(template, null, null, null);

                var result = new ScriptObject
                {
                    { "ScriptError", string.Empty },
                    { "Filename", null }
                };

                _globals["Result"] = result;

                var output = template.Template.Render(_context);
                return new ScribanResult(template, result, output, null);
            }
            catch (Exception ex)
            {
                Log.Error(template.Text);
                Log.Error(ex.ToString(), this);
                if (ex.InnerException != null)
                    Log.HandleException(ex.InnerException, this);
                return new ScribanResult(template, null, null, ex.ToString());
            }
        }
    }
}
