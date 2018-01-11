using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class TypeExtensions
    {
        public static object ConvertObject(this Type type, object obj)
        {
            if (type == typeof(string))
                return obj.ToString();
            if (type == typeof(short))
                return Convert.ToInt16(obj);
            if (type == typeof(int))
                return Convert.ToInt32(obj);
            if (type == typeof(long))
                return Convert.ToInt64(obj);
            if (type == typeof(bool))
                return Convert.ToBoolean(obj);
            if (type == typeof(decimal))
                return Convert.ToDecimal(obj);
            if (type == typeof(double))
                return Convert.ToDouble(obj);
            if (type == typeof(float))
                return Convert.ToDecimal(obj);
            throw new ArgumentException("Unsupported conversion type:" + type.FullName);
        }
    }
}
