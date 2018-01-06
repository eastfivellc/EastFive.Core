using BlackBarLabs.Extensions;
using BlackBarLabs.Linq;
using EastFive.Collections.Generic;
using EastFive.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        public static IEnumerable<Group> AsGroups(this GroupCollection collection)
        {
            foreach (Group group in collection)
                yield return group;
        }

        public static IEnumerable<Capture> AsCaptures(this CaptureCollection collection)
        {
            foreach (Group capture in collection)
                yield return capture;
        }
        
        public static T[] MatchRegex<T>(this string input, string regularExpression,
            params Expression<Func<T, object>>[] expressions)
        {
            var expressionLookup = expressions
                .Select(
                    exp => exp.MemberName(
                        (name) =>
                        {
                            var members = typeof(T).GetProperties()
                                .Cast<MemberInfo>()
                                .Concat(typeof(T).GetFields()).ToArray();
                            var propInfo = members.First(
                                prop => prop.Name == name);
                            return name.PairWithValue(propInfo);
                        },
                        () =>
                        {
                            // TODO: Warn here?
                            //throw new ArgumentException($"The expression '{exp.Body}' is invalid. You must supply an expression that references a property.");
                            return default(KeyValuePair<string, MemberInfo>?);
                        }))
                .SelectWhereHasValue()
                .ToDictionary();

            var regex = new Regex(regularExpression);
            var results = regex
                .Matches(input)
                .AsMatches()
                .Where(match => match.Success && match.Groups.Count == (expressions.Length + 1))
                .Select(
                    (match) =>
                    {
                        var groups = match.Groups.AsGroups().Skip(1).ToArray();

                        var assignmentCollection = expressionLookup
                            .Select(
                                expKvp =>
                                {
                                    var matchingGroups = groups
                                        .Where(
                                            group =>
                                            {
                                                var groupName = (string)((dynamic)group).Name;
                                                return groupName == expKvp.Key;
                                            });
                                    if (!matchingGroups.Any())
                                        return default(KeyValuePair<MemberInfo, string>?);

                                    return expKvp.Value.PairWithValue(matchingGroups.First().Value);
                                })
                            .SelectWhereHasValue()
                            .ToArray();
                        
                        return assignmentCollection;
                    })
                .Where(assignmentCollection => assignmentCollection.Length == expressionLookup.Count)
                .Select(
                    (assignmentCollection) =>
                    {
                        return assignmentCollection.Aggregate(Activator.CreateInstance<T>(),
                            (r, assignment) =>
                            {
                                assignment.Key.SetValue(ref r, assignment.Value);
                                return r;
                            });
                    })
                .ToArray();

            return results;
        }
        
    }
}
