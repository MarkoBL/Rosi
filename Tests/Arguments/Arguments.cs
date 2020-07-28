// set: config.test Hello Config!
using System;

namespace Arguments
{
    class Arguments : IRosi
    {
        public int Run(IRuntime runtime)
        {
            Console.WriteLine($"Config: {runtime.Config.Get("config.test", string.Empty)}");
            Console.WriteLine($"Argument: {runtime.Config.Get("argument.test", string.Empty)}");

            return 0;
        }
    }
}
