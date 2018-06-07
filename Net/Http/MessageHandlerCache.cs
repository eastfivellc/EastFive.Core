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
    public abstract class MessageHandlerCache : DelegatingHandler
    {
        internal const string PropertyNoCache = "MessageHandlerCache.NoCache";
        internal const string PropertyValueNoCache = "true";

        public MessageHandlerCache(HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base()
        {
            if (innerHandler.IsDefaultOrNull())
                innerHandler = new HttpClientHandler();
            this.InnerHandler = innerHandler;
        }

        protected virtual HttpResponseMessage GenerateResponse(byte[] data,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            return GenerateResponse(new MemoryStream(data), statusCode);
        }

        protected virtual HttpResponseMessage GenerateResponse(System.IO.Stream data,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Content = new StreamContent(data);
            return response;
        }

        protected bool IsNoCache(HttpRequestMessage message)
        {
            return message.Properties.ContainsKey(PropertyNoCache) &&
                message.Properties[PropertyNoCache] == PropertyValueNoCache;
        }
    }
    
    public static class MessageHandlerCacheExtensions
    {
        public static void SetNoCaching(this HttpRequestMessage message)
        {
            message.Properties.Add(MessageHandlerCache.PropertyNoCache, MessageHandlerCache.PropertyValueNoCache);
        }
    }
}
