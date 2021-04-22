using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System;

namespace Rosi.Scriban
{
    public class ThrowableScriptObject : ScriptObject
    {
        public bool ThrowOnUnknownMember = true;

        public override bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
        {
            if (!base.TryGetValue(context, span, member, out value) && ThrowOnUnknownMember)
                throw new ArgumentException($"Member '{member}' not found.", nameof(member));
            return true;
        }
    }
}
