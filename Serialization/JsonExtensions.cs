using System;

using Newtonsoft.Json;

using EastFive;
using EastFive.Extensions;

namespace EastFive.Serialization.Json
{
	public static class JsonExtensions
	{
        public static TResult JsonParse<TResource, TResult>(this string jsonData,
            Func<TResource, TResult> onSuccess,
            Func<string, TResult> onFailureToParse = default,
            Func<Exception, TResult> onException = default)
        {
            TResource resource = default;
            try
            {
                if (typeof(TResource) == typeof(string))
                    return onSuccess((TResource)(object)jsonData);

                resource = JsonConvert.DeserializeObject<TResource>(jsonData);
            }
            catch (JsonReaderException jsonEx)
            {
                if (onFailureToParse.IsNotDefaultOrNull())
                    return onFailureToParse(jsonEx.Message);

                throw;
            }
            catch(Exception ex)
            {
                if (onException.IsNotDefaultOrNull())
                    return onException(ex);
                
                if (onFailureToParse.IsNotDefaultOrNull())
                    return onFailureToParse(ex.Message);

                throw;
            }
            return onSuccess(resource);
        }

        public static TResult JsonSerialize<TResource, TResult>(this TResource resource,
            Func<string, TResult> onSuccess,
            Func<string, TResult> onFailureToParse = default)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(resource);
                return onSuccess(jsonData);
            }
            catch (JsonWriterException jsonEx)
            {
                if (onFailureToParse.IsNotDefaultOrNull())
                    return onFailureToParse(jsonEx.Message);

                throw new ArgumentException($"Failed to parse a `{typeof(TResource).FullName}` from the response.");
            }
        }
    }
}

