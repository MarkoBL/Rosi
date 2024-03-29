﻿using System;
using System.Security.Cryptography;

namespace Rosi.Runtime.Core
{
    public static class Sha1
    {
        static readonly SHA1 _sha = SHA1.Create();

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
