using System;
using System.Text;

namespace Execute
{
    public class SshKey
    {
        public readonly string PublicKey;
        public readonly string PrivateKey;

        public  SshKey(string publicKey, string privateKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException(nameof(publicKey));

            PublicKey = publicKey.Trim();
            PrivateKey = privateKey;
        }

        public SshKey(string data)
        {
            var split = data.Split(',');
            PublicKey = split[0];
            if(split.Length > 1)
                PrivateKey = Encoding.UTF8.GetString(Convert.FromBase64String(split[1]));
        }

        public override string ToString()
        {
            if (PrivateKey == null)
                return PublicKey;

            return $"{PublicKey},{Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKey))}";
        }
    }
}
