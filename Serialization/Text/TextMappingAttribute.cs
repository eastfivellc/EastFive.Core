using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualBasic.FileIO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EastFive.Linq;
using EastFive.Reflection;

namespace EastFive.Serialization.Text
{
    public class TextMappingAttribute : ScopedMapTextAttribute
    {
        public virtual (MemberInfo, IMapTextProperty)[] GetPropertyMappers<TResource>()
        {
            return typeof(TResource)
                .GetPropertyOrFieldMembers()
                .Select(
                    member =>
                    {
                        var matchingAttrs = member
                            .GetAttributesInterface<IMapTextProperty>()
                            .Where(attr => attr.DoesMap(this.Scope))
                            .ToArray();
                        return (member, matchingAttrs);
                    })
                .Where(tpl => tpl.matchingAttrs.Any())
                .Select(tpl => (tpl.member, attr: tpl.matchingAttrs.First()))
                .ToArray();
        }

        public override IEnumerable<TResource> Parse<TResource>(Stream csvData,
            IFilterText[] textFilters,
            params Stream[] csvDataJoins)
        {
            var membersAndMappers = GetPropertyMappers<TResource>();

            using (var parser = new TextFieldParser(csvData))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                var headers = parser
                    .ReadLine()
                    .Split(',');

                return Parse();

                IEnumerable<TResource> Parse()
                {
                    while (!parser.EndOfData)
                    {
                        TResource resource;
                        try
                        {
                            var fields = parser.ReadFields();
                            var values = headers.CollateSimple(fields).ToArray();

                            resource = ParseResource(membersAndMappers, values);
                        }
                        catch (Exception ex)
                        {
                            ex.GetType();
                            continue;
                        }
                        yield return resource;
                    }

                    TResource ParseResource(
                        (MemberInfo, IMapTextProperty)[] membersAndMappers,
                        (string key, string value)[] rowValues)
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
            }
        }
    }

    public class TextMapping2Attribute : TextMappingAttribute{ }
    public class TextMapping3Attribute : TextMappingAttribute { }
    public class TextMapping4Attribute : TextMappingAttribute { }
    public class TextMapping5Attribute : TextMappingAttribute { }
    public class TextMapping6Attribute : TextMappingAttribute { }
    public class TextMapping7Attribute : TextMappingAttribute { }
    public class TextMapping8Attribute : TextMappingAttribute { }
    public class TextMapping9Attribute : TextMappingAttribute { }
}

