using System.Security.Cryptography;

namespace Sample
{
    public class Violations
    {
        private int count;

        public int Next()
        {
            count = count
                + 1;
            return count;
        }

        public byte[] Hash(byte[] data) => MD5.HashData(data);

        public int Year() => DateTime.UtcNow.Year;

        public int FirstArgumentLength()
        {
            var first = Environment.GetCommandLineArgs().FirstOrDefault();
            return first.Length;
        }

        public int Sum(int first, int second, int third, int fourth, int fifth, int sixth, int seventh) => first + second;
    }
}
