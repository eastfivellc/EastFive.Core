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
    public class MessageHandlerCacheMemory : MessageHandlerCache
    {
        private static ConcurrentDictionary<string, KeyValuePair<DateTime, byte[]>> cache =
            new ConcurrentDictionary<string, KeyValuePair<DateTime, byte[]>>();

        public MessageHandlerCacheMemory(HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestUri = request.RequestUri.AbsoluteUri;
            
            if (!cache.ContainsKey(requestUri))
            {
                var response = await base.SendAsync(request, cancellationToken);
                var data = await response.Content.ReadAsByteArrayAsync();
                cache.AddOrUpdate(requestUri,
                        whenKey => DateTime.UtcNow.PairWithValue(data),
                        (k, v) => v);
                return GenerateResponse(data);
            }

            var index = cache[requestUri];
            return GenerateResponse(index.Value);
        }
    }
}
