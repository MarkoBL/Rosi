﻿using System.Text;
using Scriban;
using Rosi.Core;
using System.Collections.Generic;

namespace Rosi.Scriban
{
    public class ScribanTemplate
    {
        static readonly Dictionary<string, ScribanTemplate> _scriptLookup = new Dictionary<string, ScribanTemplate>();

        public bool IsValid => !Template.HasErrors;
        public readonly string Text;
        public readonly string ErrorMessage;
        internal readonly Template Template;

        ScribanTemplate(string text, Template template)
        {
            Text = text;
            Template = template;

            if(!IsValid)
            {
                ErrorMessage = "";
                foreach (var message in template.Messages)
                {
                    Log.Error(message.ToString());
                    ErrorMessage += $"{message}\n";
                }
            }
        }

        public static ScribanTemplate Parse2(string script, bool cache = true)
        {
            return Parse($"{{{{- {script} -}}}}", cache);
        }

        public static ScribanTemplate Parse(string script, bool cache = true)
        {
            if (cache)
            {
                if (_scriptLookup.TryGetValue(script, out var cached))
                    return cached;
            }

            var template = Template.Parse(script);
            if (template.HasErrors)
            {
                Log.Error(script);

                foreach (var message in template.Messages)
                {
                    Log.Error(message.ToString());
                }
            }

            var result = new ScribanTemplate(script, template);
            if (cache)
                _scriptLookup.Add(script, result);
            return result;
        }

        public static ScribanTemplate Load(string filename, params string[] parameters)
        {
            var script = new StringBuilder($"{{{{- include '{filename}' ");

            foreach (var parameter in parameters)
                script.Append($" '{parameter}' ");

            script.Append(" -}}");

            return Parse(script.ToString());
        }
    }
}