using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EastFive;

namespace EastFive.Net.Http
{
    public class UseCacheAlwaysMessageHandler : CacheMessageHandler
    {
        public UseCacheAlwaysMessageHandler(IProvideCache cacheProvider,
            HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base(cacheProvider, innerHandler)
        {
        }

        protected override bool UseCache(DateTime when)
        {
            return true;
        }
    }
}
