using System;
using System.Security.Cryptography;

namespace Rosi.Core
{
    public static class Sha1
    {
        static readonly SHA1 _sha = new SHA1CryptoServiceProvider();

        public static string Compute(byte[] data)
        {
            return BitConverter.ToString(_sha.ComputeHash(data));
        }

        public static string Compute(string data)
        {
            return Compute(System.Text.Encoding.UTF8.GetBytes(data));
        }
    }
}
