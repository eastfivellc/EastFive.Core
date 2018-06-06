using BlackBarLabs.Extensions;
using EastFive.Collections.Generic;
using EastFive.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EastFive.Linq.Async;
using EastFive.Linq;

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

        public static TResult MatchRegex<T, TResult>(this string input, string regularExpression,
            Func<T, TResult> onMatched,
            Func<TResult> onNotMatched,
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
            var matches = regex
                .Matches(input)
                .AsMatches();
            var assignments = matches
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
                                            groupMaybe =>
                                            {
                                                if (!(groupMaybe is System.Text.RegularExpressions.Group))
                                                    return false;
                                                var group = groupMaybe as System.Text.RegularExpressions.Group;
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
                .ToArray();
            var results = assignments
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

            return results.Any()? onMatched(results.First()) : onNotMatched();
        }
        
        public static TResult MatchRegexInvoke<T, TResult>(this string input, string regularExpression,
            Expression<Func<string, T>> expression,
            Func<T[], TResult> onMatched)
        {
            var exec = expression.Compile();
            return input.MatchRegexInvokeDynamic(regularExpression,
                onMatched,
                expression.Parameters.ToArray(),
                invokeArgs => (T)exec.Invoke(invokeArgs[0]));
        }

        public static TResult MatchRegexInvoke<T, TResult>(this string input, string regularExpression,
            Expression<Func<string, string, T>> expression,
            Func<T[], TResult> onMatched)
        {
            var exec = expression.Compile();
            return input.MatchRegexInvokeDynamic(regularExpression,
                onMatched,
                expression.Parameters.ToArray(),
                invokeArgs => (T)exec.Invoke(invokeArgs[0], invokeArgs[1]));
        }

        public static TResult MatchRegexInvoke<T, TResult>(this string input, string regularExpression,
            Expression<Func<string, string, string, T>> expression,
            Func<T[], TResult> onMatched)
        {
            var exec = expression.Compile();
            return input.MatchRegexInvokeDynamic(regularExpression,
                onMatched,
                expression.Parameters.ToArray(),
                invokeArgs => (T)exec.Invoke(invokeArgs[0], invokeArgs[1], invokeArgs[2]));
        }

        private static TResult MatchRegexInvokeDynamic<T, TResult>(this string input, string regularExpression,
            Func<T[], TResult> onMatched,
            ParameterExpression [] parameterSet,
            Func<string[], T> invoker)
        {
            var regex = new Regex(regularExpression);
            var matches = regex
                .Matches(input)
                .AsMatches();
            var parameterAndValuess = matches
                .Where(match => match.Success && match.Groups.Count == (parameterSet.Length + 1))
                .Select(
                    (match) =>
                    {
                        var groups = match.Groups.AsGroups().Skip(1).ToArray();

                        var assignmentCollection = parameterSet
                            .Select(
                                parameter =>
                                {
                                    var matchingGroups = groups
                                        .Where(
                                            groupMaybe =>
                                            {
                                                if (!(groupMaybe is System.Text.RegularExpressions.Group))
                                                    return false;
                                                var group = groupMaybe as System.Text.RegularExpressions.Group;
                                                var groupName = (string)((dynamic)group).Name;
                                                return groupName == parameter.Name;
                                            });
                                    if (!matchingGroups.Any())
                                        return default(KeyValuePair<ParameterExpression, string>?);

                                    return parameter.PairWithValue(matchingGroups.First().Value);
                                })
                            .SelectWhereHasValue()
                            .ToArray();

                        return assignmentCollection;
                    })
                .Where(parameterAndValues => parameterAndValues.Length == parameterSet.Length)
                .ToArray();
            return parameterAndValuess
                .Aggregate(
                    new string[][] { },
                    (invokeArgss, parameterAndValues) =>
                    {
                        var nextArgs = parameterSet
                            .Aggregate(
                                new string[] { },
                                (invokeArgs, parameter) =>
                                {
                                    var valuesMaybe = parameterAndValues.Where(pAndV => pAndV.Key == parameter);
                                    if (!valuesMaybe.Any())
                                        throw new ArgumentException();
                                    var value = valuesMaybe.First();
                                    return invokeArgs.Append(value.Value).ToArray();
                                });
                        return invokeArgss.Append(nextArgs).ToArray();
                    },
                    (invokeArgss) =>
                    {
                        if (!invokeArgss.Any())
                            return onMatched(new T[] { });
                        
                        return onMatched(
                            invokeArgss
                                .Select(invoker)
                                .ToArray());
                    });
        }
    }
}
