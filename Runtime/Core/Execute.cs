using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rosi.Runtime.Core
{
    public sealed class Execute
    {
        public readonly int ExitCode;
        public bool Success => ExitCode == 0;

        public byte[] StdOutData { get; private set; }
        public byte[] StdErrData { get; private set; }

        public string StdOut => StdOutData != null ? Encoding.UTF8.GetString(StdOutData) : string.Empty;
        public string StdErr => StdErrData != null ? Encoding.UTF8.GetString(StdErrData) : string.Empty;

        Execute(int exitCode)
        {
            ExitCode = exitCode;
        }

        public static Task<Execute> RunAsync(string cmd, string args = "", string workingDirectory = "")
        {
            var completionSource = new TaskCompletionSource<Execute>();
            var stdStream = new MemoryStream();
            var errStream = new MemoryStream();

            errStream.ToArray();

            var startInfo = new ProcessStartInfo()
            {
                FileName = cmd,
                Arguments = args,
                WorkingDirectory = workingDirectory,

                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                var execute = new Execute(process.ExitCode);

                if (stdStream.Length > 0)
                    execute.StdOutData = stdStream.ToArray();
                if (errStream.Length > 0)
                    execute.StdErrData = errStream.ToArray();

                stdStream.Dispose();
                stdStream.Dispose();

                completionSource.SetResult(execute);
            };

            process.Start();

            process.StandardOutput.BaseStream.CopyTo(stdStream);
            process.StandardError.BaseStream.CopyTo(errStream);

            return completionSource.Task;
        }
    }
}
