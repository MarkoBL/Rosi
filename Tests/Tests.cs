using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Rosi.Runtime;

namespace Rosi.Tests
{
    public class Test
    {
        public static string BasePath = "../../../";
        public static bool UnitTest = true;
        public static bool Debug;

        static DirectoryInfo _rootPath;

        static Type DebugType<T>()
        {
            return Debug ? typeof(T) : null;
        }

        static Runtime.Runtime NewRuntime<T>(params string[] args)
        {
            if (_rootPath == null)
                _rootPath = new DirectoryInfo(BasePath);

            Directory.SetCurrentDirectory(_rootPath.FullName);
            return new Runtime.Runtime(DebugType<T>(), args).SetValue("playground", true);
        }

        [Fact]
        public static async Task Shebang()
        {
            var result = await NewRuntime<Shebang.Shebang>("Shebang/Shebang.rosi").RunAsync();
            if(UnitTest)
                Assert.False(result == 0, "Shebang failed.");
        }

        [Fact]
        public static async Task Scriban()
        {
            var result = await NewRuntime<ScribanTest.Scriban>("Scriban/Scriban.cs").RunAsync();
            if(UnitTest)
                Assert.False(result == 0, "Scriban failed.");
        }

        [Fact]
        public static async Task Arguments()
        {
            var runtime = NewRuntime<Arguments.Arguments>("Arguments/Arguments.cs", "-argument.test", "Hello Argument!");

            var result = await runtime.RunAsync();
            if (UnitTest)
                Assert.False(result == 0, "Arguments failed.");
        }

        [Fact]
        public static async Task Execute()
        {
            var runtime = NewRuntime<Execute.Execute>("Execute/Execute.cs");

            var result = await runtime.RunAsync();
            if (UnitTest)
                Assert.False(result == 0, "Arguments failed.");
        }
    }
}
