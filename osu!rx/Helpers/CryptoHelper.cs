using System.Globalization;
using System.Security.Cryptography;

namespace osu_rx.Helpers
{
    public class CryptoHelper
    {
        private static readonly NumberFormatInfo numberFormat = new CultureInfo(@"en-US", false).NumberFormat;
        private static MD5 md5 = MD5.Create();

        public static string GetMD5String(byte[] data)
        {
            lock (md5)
                data = md5.ComputeHash(data);

            char[] str = new char[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
                data[i].ToString("x2", numberFormat).CopyTo(0, str, i * 2, 2);

            return new string(str);
        }
    }
}
