using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    }
}
