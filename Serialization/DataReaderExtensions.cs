using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

using EastFive.Serialization.DataReader;
using Newtonsoft.Json;
using System.Dynamic;

namespace EastFive.Serialization
{
	public static class DataReaderExtensions
	{
        public delegate bool TryParseTextValueDelegate(Type type, object value, out string parsedValue);

		public static IEnumerable<string[]> ReadAsTextValues(this IDataReader dataReader,
            TryParseTextValueDelegate tryParseValue)
		{
            if (!dataReader.Read())
                yield break;

            var fieldCount = dataReader.FieldCount;
            var headerRow = Enumerable
                .Range(0, fieldCount)
                .Select(
                    index =>
                    {
                        var column = dataReader.GetName(index);
                        return column;
                    })
                .ToArray();

            yield return headerRow;

            while (dataReader.Read())
            {
                string[] populatedRow = default;
                try
                {
                    populatedRow = Enumerable
                        .Range(0, fieldCount)
                        .Select(
                            index =>
                            {
                                var value = dataReader.GetValue(index);
                                var type = dataReader.GetFieldType(index);
                                if (tryParseValue(type, value, out var parsedValue))
                                    return parsedValue;

                                return value.ToString();
                            })
                        .ToArray();
                }
                catch (Exception ex)
                {
                    ex.GetType();
                    continue;
                }
                yield return populatedRow;
            }
        }

        public static IEnumerable<string> ReadAsJsonRows(this IDataReader dataReader)
        {
            var fieldCount = dataReader.FieldCount;

            while (dataReader.Read())
            {
                dynamic myobject = new ExpandoObject();
                try
                {
                    var myUnderlyingObject = Enumerable
                        .Range(0, fieldCount)
                        .Aggregate((IDictionary<string, object>)myobject,
                            (myUnderlyingObject, index) =>
                            {
                                var name = dataReader.GetName(index);
                                var value = dataReader.GetValue(index);
                                var type = dataReader.GetFieldType(index);
                                myUnderlyingObject.Add(name, value); // Adding dynamically named property

                                return myUnderlyingObject;
                            })
                        .ToArray();
                }
                catch (Exception ex)
                {
                    ex.GetType();
                    continue;
                }

                var jsonString = JsonConvert.SerializeObject(myobject);
                yield return jsonString;
            }
        }
    }
}

