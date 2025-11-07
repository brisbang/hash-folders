using System.Text;

namespace HashLib7
{
    internal class HashAlgorithm
    {
        private System.Security.Cryptography.SHA1 sha1;
        private static readonly object _mutex = new();
        private static HashAlgorithm _instance;

        public static HashAlgorithm GetInstance()
        {
            if (_instance == null)
            {
                lock (_mutex)
                {
                    _instance ??= new HashAlgorithm();
                }
            }
            return _instance;
        }

        public HashAlgorithm()
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
            StringBuilder hex = new(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
