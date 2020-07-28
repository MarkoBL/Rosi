// set: rosi.waitafterexit 1
// compile: Host
using System;
using System.Collections.Generic;
using System.Net;

namespace ScribanTest
{
    public class Scriban : IRosi
    {
        public int Run(IRuntime runtime)
        {
            var scriban = runtime.Scriban;

            scriban.ImportObject("Hosts", new List<Host> {
                new Host { Name = "host1", Address = IPAddress.Parse("10.0.0.1") },
                new Host { Name = "host2", Address = IPAddress.Parse("10.0.0.2") }
            });

            var result = scriban.Render("hosts", "testhostname");
            if (result.Valid)
            {
                //System.IO.File.WriteAllText(result.Filename, result.Output);
                Console.WriteLine(result.Output);
                return 0;
            }

            return 1;
        }
    }
}
