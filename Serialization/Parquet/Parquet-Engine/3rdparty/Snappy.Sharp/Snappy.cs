﻿using System;
using System.Linq;
using System.IO;

namespace Snappy.Sharp
{
   static class Snappy
   {
      internal const int LITERAL = 0;
      internal const int COPY_1_BYTE_OFFSET = 1; // 3 bit length + 3 bits of offset in opcode
      internal const int COPY_2_BYTE_OFFSET = 2;
      internal const int COPY_4_BYTE_OFFSET = 3;

      public static int MaxCompressedLength(int sourceLength)
      {
         var compressor = new SnappyCompressor();
         return compressor.MaxCompressedLength(sourceLength);
      }

      public static byte[] Compress(byte[] uncompressed)
      {
         var target = new SnappyCompressor();
         byte[] result = new byte[target.MaxCompressedLength(uncompressed.Length)];
         int count = target.Compress(uncompressed, 0, uncompressed.Length, result);
         return result.Take(count).ToArray();
      }

      public static int GetUncompressedLength(byte[] compressed, int offset = 0)
      {
         var decompressor = new SnappyDecompressor();
         return decompressor.ReadUncompressedLength(compressed, offset)[0];
      }
   }
}