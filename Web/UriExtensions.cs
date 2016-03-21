using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core
{
    public static class UriExtensions
    {
        public static Uri ToUri(this string uriString)
        {
            return new Uri(uriString);
        }
    }
}
