using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EastFive.Linq;

namespace EastFive.Serialization.Text
{
	public static class TextSerializationExtensions
	{
		public static IEnumerable<TResource> ParseCSV<TResource>(this Stream csvData,
            string scope, Stream[] extraStreams )
		{
            var filters = typeof(TResource)
                .GetAttributesInterface<IFilterText>()
                .Where(attrInter => attrInter.DoesFilter(scope))
                .ToArray();

            return typeof(TResource)
                .GetAttributesInterface<IMapText>()
                .Where(attrInter => attrInter.DoesParse(scope))
                .First<IMapText, IEnumerable<TResource>>(
                    (attrInter, next) =>
                    {
                        var resources = attrInter.Parse<TResource>(csvData, filters, extraStreams);
                        return resources;
                    },
                    () => throw new Exception("No matching scope."));
        }
	
	}
}

