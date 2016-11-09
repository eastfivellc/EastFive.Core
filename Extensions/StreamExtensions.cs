using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core
{
    public static class StreamExtensions
    {
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
                var sb = new StringBuilder(32);
                foreach (var hex in hash)
                    sb.Append(hex.ToString("X2"));
                return sb.ToString().ToUpper();
        }
    }
}
