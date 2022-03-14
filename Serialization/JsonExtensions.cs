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
            Func<string, TResult> onFailureToParse = default)
        {
            try
            {
                if (typeof(TResource) == typeof(string))
                    return onSuccess((TResource)(object)jsonData);

                var resource = JsonConvert.DeserializeObject<TResource>(jsonData);
                return onSuccess(resource);
            }
            catch (JsonReaderException jsonEx)
            {
                if (onFailureToParse.IsNotDefaultOrNull())
                    return onFailureToParse(jsonEx.Message);

                throw new ArgumentException($"Failed to parse a `{typeof(TResource).FullName}` from the response.");
            }
        }
    }
}

