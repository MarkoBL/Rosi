// set rosi.waitafterexit 1
// include Host.cs
using Rosi.Scriban;
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

            scriban.ImportObject(new List<Host> {
                new Host { Name = "host1", Address = IPAddress.Parse("10.0.0.1") },
                new Host { Name = "host2", Address = IPAddress.Parse("10.0.0.2") }
            }, "Hosts");

            var result = scriban.Render(ScribanTemplate.Load("hosts", "testhostname"));
            if (result.IsValid)
            {
                //var filename = result.Runtime.GetGlobalValue<string>("Filename");
                //System.IO.File.WriteAllText(filename, result.Output);
                Console.WriteLine(result.Output);
                return 0;
            }

            Console.WriteLine(result.Error);
            return 1;
        }
    }
}
