using BlackBarLabs.Extensions;
using EastFive;
using EastFive.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class StreamExtensions
    {
        public static async Task<byte []> ToBytesAsync(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
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

        /// <summary>
        /// This method is ideally used to create a unique string.  WARNING: Using this method to protect sensitive information would be a security vulnerability due to it using a weak hashing function.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string Md5Checksum(this Stream stream)
        {
            using (var algorithm = new MD5CryptoServiceProvider())
            {
                var hash = algorithm.ComputeHash(stream);
                var result = hash.Select(hex => hex.ToString("X2"))
                    .Join("")
                    .ToUpper();
                return result;
            }
        }


        /// <summary>
        /// This method is ideally used to create a unique string.  WARNING: Using this method to protect sensitive information would be a security vulnerability due to it using a weak hashing function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string Md5Checksum(this byte[] bytes)
        {
            using (var algorithm = new MD5CryptoServiceProvider())
            {
                var hash = algorithm.ComputeHash(bytes);
                var result = hash.Select(hex => hex.ToString("X2"))
                    .Join("")
                    .ToUpper();
                return result;
            }
        }

        public static Task<string> ReadAsStringAsync(this Stream stream, 
            Encoding encoding = default(Encoding))
        {
            if (encoding.IsDefault())
                encoding = Encoding.UTF8;
            using (var reader = new StreamReader(stream, encoding))
            {
                var value = reader.ReadToEndAsync();
                return value;
            }
        }
    }
}
