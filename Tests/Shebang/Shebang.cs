using System;

namespace Shebang
{
    public class Shebang : IRosi
    {
        public int Run(IRuntime runtime)
        {
            Console.WriteLine("Hello World!");

            if (runtime.GetValue<bool>("playground"))
                Console.WriteLine("Started from the playground.");

            return 0;
        }
    }
}
