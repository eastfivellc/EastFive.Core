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

        public ThrottleMessageHandler(HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base()
        {
            if(innerHandler.IsDefaultOrNull())
                innerHandler = new HttpClientHandler();
            this.InnerHandler = innerHandler;
        }

        protected abstract TimeSpan ComputeDelay(HttpRequestMessage request);

        protected abstract void UpdateDelay(HttpResponseMessage response, bool timeout);

        protected virtual bool ShouldRateLock(HttpRequestMessage request)
        {
            return true;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ct = new CancellationToken();
            return await await Task.Run<Task<HttpResponseMessage>>(
                async () =>
                {
                    var threadName = request.RequestUri.AbsoluteUri;
                    var didRateLock = ShouldRateLock(request);
                    if (didRateLock)
                    {
                        System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] requesting mutex");
                        rateLock.WaitOne();
                    }
                    var delay = ComputeDelay(request);

                    if (delay.Seconds > 0)
                        Thread.Sleep(delay);

                    System.Diagnostics.Debug.WriteLine((didRateLock ? "SYNC:" : "PARALLEL") + $"Request [{threadName}]");
                    HttpResponseMessage response = default(HttpResponseMessage);
                    bool didTimeout = false;
                    try
                    {
                        response = await base.SendAsync(request, ct);
                        System.Diagnostics.Debug.WriteLine((didRateLock ? "SYNC:" : "PARALLEL") + $"Response [{threadName}]");
                        if (!IsOverage(response))
                        {
                            return response;
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        if (ex.CancellationToken.IsCancellationRequested)
                            throw ex;

                        // This is normally just a timeout
                        didTimeout = true;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        UpdateDelay(response, didTimeout);
                        if (didRateLock)
                        {
                            System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] leaving mutex");
                            rateLock.Set();
                            System.Diagnostics.Debug.WriteLine($"Thread [{threadName}] exited mutex");
                        }
                    }
                    return await SendAsync(request, ct);
                }).ConfigureAwait(false);
        }

        protected virtual bool IsOverage(HttpResponseMessage response)
        {
            return (((int)response.StatusCode) == 429);
        }
    }
}
