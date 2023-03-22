using System;
using System.Reflection;

namespace EastFive.Serialization.Parquet
{
    public interface IMapParquetProperty
    {
        bool DoesMap(string scope);
        TResource ParseMemberValueFromRow<TResource>(TResource resource,
            MemberInfo member, (global::Parquet.Data.Field key, object value)[] rowValues);
    }
}

