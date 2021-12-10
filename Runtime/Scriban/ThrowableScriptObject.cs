using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System;
using System.Collections.Generic;

namespace Rosi.Runtime.Scriban
{
    public class ThrowableScriptObject : ScriptObject
    {
        public static readonly HashSet<string> IgnoreUnknownMembers = new HashSet<string> { "array", "date", "html", "math", "object", "regex", "string", "timespan" };
        public bool ThrowOnUnknownMember = true;

        public override bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
        {
            if (!base.TryGetValue(context, span, member, out value))
            {
                if(ThrowOnUnknownMember && !IgnoreUnknownMembers.Contains(member))
                    throw new ArgumentException($"Member '{member}' not found.", nameof(member));
                return false;
            }
            return true;
        }
    }
}
