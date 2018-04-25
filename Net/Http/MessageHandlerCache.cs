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
            return GenerateResponse(new MemoryStream(data));
        }

        protected virtual HttpResponseMessage GenerateResponse(System.IO.Stream data,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StreamContent(data);
            return response;
        }
    }
    
}
