using System;
using System.Collections.Generic;
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
        public static async Task<TResult> HttpClientGetResourceAsync<TResource, TResult>(this Uri location,
            Func<TResource, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default,
            Func<HttpRequestMessage, HttpRequestMessage> mutateRequest = default)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, location);
                if (mutateRequest.IsNotDefaultOrNull())
                    request = mutateRequest(request);
                try
                {
                    return await await httpClient
                        .SendAsync(request)
                        .IsSuccessStatusCodeAsync(
                            responseSuccess => ParseResourceResponse(responseSuccess,
                                onSuccess: onSuccess,
                                onFailure: onFailure,
                                onFailureWithBody: onFailureWithBody,
                                onResponseFailure: onResponseFailure,
                                onFailureToParse: onFailureToParse),
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

        public static async Task<TResult> HttpClientPostResourceAsync<TResource, TResponse, TResult>(this Uri location, TResource resource,
            Func<TResponse, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default,
            Func<HttpRequestMessage, HttpRequestMessage> mutateRequest = default)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, location))
                {
                    var json = JsonConvert.SerializeObject(resource,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore,
                        });
                    using (var content = new StringContent(json,
                        encoding: System.Text.Encoding.UTF8, "application/json"))
                    {
                        var requestToSend = request;
                        requestToSend.Content = content;
                        if (mutateRequest.IsNotDefaultOrNull())
                            requestToSend = mutateRequest(requestToSend);
                        try
                        {
                            return await await httpClient
                                .SendAsync(requestToSend)
                                .IsSuccessStatusCodeAsync(
                                    responseSuccess => ParseResourceResponse<TResponse, TResult>(responseSuccess,
                                        onSuccess: onSuccess,
                                        onFailure: onFailure,
                                        onFailureWithBody: onFailureWithBody,
                                        onResponseFailure: onResponseFailure,
                                        onFailureToParse: onFailureToParse),
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
                                            return onFailure($"Server returned error code:{responseFailure.StatusCode}");

                                        throw new ArgumentException($"Server returned error code:{responseFailure.StatusCode}");
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
            }
        }

        public static async Task<TResult> HttpClientPutResourceAsync<TResource, TResponseResource, TResult>(this Uri location, TResource resource,
            Func<TResponseResource, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default,
            Func<HttpRequestMessage, HttpRequestMessage> mutateRequest = default)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Put, location))
                {
                    var json = JsonConvert.SerializeObject(resource,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore,
                        });
                    using (var content = new StringContent(json,
                        encoding: System.Text.Encoding.UTF8, "application/json"))
                    {
                        var requestToSend = request;
                        requestToSend.Content = content;

                        if (mutateRequest.IsNotDefaultOrNull())
                            requestToSend = mutateRequest(requestToSend);
                        try
                        {
                            return await await httpClient
                                .SendAsync(request)
                                .IsSuccessStatusCodeAsync(
                                    responseSuccess => ParseResourceResponse(responseSuccess,
                                        onSuccess: onSuccess,
                                        onFailure: onFailure,
                                        onFailureWithBody: onFailureWithBody,
                                        onResponseFailure: onResponseFailure,
                                        onFailureToParse: onFailureToParse),
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
            }
        }

        public static async Task<TResult> ParseResourceResponse<TResource, TResult>(
                HttpResponseMessage responseSuccess,
            Func<TResource, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default)
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
        }

        public static async Task<TResult> HttpPostFormUrlEncodedContentAsync<TResource, TResult>(this Uri location,
                IDictionary<string, string> postValues,
            Func<TResource, TResult> onSuccess,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, string, TResult> onFailureWithBody = default,
            Func<HttpStatusCode, Func<Task<string>>, TResult> onResponseFailure = default,
            Func<string, string, TResult> onFailureToParse = default)
        {
            using (var httpClient = new HttpClient())
            {
                using (var body = new FormUrlEncodedContent(postValues))
                {
                    try
                    {
                        return await await httpClient.PostAsync(location, body)
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

