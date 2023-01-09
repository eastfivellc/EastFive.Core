using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using EastFive.Linq;
using EastFive.Linq.Async;

namespace EastFive.Serialization.Parquet
{
	public static class ParquetSerializationExtensions
	{
		public static TResource[] ParseParquet<TResource>(this Stream parquetData,
            string scope, Stream[] extraStreams )
		{
            var filters = typeof(TResource)
                .GetAttributesInterface<IFilterParquet>()
                .Where(attrInter => attrInter.DoesFilter(scope))
                .ToArray();

            return typeof(TResource)
                .GetAttributesInterface<IMapParquet>()
                .Where(attrInter => attrInter.DoesParse(scope))
                .First<IMapParquet, TResource[]>(
                    (attrInter, next) =>
                    {
                        var resources = attrInter.Parse<TResource>(parquetData, filters, extraStreams);
                        return resources;
                    },
                    () => throw new Exception("No matching scope."));
        }
	
	}
}

