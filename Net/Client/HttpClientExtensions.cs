using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using EastFive;
using EastFive.Extensions;
using Newtonsoft.Json;

namespace EastFive.Net
{
    public static class HttpClientExtensions
    { 
        public static async Task<TResult> HttpClientGetResource<TResource, TResult>(this Uri location,
            Func<TResource, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, location);
                try
                {
                    return await await httpClient
                        .SendAsync(request)
                        .IsSuccessStatusCodeAsync(
                            async responseSuccess =>
                            {
                                var content = await responseSuccess.Content.ReadAsStringAsync();
                                try
                                {
                                    var resource = JsonConvert.DeserializeObject<TResource>(content);
                                    return onSuccess(resource);
                                }
                                catch (Newtonsoft.Json.JsonReaderException jsonEx)
                                {
                                    if (onFailureToParse.IsNotDefaultOrNull())
                                        return onFailureToParse(jsonEx.Message, content);

                                    if (onFailureWithBody.IsNotDefaultOrNull())
                                        return onFailureWithBody(responseSuccess.StatusCode, content);

                                    if (onResponseFailure.IsNotDefaultOrNull())
                                        return onResponseFailure(responseSuccess.StatusCode, () => content.AsTask());

                                    if (onFailure.IsNotDefaultOrNull())
                                        return onFailure($"Server returned arror code:{responseSuccess.StatusCode}");

                                    throw new ArgumentException($"Failed to parse a `{typeof(TResource).FullName}` from the response.");
                                }
                            },
                            async responseFailure =>
                            {
                                if (onFailureWithBody.IsNotDefaultOrNull())
                                {
                                    var content = await responseFailure.Content.ReadAsStringAsync();
                                    return onFailureWithBody(responseFailure.StatusCode, content);
                                }

                                if (onResponseFailure.IsNotDefaultOrNull())
                                    return onResponseFailure(responseFailure.StatusCode,
                                        () => responseFailure.Content.ReadAsStringAsync());

                                if (onFailure.IsNotDefaultOrNull())
                                    return onFailure($"Server returned arror code:{responseFailure.StatusCode}");

                                throw new ArgumentException($"Server returned arror code:{responseFailure.StatusCode}");
                            });
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    if (onFailure.IsNotDefaultOrNull())
                        return onFailure($"Connection Failure: {ex.GetType().FullName}:{ex.Message}");

                    throw;
                }
                catch (Exception exGeneral)
                {
                    if (onFailure.IsNotDefaultOrNull())
                        return onFailure(exGeneral.Message);

                    throw;
                }
            }
        }

        public static async Task<TResult> IsSuccessStatusCodeAsync<TResult>(this Task<HttpResponseMessage> responseFetching,
            Func<HttpResponseMessage, TResult> onSuccessCode,
            Func<HttpResponseMessage, TResult> onFailureCode)
        {
            var response = await responseFetching;
            return response.IsSuccessStatusCode(
                onSuccessCode: (status) => onSuccessCode(response),
                onFailureCode: (status) => onFailureCode(response));
        }

        public static TResult IsSuccessStatusCode<TResult>(this HttpResponseMessage response,
            Func<HttpStatusCode, TResult> onSuccessCode,
            Func<HttpStatusCode, TResult> onFailureCode) => response.IsSuccessStatusCode ?
                onSuccessCode(response.StatusCode)
                :
                onFailureCode(response.StatusCode);
    }
}

