using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlackBarLabs
{
    public static class RegexExtensions
    {
        public static IEnumerable<Match> AsMatches(this MatchCollection collection)
        {
            foreach (Match match in collection)
                yield return match;
        }
    }
}
