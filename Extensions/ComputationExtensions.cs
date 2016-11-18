using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlackBarLabs
{
    public static class ComputationExtensions
    {
        public static string ToMoneyString(this int cents)
        {
            return (cents / 100).ToString() + (cents % 100).ToString();
        }
        
        public static TResult FromMoneyString<TResult>(this string money,
            Func<int, TResult> parsed,
            Func<TResult> invalidOrUnrecognizedFormat)
        {
            if (String.IsNullOrWhiteSpace(money))
                return invalidOrUnrecognizedFormat();

            var regex = new Regex(@"(\-)?(\d+)?(\.(\d+))?");
            var matches = regex.Matches(money);
            if (matches.Count == 0 || matches[0].Groups.Count != 5)
                return invalidOrUnrecognizedFormat();

            var match = matches[0];
            var multiplier = match.Groups[1].Success ? -1 : 1;
            var dollars = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            var cents = match.Groups[3].Success ?
                    match.Groups[4].Value.Length == 2?
                        int.Parse(match.Groups[4].Value)
                        :
                        int.Parse(match.Groups[4].Value) * 10

                :
                    0;

            var value = multiplier * ((dollars * 100) + cents);
            return parsed(value);
        }

        //public static void Main(string[] args)
        //{
        //    var strInput = "-.3";     //should match

        //    var a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != -30)
        //        throw new Exception();


        //    strInput = "-.03";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != -3)
        //        throw new Exception();

        //    strInput = ".03";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != 3)
        //        throw new Exception();


        //    strInput = "-3.1";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != -310)
        //        throw new Exception();

        //    strInput = "2.";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != 200)
        //        throw new Exception();

        //    strInput = "230";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != 23000)
        //        throw new Exception();


        //    strInput = "-3";
        //    a = FromMoneyString(strInput,
        //                            (v) => v,
        //                            () => 0);
        //    if (a != -300)
        //        throw new Exception();


        //    Console.WriteLine(a);
        //}

    }
}
