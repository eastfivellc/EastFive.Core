﻿using BlackBarLabs.Extensions;
using EastFive;
using EastFive.Extensions;
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
            using (var algorithm = new MD5CryptoServiceProvider())
            {
                var hash = algorithm.ComputeHash(stream);
                var result = hash.Select(hex => hex.ToString("X2"))
                    .Join("")
                    .ToUpper();
                return result;
            }
        }

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

        public static string ReadAsString(this Stream stream, 
            Encoding encoding = default(Encoding))
        {
            if (encoding.IsDefault())
                encoding = Encoding.UTF8;
            using (var reader = new StreamReader(stream, encoding))
            {
                string value = reader.ReadToEnd();
                return value;
            }
        }
    }
}
