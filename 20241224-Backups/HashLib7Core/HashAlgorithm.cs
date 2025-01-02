using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    internal class HashAlgorithm
    {
        private System.Security.Cryptography.SHA1 sha1;

        internal HashAlgorithm()
        {
            sha1 = System.Security.Cryptography.SHA1.Create();
        }

        internal string HashFile(string fileName)
        {
            System.IO.FileStream inputStream = Io.GetFileStream(fileName);
            byte[] hash = sha1.ComputeHash(inputStream);
            return ByteArrayToString(hash);
        }

/*        internal string HashString(string s)
        {
            byte[] hash = sha1.ComputeHash(ASCIIEncoding.ASCII.GetBytes(s));
            return ByteArrayToString(hash);
        }*/

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
