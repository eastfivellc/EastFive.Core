using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Net.Http
{
    public abstract class RefreshTokenMessageHandler : DelegatingHandler
    {
        public RefreshTokenMessageHandler(HttpMessageHandler innerHandler = default(HttpMessageHandler))
            : base()
        {
            this.InnerHandler = innerHandler.IsDefaultOrNull() ?
                new HttpClientHandler()
                :
                innerHandler;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = AccessToken;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await await RefreshTokenAsync(
                    (accessTokenNew) =>
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenNew);
                        return base.SendAsync(request, cancellationToken);
                    },
                    (why) => response.ToTask());
            }

            return response;
        }

        protected abstract string AccessToken { get; }

        protected abstract Task<TResult> RefreshTokenAsync<TResult>(
            Func<string, TResult> onSuccess,
            Func<string, TResult> onFailure);
    }
}
