using System;
using System.Threading.Tasks;

namespace Execute
{
    public class Execute : IAsyncRosi
    {
        public async Task<int> Run(IRuntime runtime)
        {
            var sshKey = await Ssh.GenerateEd25519Key();
            Console.WriteLine($"New Ed25519 Ssh Key.\nPublic Key:\n{sshKey.PublicKey}\n\nPrivate Key:\n{sshKey.PrivateKey}");
            return 0;
        }
    }
}
