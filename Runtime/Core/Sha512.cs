using System;
using System.Security.Cryptography;

namespace Rosi.Runtime.Core
{
    public static class Sha512
    {
        static readonly SHA512 _sha = SHA512.Create();

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
