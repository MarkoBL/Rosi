using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Rosi.Core;

namespace Rosi
{
    class Program
    {
        public static readonly Uri GithubLatestReleaseUrl = new Uri("https://api.github.com/repos/MarkoBL/Rosi/releases/latest");

        static void Print(string text)
        {
            Console.WriteLine(text);
        }

        static void PrintVersion()
        {
            Print(Runtime.RuntimeVersion.ToString(3));
        }

        static void PrintRuntimeVersions()
        {
            Print($"Rosi Runtime: {Runtime.RuntimeVersion.ToString(3)}");
            Print($"Scriban: {Runtime.ScribanVersion.ToString(3)}");
            Print($".NET Core: {Runtime.NetCoreVersion}");
        }

        static void PrintMacOsWelcome()
        {
            Console.Clear();
            Print("Thanks for using Rosi.\n");

            PrintRuntimeVersions();
        }

        static async Task<int> CheckUpdate(bool silent)
        {
            try
            {
                var version = Runtime.RuntimeVersion;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Rosi", version.ToString(3)));

                var response = await client.GetStringAsync(GithubLatestReleaseUrl);

                var latestRelease = JsonSerializer.Deserialize<GithubRelease>(response);
                var latestVersion = latestRelease.Version;
                if (latestVersion > version)
                {

                    Log.Warn($"New Rosi version available: {latestVersion}. Installed version: {version.ToString(3)}.\nDownload: {latestRelease.DownloadUrl}");
                    return 1;
                }

                if(!silent)
                    Log.Info("No new version available. Latest Rosi version installed.");
                return 0;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return -1;
            }
        }

        static async Task<int> Main(string[] args)
        {
            typeof(YamlDotNet.Core.IEmitter).GetType();
            typeof(System.Net.IPNetwork).GetType();

            foreach (var arg in args)
            {
                if (arg == "--rosiversion")
                {
                    PrintVersion();
                    return 0;
                }
                else if (arg == "--rosiruntimeversions")
                {
                    PrintRuntimeVersions();
                    return 0;
                }
                else if (arg == "--rosimacoswelcome")
                {
                    PrintMacOsWelcome();
                    await CheckUpdate(true);
                    return 0;
                }
                else if (arg == "--rosicheckupdate")
                {
                    return await CheckUpdate(false);
                }
            }


            var runtime = new Runtime(null, args);

            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = runtime.Config.Get("rosi.title", "Rosi");
            var result = await runtime.RunAsync();

            if (runtime.Config.Get("rosi.waitforexit", false))
                Console.ReadKey();

            return result;
        }
    }
}
