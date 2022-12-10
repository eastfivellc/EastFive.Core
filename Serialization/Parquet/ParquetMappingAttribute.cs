using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EastFive.Linq;
using EastFive.Reflection;
using EastFive.Linq.Async;
using System.Threading.Tasks;

namespace EastFive.Serialization.Parquet
{
    public class ParquetMappingAttribute : ScopedMapParquetAttribute
    {
        public virtual (MemberInfo, IMapParquetProperty)[] GetPropertyMappers<TResource>()
        {
            return typeof(TResource)
                .GetPropertyOrFieldMembers()
                .Select(
                    member =>
                    {
                        var matchingAttrs = member
                            .GetAttributesInterface<IMapParquetProperty>()
                            .Where(attr => attr.DoesMap(this.Scope))
                            .ToArray();
                        return (member, matchingAttrs);
                    })
                .Where(tpl => tpl.matchingAttrs.Any())
                .Select(tpl => (tpl.member, attr: tpl.matchingAttrs.First()))
                .ToArray();
        }

        public override IEnumerableAsync<TResource> Parse<TResource>(Stream parquetData,
            IFilterParquet[] textFilters,
            params Stream[] parquetDataJoins)
        {
            return ParseAsync<TResource>(parquetData, textFilters, parquetDataJoins)
                .FoldTask();
        }

        private async Task<IEnumerable<TResource>> ParseAsync<TResource>(Stream parquetData,
            IFilterParquet[] textFilters,
            params Stream[] parquetDataJoins)
        {
            var membersAndMappers = GetPropertyMappers<TResource>();

            var table = await global::Parquet.ParquetReader.ReadTableFromStreamAsync(parquetData);
            var headers = table.Schema.Fields;

            return Iterate();

            IEnumerable<TResource> Iterate()
            {
                foreach (var row in table)
                {
                    TResource resource;
                    try
                    {
                        var fields = row.Values;
                        var properties = headers.CollateSimple(fields).ToArray();

                        resource = ParseResource(membersAndMappers, properties);

                        TResource ParseResource(
                            (MemberInfo, IMapParquetProperty)[] membersAndMappers,
                            (global::Parquet.Data.Field key, object value)[] rowValues)
                        {
                            var resource = Activator.CreateInstance<TResource>();
                            return membersAndMappers
                                .Aggregate(resource,
                                    (resource, memberAndMapper) =>
                                    {
                                        var (member, mapper) = memberAndMapper;
                                        return mapper.ParseRow(resource, member, rowValues);
                                    });
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetType();
                        continue;
                    }
                    yield return resource;
                }
            }
        }
    }

    public class ParquetMapping2Attribute : ParquetMappingAttribute { }
    public class ParquetMapping3Attribute : ParquetMappingAttribute { }
    public class ParquetMapping4Attribute : ParquetMappingAttribute { }
    public class ParquetMapping5Attribute : ParquetMappingAttribute { }
    public class ParquetMapping6Attribute : ParquetMappingAttribute { }
    public class ParquetMapping7Attribute : ParquetMappingAttribute { }
    public class ParquetMapping8Attribute : ParquetMappingAttribute { }
    public class ParquetMapping9Attribute : ParquetMappingAttribute { }
}

