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
    public abstract class CacheMessageHandler : DelegatingHandler
    {
        public interface IProvideCache
        {
            Task<TResult> FindAsync<TResult>(string requestUri,
                Func<System.IO.Stream, DateTime, Func<System.IO.Stream, DateTime, Task>, Task<TResult>> onCached,
                Func<Func<System.IO.Stream, DateTime, Task>, Task<TResult>> onNotCached);
        }

        private IProvideCache cacheProvider;

        public CacheMessageHandler(IProvideCache cacheProvider, HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base()
        {
            if(innerHandler.IsDefaultOrNull())
                innerHandler = new HttpClientHandler();
            this.InnerHandler = innerHandler;
            this.cacheProvider = cacheProvider;
        }

        protected abstract bool UseCache(DateTime when);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Func<HttpResponseMessage, Func<System.IO.Stream, DateTime, Task>, Task<HttpResponseMessage>> onUpdateCache =
                async (response, updateAsync) =>
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var data = stream.ToBytes(
                        bytes => new MemoryStream(bytes));
                    await updateAsync(data, DateTime.UtcNow);
                    data.Position = 0;
                    var responseNew = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    responseNew.Content = new StreamContent(data);
                    return responseNew;
                };

            return cacheProvider.FindAsync(request.RequestUri.AbsoluteUri,
                async (data, when, updateAsync) =>
                {
                    if (!UseCache(when))
                    {
                        var responseLive = await base.SendAsync(request, cancellationToken);
                        return await onUpdateCache(responseLive, updateAsync);
                    }

                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    response.Content = new StreamContent(data);
                    return response;
                },
                async (createAsync) =>
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    return await onUpdateCache(response, createAsync);
                });
        }
    }

    public class MemoryCacheProvider : CacheMessageHandler.IProvideCache
    {
        private static ConcurrentDictionary<string, KeyValuePair<DateTime, byte[]>> cache =
            new ConcurrentDictionary<string, KeyValuePair<DateTime, byte[]>>();

        public Task<TResult> FindAsync<TResult>(string requestUri, 
            Func<Stream, DateTime, Func<Stream, DateTime, Task>, Task<TResult>> onCached,
            Func<Func<Stream, DateTime, Task>, Task<TResult>> onNotCached)
        {
            Func<Stream, DateTime, Task> onUpdate =
                async (stream, when) =>
                {
                    cache.AddOrUpdate(requestUri,
                        whenKey => stream.ToBytes(
                            bytes => when.PairWithValue(bytes)),
                        (k, v) => v);
                    await 1.ToTask();
                };

            if (!cache.ContainsKey(requestUri))
                return onNotCached(onUpdate);

            var index = cache[requestUri];
            return onCached(
                new MemoryStream(index.Value), index.Key, onUpdate);
        }
    }
}
