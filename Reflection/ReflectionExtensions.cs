using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Reflection
{
    public static class ReflectionExtensions
    {
        public static Type GetPropertyOrFieldType(this MemberInfo memberType)
        {
            if(memberType is PropertyInfo)
                return (memberType as PropertyInfo).PropertyType;

            if (memberType is FieldInfo)
                return (memberType as FieldInfo).FieldType;

            throw new ArgumentException("memberType",
                $"Cannot determine type of {memberType.GetType().FullName} since it could not be casted to {typeof(PropertyInfo).Name} or {typeof(FieldInfo).Name}.");

        }
    }
}
