using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EastFive.Linq;

namespace EastFive.Serialization.Text
{
	public static class TextSerializationExtensions
	{
		public static IEnumerable<TResource> ParseCSV<TResource>(this Stream csvData,
            string scope = default, Stream[] extraStreams = default)
		{
            var filters = typeof(TResource)
                .GetAttributesInterface<IFilterText>()
                .If(scope.HasBlackSpace(),
                    items => items.Where(attrInter => attrInter.DoesFilter(scope)))
                .ToArray();

            return typeof(TResource)
                .GetAttributesInterface<IMapText>()
                .If(scope.HasBlackSpace(),
                    items => items.Where(attrInter => attrInter.DoesParse(scope)))
                .First<IMapText, IEnumerable<TResource>>(
                    (attrInter, next) =>
                    {
                        var resources = attrInter.Parse<TResource>(csvData, filters, extraStreams);
                        return resources;
                    },
                    () => throw new Exception($"{typeof(TResource).FullName} does not have any attributes implementing {typeof(IMapText).FullName}."));
        }
    }
}

