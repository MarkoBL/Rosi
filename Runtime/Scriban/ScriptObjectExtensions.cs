using System;
using Scriban.Runtime;

namespace Rosi.Runtime.Scriban
{
    public static class ScriptObjectExtensions
    {
        public static ScriptObject ImportClass(this ScriptObject scriptObject, Type type, string name, ScriptObject importScriptObject = null)
        {
            importScriptObject ??= new ScriptObject();
            importScriptObject.Import(type, null, (member) => member.Name);

            scriptObject[name] = importScriptObject;
            return importScriptObject;
        }

        public static void ImportClass(this ScriptObject scriptObject, Type type)
        {
            scriptObject.Import(type, null, (member) => member.Name);
        }

        public static void ImportObject(this ScriptObject scriptObject, object value, string name)
        {
            scriptObject[name] = value;
        }
    }
}
