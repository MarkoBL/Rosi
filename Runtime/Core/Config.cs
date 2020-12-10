using System;
using System.Collections.Generic;

namespace Rosi.Core
{
    public sealed class Config : Dictionary<string, string>
    {
        public readonly string[] Arguments;
        readonly ArgumentsParser _argumentsParser;

        public string LogFilename => Get("runtime.logfilename", "log.txt");
        public bool LogAppend => Get("runtime.logfileappend", true);
        public bool LogToFile => Get("runtime.logtofile", false);
        public bool LogToConsole => Get("runtime.logtoconsole", true);
        public string ConsoleLogLevel => Get("runtime.consoleloglevel", LogLevels.Info.ToString());
        public string FileLogLevel => Get("runtime.fileloglevel", LogLevels.Warning.ToString());
        public bool LogScript => Get("runtime.logscript", false);
        public string TranslationPath => Get("runtime.translationpath", ".");

        public bool CacheAssemblies => Get("runtime.usecachedassemblies", true);
        public string ScriptPath => Get("runtime.scriptpath", ".");
        public string AssemblyPath => Get("runtime.assemblypath", ".");
        public string ScriptOutputPath => Get("runtime.scriptoutputpath", "");
        public string ScriptNamespace => Get("runtime.scriptnamespace", "Script");
        public string ScribanScriptPath => Get("scriban.path", ".");

        public Config(string[] args)
        {
            Arguments = args;
            _argumentsParser = new ArgumentsParser(args);
        }

        bool GetValue(string key, out string value)
        {
            if (_argumentsParser.GetString(key, out value))
                return true;

            return TryGetValue(key, out value);
        }

        public bool Get(string key, out string value)
        {
            return GetValue(key, out value);
        }

        public string Get(string key, string @default)
        {
            if (GetValue(key, out var value))
                return value;

            return @default;
        }

        public bool Get(string key, out bool value)
        {
            if(GetValue(key, out var v))
            {
                v = v.Trim().ToLower();
                value = v == "1" || v == "yes" || v == "on" || v == "true";
                return true;
            }

            value = false;
            return false;
        }

        public bool Get(string key, bool @default)
        {
            if (Get(key, out bool value))
                return value;

            return @default;
        }

        public bool Get(string key, out int value)
        {
            if(GetValue(key, out var v))
            {
                if (int.TryParse(v, out value))
                    return true;
            }

            value = 0;
            return false;
        }
    }
}
