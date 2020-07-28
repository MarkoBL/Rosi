using System.Threading.Tasks;
using Rosi.Tests;

namespace Rosi.Playground
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Test.UnitTest = false;
            Test.BasePath = "../../../../Tests/";

            Test.Debug = true;

            await Test.Arguments();
            await Test.Shebang();
            await Test.Scriban();
        }
    }
}
