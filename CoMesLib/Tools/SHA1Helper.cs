using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CorporateMessengerLibrary.Tools
{
    public static class Sha1Helper
    {
        public static string GetHash(byte[] imageData)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return ByteArrayToString(sha1.ComputeHash(imageData));
            }
        }

        //stolen from http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
