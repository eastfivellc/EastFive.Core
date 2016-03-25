using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core.Web
{
    public static class WebRequestExtensions
    {
        public static TResult GetRequest<TResult, TResource>(this HttpWebRequest httpWebRequest,
            TResource resource,
            Func<TResult> onSuccess, Func<WebException, TResult> onFailure)
        {
            try
            {
                httpWebRequest.ContentType = "application/json";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    var resourceJson = Newtonsoft.Json.JsonConvert.SerializeObject(resource);
                    streamWriter.Write(resourceJson);
                    streamWriter.Flush();
                }
                return onSuccess();
            }
            catch (WebException ex)
            {
                return onFailure(ex);
            }
        }

        public static async Task<TResult> GetResponseAsync<TResult, TResource>(this HttpWebRequest httpWebRequest,
            Func<HttpWebResponse, TResult> onSuccess, Func<HttpStatusCode, string, TResult> onFailure)
        {
            try
            {
                using (var createAuthResponse = ((HttpWebResponse)(await httpWebRequest.GetResponseAsync())))
                {
                    return onSuccess(createAuthResponse);
                }
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                var responseText = new System.IO.StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                return onFailure(httpResponse.StatusCode, responseText);
            }
        }

        public static HttpWebRequest AsHttpWebRequest(this WebRequest webRequest, string method)
        {
            if (!(webRequest is HttpWebRequest))
                throw new ArgumentException("webRequest must be of type HttpWebRequest");
            var httpWebRequest = (HttpWebRequest)webRequest;

            httpWebRequest.Method = "POST";
            return httpWebRequest;
        }

        public static Task<TResult> PostAsync<TResource, TResult>(this WebRequest webRequest, TResource resource,
            Func<HttpWebResponse, TResult> onSuccess, Func<HttpStatusCode, string, TResult> onWebFailure, Func<string, TResult> onFailure)
        {
            var httpWebRequest = webRequest.AsHttpWebRequest("POST");
            return httpWebRequest
                .GetRequest<Task<TResult>, TResource>(resource,
                    () => httpWebRequest.GetResponseAsync<TResult, TResource>(onSuccess, onWebFailure),
                    (webEx) => Task.FromResult(onFailure(webEx.Message)));
        }

        public static async Task<TResult> GetAsync<TResource, TResult>(this WebRequest webRequest,
            Func<TResource, TResult> onSuccess, Func<HttpStatusCode, string, TResult> onFailure)
        {
            var httpWebRequest = webRequest.AsHttpWebRequest("GET");
            httpWebRequest.ContentType = "application/json";
            return await httpWebRequest.GetResponseAsync<TResult, TResource>(
                (response) =>
                {
                    try
                    {
                        var responseJson = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        var resource = Newtonsoft.Json.JsonConvert.DeserializeObject<TResource>(responseJson);
                        return onSuccess(resource);
                    }
                    catch (Exception ex)
                    {
                        return onFailure(HttpStatusCode.InternalServerError, ex.Message);
                    }
                },
                onFailure);
        }

        public static Task<TResult> DeleteAsync<TResource, TResult>(this WebRequest webRequest,
            TResource resource,
            Func<HttpWebResponse, TResult> onSuccess,
            Func<HttpStatusCode, string, TResult> onWebFailure,
            Func<string, TResult> onFailure)
        {
            var httpWebRequest = webRequest.AsHttpWebRequest("DELETE");
            return httpWebRequest
                .GetRequest<Task<TResult>, TResource>(resource,
                    () => httpWebRequest.GetResponseAsync<TResult, TResource>(onSuccess, onWebFailure),
                    (webEx) => Task.FromResult(onFailure(webEx.Message)));
        }
    }
}
