// include: SshKey

using System;
using System.IO;
using System.Threading.Tasks;

namespace Execute
{
    public static class Ssh
    {
        readonly static string _tempPath;

        static Ssh()
        {
            _tempPath = Path.GetTempPath();
        }

        public static async Task<SshKey> GenerateEd25519Key()
        {
            var guid = Guid.NewGuid();

            var privateKeyFile = Path.Combine(_tempPath, guid.ToString());
            var publicKeyFile = Path.Combine(_tempPath, $"{guid}.pub");

            var execute = await Rosi.Core.Execute.RunAsync("ssh-keygen", $"-t ed25519 -f {privateKeyFile} -C \"\" -q -N \"\"");

            if(execute.ExitCode == 0)
            {
                var privateKey = File.ReadAllText(privateKeyFile);
                var publicKey = File.ReadAllText(publicKeyFile);

                File.Delete(privateKeyFile);
                File.Delete(publicKeyFile);

                return new SshKey(publicKey, privateKey);
            }

            throw new Exception($"Could not generate SSH Ed25519 key, {execute.StdErr}{execute.StdOut}");
        }
    }
}
