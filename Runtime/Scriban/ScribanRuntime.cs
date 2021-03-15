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
        static readonly ScribanTemplate _initTemplate;
        public static ScribanTemplate DefaultInitTemplate;

        static ScribanRuntime()
        {
            _initTemplate = ScribanTemplate.Parse(@"
{{-
func setscripterror (error)
  RosiScriptError = error
end
-}}");
        }

        readonly TemplateContext _context;
        readonly DirectoryInfo _scribanPath;
        readonly bool _forceLinefeed;

        public readonly ScriptObject Globals = new ScriptObject();

        public ScribanRuntime(string scribanPath, bool forceLineFeed)
        {
            _scribanPath = new DirectoryInfo(scribanPath);

            _context = new TemplateContext
            {
                TemplateLoader = this,
                MemberRenamer = (memeber) => memeber.Name,
            };

            _forceLinefeed = forceLineFeed;
            if (_forceLinefeed)
                _context.NewLine = "\n";

            _context.PushGlobal(Globals);
            Render(_initTemplate);

            if(DefaultInitTemplate != null)
                Render(DefaultInitTemplate);
        }

        public object GetGlobalValue(string name)
        {
            Globals.TryGetValue(name, out var value);
            return value;
        }

        public T GetGlobalValue<T>(string name)
        {
            return (T)GetGlobalValue(name);
        }

        public bool HasGlobalValue(string name)
        {
            return Globals.ContainsKey(name);
        }

        public void ImportClass(Type type, string name)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import(type, null, (member) => member.Name);

            Globals[name] = scriptObject;
        }

        public void ImportObject(object value, string name)
        {
            Globals[name] = value;
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
                    return new ScribanResult(ScribanResultType.TemplateError, this, template, template.ErrorMessage);

                Globals["RosiScriptError"] = string.Empty;

                var output = template.Template.Render(_context);
                if (!string.IsNullOrEmpty(output) && _forceLinefeed)
                    output = output.Replace("\r\n", "\n"); ;

                var scriptError = (string)Globals["RosiScriptError"];
                if(!string.IsNullOrWhiteSpace(scriptError))
                    return new ScribanResult(ScribanResultType.ScriptError, this, template, scriptError);

                return new ScribanResult(ScribanResultType.Success, this, template, output);
            }
            catch (Exception ex)
            {
                Log.Error(template.Text);
                Log.Error(ex.ToString(), this);
                if (ex.InnerException != null)
                    Log.HandleException(ex.InnerException, this);
                return new ScribanResult(ScribanResultType.RenderError, this, template, ex.ToString());
            }
        }
    }
}
