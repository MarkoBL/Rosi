using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Rosi.Runtime.Core
{
	// http://www.codeproject.com/Articles/3111/C-NET-Command-Line-Arguments-Parser
	sealed class ArgumentsParser
	{
        readonly StringDictionary _parameters = new StringDictionary();

		public ArgumentsParser(string[] args)
		{
			//var splitter = new Regex(@"^-{1,2}|=", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var splitter = new Regex(@"^-{1,2}|[^['""]?.*]=['""]?$|[^['""]?.*]:['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var remove = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			string parameter = null;

			// Valid parameters forms:
			// {-,--}param{ ,=}((",')value(",'))
			// Examples: 
			// -param1 value1 --param2 -param3="Test-:-work" 
			//   /param4=happy -param5 '--=nice=--'
			foreach(string arg in args)
			{
				// Look for new parameters (-,/ or --) and a
				// possible enclosed value (=,:)
				var parts = splitter.Split(arg, 3);

				switch(parts.Length) 
                {
    				// Found a value (for the last parameter 
    				// found (space separator))
    				case 1:
    					if(parameter != null)
    					{
    						if(!_parameters.ContainsKey(parameter)) 
    						{
    							parts[0] = remove.Replace(parts[0], "$1");
    							_parameters.Add(parameter, parts[0]);
    						}
    						parameter = null;
    					}
    					// else Error: no parameter waiting for a value (skipped)
    					break;

    					// Found just a parameter
    				case 2:
    					// The last parameter is still waiting. 
    					// With no value, set it to true.
    					if(parameter != null)
    					{
    						if(!_parameters.ContainsKey(parameter)) 
    							_parameters.Add(parameter, "true");
    					}
    					parameter = parts[1];
    					break;

    					// Parameter with enclosed value
    				case 3:
    					// The last parameter is still waiting. 
    					// With no value, set it to true.
    					if(parameter != null)
    					{
    						if(!_parameters.ContainsKey(parameter)) 
    							_parameters.Add(parameter, "true");
    					}

    					parameter = parts[1];

    					// Remove possible enclosing characters (",')
    					if(!_parameters.ContainsKey(parameter))
    					{
    						parts[2] = remove.Replace(parts[2], "$1");
    						_parameters.Add(parameter, parts[2]);
    					}

    					parameter = null;
    					break;
				}
			}

			// In case a parameter is still waiting
			if(parameter != null)
			{
				if(!_parameters.ContainsKey(parameter)) 
					_parameters.Add(parameter, "true");
			}
		}

		// Retrieve a parameter value if it exists 
		// (overriding C# indexer property)
		public string this [string parameter]
		{
			get
			{
				return(_parameters[parameter]);
			}
		}

		public bool Contains(string name)
		{
            return _parameters.ContainsKey(name);
		}

        public string GetString(string name, string @default)
		{
			if (!Contains (name))
				return @default;

			return this[name];
		}

        public bool GetString(string name, out string value)
        {
            if(!Contains(name))
            {
                value = default;
                return false;
            }

            value = this[name];
            return true;
        }
	}
}
