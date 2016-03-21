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
        public static async Task<TResult> PostAsync<TResource, TResult>(this WebRequest webRequest, TResource resource,
            Func<HttpWebResponse, TResult> onSuccess, Func<HttpStatusCode, string, TResult> onFailure)
        {
            if (!(webRequest is HttpWebRequest))
                throw new ArgumentException("webRequest must be of type HttpWebRequest");
            var httpWebRequest = (HttpWebRequest)webRequest;

            var resourceJson = Newtonsoft.Json.JsonConvert.SerializeObject(resource);

            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(resourceJson);
                streamWriter.Flush();
            }
            try
            {
                var createAuthResponse = ((HttpWebResponse)(await httpWebRequest.GetResponseAsync()));
                return onSuccess(createAuthResponse);
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                var responseText = new System.IO.StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                return onFailure(httpResponse.StatusCode, responseText);
            }
        }

        public static async Task<TResult> GetAsync<TResource, TResult>(this WebRequest webRequest,
            Func<TResource, TResult> onSuccess, Func<HttpStatusCode, string, TResult> onFailure)
        {
            if (!(webRequest is HttpWebRequest))
                throw new ArgumentException("webRequest must be of type HttpWebRequest");
            var httpWebRequest = (HttpWebRequest)webRequest;
            
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "GET";
            try
            {
                var response = ((HttpWebResponse)(await httpWebRequest.GetResponseAsync()));
                var responseJson = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var resource = Newtonsoft.Json.JsonConvert.DeserializeObject<TResource>(responseJson);
                return onSuccess(resource);
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                var responseText = new System.IO.StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                return onFailure(httpResponse.StatusCode, responseText);
            }
            catch (Exception ex)
            {
                return onFailure(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
