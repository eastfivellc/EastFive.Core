using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Text
{
    public static class StringDistance
    {
        public static int Levenshtein(this string str1, string str2)
        {
            if (str1.IsNullOrWhiteSpace())
                return str2.IsNullOrWhiteSpace() ? 0 : str2.Length;

            if (str2.IsNullOrWhiteSpace())
                return str1.Length;

            int lengthA = str1.Length;
            int lengthB = str2.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = str2[j - 1] == str1[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            return distances[lengthA, lengthB];
        }

        /// <summary>
        /// The Winkler modification will not be applied unless the 
        /// percent match was at or above the mWeightThreshold percent
        /// without the modification.
        /// Winkler's paper used a default value of 0.7
        /// </summary>
        private static readonly double mWeightThreshold = 0.7;
        
        /// <summary>
        /// Size of the prefix to be concidered by the Winkler modification. 
        /// Winkler's paper used a default value of 4
        /// </summary>
        private static readonly int mNumChars = 4;
        
        /// <summary>
        /// Returns the Jaro-Winkler distance between the specified  
        /// strings. The distance is symmetric and will fall in the 
        /// range 0 (perfect match) to 1 (no match). 
        /// </summary>
        /// <param name="aString1">First String</param>
        /// <param name="aString2">Second String</param>
        /// <returns></returns>
        public static double JaroWinkler(this string aString1, string aString2)
        {
            return 1.0 - JaroWinklerInverted(aString1, aString2);
        }
        
        /// <summary>
        /// Returns the Jaro-Winkler distance between the specified  
        /// strings. The distance is symmetric and will fall in the 
        /// range 0 (no match) to 1 (perfect match). 
        /// </summary>
        /// <param name="aString1">First String</param>
        /// <param name="aString2">Second String</param>
        /// <returns></returns>
        public static double JaroWinklerInverted(this string aString1, string aString2)
        {
            int lLen1 = aString1.Length;
            int lLen2 = aString2.Length;
            if (lLen1 == 0)
                return lLen2 == 0 ? 1.0 : 0.0;

            int lSearchRange = Math.Max(0, Math.Max(lLen1, lLen2) / 2 - 1);

            // default initialized to false
            bool[] lMatched1 = new bool[lLen1];
            bool[] lMatched2 = new bool[lLen2];

            int lNumCommon = 0;
            for (int i = 0; i < lLen1; ++i)
            {
                int lStart = Math.Max(0, i - lSearchRange);
                int lEnd = Math.Min(i + lSearchRange + 1, lLen2);
                for (int j = lStart; j < lEnd; ++j)
                {
                    if (lMatched2[j]) continue;
                    if (aString1[i] != aString2[j])
                        continue;
                    lMatched1[i] = true;
                    lMatched2[j] = true;
                    ++lNumCommon;
                    break;
                }
            }
            if (lNumCommon == 0) return 0.0;

            int lNumHalfTransposed = 0;
            int k = 0;
            for (int i = 0; i < lLen1; ++i)
            {
                if (!lMatched1[i]) continue;
                while (!lMatched2[k]) ++k;
                if (aString1[i] != aString2[k])
                    ++lNumHalfTransposed;
                ++k;
            }
            // System.Diagnostics.Debug.WriteLine("numHalfTransposed=" + numHalfTransposed);
            int lNumTransposed = lNumHalfTransposed / 2;

            // System.Diagnostics.Debug.WriteLine("numCommon=" + numCommon + " numTransposed=" + numTransposed);
            double lNumCommonD = lNumCommon;
            double lWeight = (lNumCommonD / lLen1
                             + lNumCommonD / lLen2
                             + (lNumCommon - lNumTransposed) / lNumCommonD) / 3.0;

            if (lWeight <= mWeightThreshold) return lWeight;
            int lMax = Math.Min(mNumChars, Math.Min(aString1.Length, aString2.Length));
            int lPos = 0;
            while (lPos < lMax && aString1[lPos] == aString2[lPos])
                ++lPos;
            if (lPos == 0) return lWeight;
            return lWeight + 0.1 * lPos * (1.0 - lWeight);

        }

        #region Smith Waterman

        public static double SmithWaterman(this string str1, string str2, 
            double matchValue = 5.0,
            double mismatchValue = -3.0,
            double startValue = -5.0f,
            double gapValue = -1.0f,
            int windowSize = int.MaxValue)
        {
            if (str1.IsNullOrWhiteSpace() && str2.IsNullOrWhiteSpace())
                return 0.0f;

            if (str1.IsNullOrWhiteSpace() || str2.IsNullOrWhiteSpace())
                return 1.0f;

            var maxDistance = Math.Min(str1.Length, str2.Length) * matchValue;

            Func<string, int, string, int, double> compare =
                (a, aIndex, b, bIndex) =>
            {
                return a[aIndex] == b[bIndex] ? matchValue
                        : mismatchValue;
            };
            Func<int, int, double> gap = (fromIndex, toIndex) =>
            {
                return startValue + gapValue * (toIndex - fromIndex - 1);
            };
            return 1.0 - (SmithWatermanNoNormal(str1, str2, compare, gap, windowSize) / maxDistance);
        }

        private static double SmithWatermanNoNormal(String str1, String str2,
            Func<string, int, string, int, double> compare,
            Func<int, int, double> gap,
            int windowSize)
        {
            var n = str1.Length;
            var m = str2.Length;

            var d = new double[n,m];

            // Initialize corner
            var max = Math.Max(0, compare(str1, 0, str2, 0));
            d[0, 0] = max;

            // Initialize edge
            for (int i = 0; i < n; i++)
            {

                // Find most optimal deletion
                double maxGapCost = 0;
                for (int k = Math.Max(1, i - windowSize); k < i; k++)
                {
                    maxGapCost = Math.Max(maxGapCost, d[i - k,0] + gap(i - k, i));
                }

                d[i,0] = Math.Max(0, Math.Max(maxGapCost, compare(str1, i, str2, 0)));

                max = Math.Max(max, d[i,0]);

            }

            // Initialize edge
            for (int j = 1; j < m; j++)
            {
                // Find most optimal insertion
                double maxGapCost = 0;
                for (int k = Math.Max(1, j - windowSize); k < j; k++)
                {
                    maxGapCost = Math.Max(maxGapCost, d[0,j - k] + gap(j - k, j));
                }

                d[0,j] = Math.Max(0, Math.Max(maxGapCost, compare(str1, 0, str2, j)));

                max = Math.Max(max, d[0,j]);

            }

            // Build matrix
            for (int i = 1; i < n; i++)
            {

                for (int j = 1; j < m; j++)
                {

                    double maxGapCost = 0;
                    // Find most optimal deletion
                    for (int k = Math.Max(1, i - windowSize); k < i; k++)
                    {
                        maxGapCost = Math.Max(maxGapCost,
                                d[i - k,j] + gap(i - k, i));
                    }
                    // Find most optimal insertion
                    for (int k = Math.Max(1, j - windowSize); k < j; k++)
                    {
                        maxGapCost = Math.Max(maxGapCost,
                                d[i,j - k] + gap(j - k, j));
                    }

                    // Find most optimal of insertion, deletion and substitution
                    d[i,j] = Math.Max(Math.Max(0, maxGapCost),
                            d[i - 1,j - 1] + compare(str1, i, str2, j));

                    max = Math.Max(max, d[i,j]);
                }

            }

            return max;
        }

        #endregion
    }
}
