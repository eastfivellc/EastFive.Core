using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Net.Http
{
    public abstract class ThrottleMessageHandler : DelegatingHandler
    {
        private static AutoResetEvent rateLock = new AutoResetEvent(true);

        public ThrottleMessageHandler(HttpClientHandler innerHandler = default(HttpClientHandler))
            : base()
        {
            if(innerHandler.IsDefaultOrNull())
                innerHandler = new HttpClientHandler();
            this.InnerHandler = innerHandler;
        }

        protected abstract TimeSpan ComputeDelay(HttpRequestMessage request);

        protected abstract void UpdateDelay(HttpResponseMessage response);

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await await Task.Run<Task<HttpResponseMessage>>(
                async () =>
                {
                    var threadName = request.RequestUri.AbsoluteUri;
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] requesting mutex");
                        rateLock.WaitOne();
                        System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] inside mutex");

                        var delay = ComputeDelay(request);

                        if (delay.Seconds > 0)
                            Thread.Sleep(delay);

                        var response = await base.SendAsync(request, cancellationToken);
                        UpdateDelay(response);

                        if (((int)response.StatusCode) != 429)
                        {
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] leaving mutex");
                        rateLock.Set();
                        System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] exited mutex");
                    }
                    return await SendAsync(request, cancellationToken);
                }).ConfigureAwait(false);
        }
    }
}
