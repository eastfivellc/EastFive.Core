using BlackBarLabs.Extensions;
using EastFive;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EastFive
{
    public static class StreamExtensions
    {
        public static byte [] ToBytes(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                return bytes;
            }
        }

        public static TResult ToBytes<TResult>(this Stream stream,
            Func<byte [], TResult> success)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                return success(bytes);
            }
        }

        public static string Md5Checksum(this Stream stream)
        {
            var hash = new MD5CryptoServiceProvider().ComputeHash(stream);
            var result = hash.Select(hex => hex.ToString("X2"))
                .Join("")
                .ToUpper();
            return result;
        }

        public static string ReadAsString(this Stream stream, 
            Encoding encoding = default(Encoding))
        {
            if (encoding.IsDefault())
                encoding = Encoding.UTF8;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                return value;
            }
        }
    }
}
