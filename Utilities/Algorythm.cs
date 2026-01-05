using Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static Music.Messages;
using Melodies25.Utilities;
using System.Drawing; // added for LoggingManager

namespace Music
{
    public static class Algorythm
    {
        //================== Longest Common Substring =================//
        // Повертає (довжина послідовності нот, початковий індекс у другому масиві) для int масивів
        public static (int length, int startIndex) LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            var sw = Stopwatch.StartNew();
            (int length, int startIndex) result = (0, -1);

            // prepare a buffer for diagnostics so printing can be deferred
            var diag = new System.Text.StringBuilder();
            if (LoggingManager.AlgorithmDiagnostics)
                MessageL(7, " arr1: " + string.Join(", ", arr1 ?? Array.Empty<int>()) + ",\narr2: " + string.Join(", ", arr2 ?? Array.Empty<int>()));
            try
            {
                if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
                {
                    result = (0, -1);
                    return result;
                }

                int[,] dp = new int[arr1.Length + 1, arr2.Length + 1];
                int maxLength = 0;
                int endIndex = -1;

                for (int i = 1; i <= arr1.Length; i++)
                {
                    for (int j = 1; j <= arr2.Length; j++)
                    {
                        if (arr1[i - 1] == arr2[j - 1])
                        {
                            dp[i, j] = dp[i - 1, j - 1] + 1;
                            if (dp[i, j] > maxLength)
                            {
                                maxLength = dp[i, j];
                                endIndex = j - 1;
                            }
                        }
                        else
                        {
                            dp[i, j] = 0;
                        }
                    }
                }

                int startIndex = (maxLength > 0) ? (endIndex - maxLength + 1) : -1;
                int notesCount = (maxLength > 0) ? (maxLength + 1) : 0;

                // build diagnostics but do not print yet
                diag.AppendLine($"\nmatches (arr1Index:arr2Index value) example: (notesCount={notesCount}, startIndex={startIndex})");
                result = (notesCount, startIndex);
                return result;
            }
            finally
            {
                sw.Stop();

                // діагностика
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    if (diag.Length > 0)
                        Console.WriteLine(diag.ToString());

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.LongestCommonSubstring(int[]) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        // Повертає (довжина послідовності нот, початковий індекс у другому масиві) для float масивів
        public static (int length, int startIndex) LongestCommonSubstring(float[] arr1, float[] arr2)
        {
            var sw = Stopwatch.StartNew();
            (int length, int startIndex) result = (0, -1);

            if (LoggingManager.AlgorithmDiagnostics)
                MessageL(7, " arr1: " + string.Join(", ", arr1 ?? Array.Empty<float>()) + ",\narr2: " + string.Join(", ", arr2 ?? Array.Empty<float>()));
            try
            {
                if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
                {
                    result = (0, -1);
                    return result;
                }

                const double EPS = 1e-6; // tolerance for float comparison
                int n = arr1.Length;
                int m = arr2.Length;
                int[,] dp = new int[n + 1, m + 1];
                int maxLength = 0;
                int endIndex = -1;

                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        if (Math.Abs(arr1[i - 1] - arr2[j - 1]) <= EPS)
                        {
                            dp[i, j] = dp[i - 1, j - 1] + 1;
                            if (dp[i, j] > maxLength)
                            {
                                maxLength = dp[i, j];
                                endIndex = j - 1;
                            }
                        }
                        else
                        {
                            dp[i, j] = 0;
                        }
                    }
                }

                int startIndex = (maxLength > 0) ? (endIndex - maxLength + 1) : -1;
                int notesCount = (maxLength > 0) ? (maxLength + 1) : 0;
                result = (notesCount, startIndex);
                return result;
            }
            finally
            {
                sw.Stop();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Algorythm.LongestCommonSubstring(float[]) elapsed: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();
            }
        }

        //================== Longest Common Subsequence with Gap =================//
        // Повертає (довжина послідовності нот, індекси у першому масиві, індекси у другому масиві)
        // gap - максимальна кількість пропущених елементів у другому масиві між послідовними співпадіннями
        public static (int length, List<int> indicesInFirst, List<int> indicesInSecond) LongestCommonSubsequence(int[] arr1, int[] arr2, int gap)
        {
            var sw = Stopwatch.StartNew();
            (int length, List<int> indicesInFirst, List<int> indicesInSecond) result = (0, new List<int>(), new List<int>());

            if (LoggingManager.AlgorithmDiagnostics)
                MessageL(7, " arr1: " + string.Join(", ", arr1 ?? Array.Empty<int>()) + ",\narr2: " + string.Join(", ", arr2 ?? Array.Empty<int>()));
            try
            {
                if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
                {
                    return result;
                }

                int m = arr1.Length;
                int n = arr2.Length;

                // Ми будемо робити жадібний пошук всіх можливих послідовностей, що починаються з будь-якої пари співпадіння
                int bestLen = 0;
                List<int> bestI = new List<int>();
                List<int> bestJ = new List<int>();

                for (int iStart = 0; iStart < m; iStart++)
                {
                    for (int jStart = 0; jStart < n; jStart++)
                    {
                        if (arr1[iStart] != arr2[jStart])
                            continue;

                        var curI = new List<int> { iStart };
                        var curJ = new List<int> { jStart };
                        int prevJ = jStart;

                        // Пробігаємо наступні індекси arr1 (можемо пропускати елементи arr1)
                        for (int i = iStart + 1; i < m; i++)
                        {
                            // допустимий інтервал пошуку в arr2: від prevJ+1 до prevJ + gap +1
                            int limit = Math.Min(n - 1, prevJ + gap + 1);
                            bool found = false;
                            for (int jj = prevJ + 1; jj <= limit; jj++)
                            {
                                if (arr2[jj] == arr1[i])
                                {
                                    curI.Add(i);
                                    curJ.Add(jj);
                                    prevJ = jj;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                // якщо наступний співпад не знайдено в межах гепа, послідовність вважається завершеною
                                break;
                            }
                        }

                        if (curI.Count > bestLen)
                        {
                            bestLen = curI.Count;
                            bestI = new List<int>(curI);
                            bestJ = new List<int>(curJ);
                        }
                    }
                }

                // Друк для діагностики
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.Write($"\nmatches (arr1Index:arr2Index value) with gap={gap}: ");
                    for (var k = 0; k < bestI.Count; k++)
                    {
                        MessageL(COLORS.gray, $"{bestI[k]}:{bestJ[k]} {arr1[bestI[k]]}/{arr2[bestJ[k]]} ");
                    }
                    Console.WriteLine();
                }
                result = (bestLen, bestI, bestJ);
                return result;
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.LongestCommonSubsequence(int[]) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        // public wrapper with default tolerance so callers can call without eps
        public static (int length, List<int> indicesInFirst, List<int> indicesInSecond) LongestCommonSubsequence(float[] arr1, float[] arr2, int gap)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                const double DEFAULT_EPS = 1e-3; // adjust precision if needed
                return LongestCommonSubsequence(arr1, arr2, gap, DEFAULT_EPS);
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.LongestCommonSubsequence(float[] wrapper) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        // Add missing float overload that accepts eps tolerance
        public static (int length, List<int> indicesInFirst, List<int> indicesInSecond) LongestCommonSubsequence(float[] arr1, float[] arr2, int gap, double eps)
        {
            var sw = Stopwatch.StartNew();
            (int length, List<int> indicesInFirst, List<int> indicesInSecond) result = (0, new List<int>(), new List<int>());

            if (LoggingManager.AlgorithmDiagnostics)
                MessageL(7, " arr1: " + string.Join(", ", arr1 ?? Array.Empty<float>()) + ",\narr2: " + string.Join(", ", arr2 ?? Array.Empty<float>()));
            try
            {
                if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
                    return result;

                int m = arr1.Length;
                int n = arr2.Length;

                int bestLen = 0;
                List<int> bestI = new List<int>();
                List<int> bestJ = new List<int>();

                for (int iStart = 0; iStart < m; iStart++)
                {
                    for (int jStart = 0; jStart < n; jStart++)
                    {
                        if (Math.Abs(arr1[iStart] - arr2[jStart]) > eps)
                            continue;

                        var curI = new List<int> { iStart };
                        var curJ = new List<int> { jStart };
                        int prevJ = jStart;

                        for (int i = iStart + 1; i < m; i++)
                        {
                            int limit = Math.Min(n - 1, prevJ + gap + 1);
                            bool found = false;
                            for (int jj = prevJ + 1; jj <= limit; jj++)
                            {
                                if (Math.Abs(arr2[jj] - arr1[i]) <= eps)
                                {
                                    curI.Add(i);
                                    curJ.Add(jj);
                                    prevJ = jj;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                break;
                        }

                        if (curI.Count > bestLen)
                        {
                            bestLen = curI.Count;
                            bestI = new List<int>(curI);
                            bestJ = new List<int>(curJ);
                        }
                    }
                }

                // Diagnostics
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.Write($"\nmatches (arr1Index:arr2Index value) with gap={gap}: ");
                    for (var k = 0; k < bestI.Count; k++)
                    {
                        Console.Write($"{bestI[k]}:{bestJ[k]} {arr1[bestI[k]]}/{arr2[bestJ[k]]} ");
                    }
                    Console.WriteLine();
                }

                result = (bestLen, bestI, bestJ);
                return result;
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.LongestCommonSubsequence(float[], eps) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        public static (int len, int pos, List<(int i1, int i2)> pairs) FindCommonSubstringByIntervalAndDuration(int[] arr1, int[] arr2, float[] float1, float[] float2, double eps = 1e-3)
        {
            var sw = Stopwatch.StartNew();
            (int len, int pos, List<(int i1, int i2)> pairs) result = (0, -1, new List<(int i1, int i2)>());

            if (LoggingManager.AlgorithmDiagnostics)
                MessageL(7, " arr1: " + string.Join(", ", arr1 ?? Array.Empty<int>()) + ",\narr2: " + string.Join(", ", arr2 ?? Array.Empty<int>()) + ",\nfloat1: " + string.Join(", ", float1 ?? Array.Empty<float>()) + ",\nfloat2: " + string.Join(", ", float2 ?? Array.Empty<float>()));
            try
            {
                // Validate
                if (arr1 == null || arr2 == null || float1 == null || float2 == null)
                    return result;
                if (arr1.Length == 0 || arr2.Length == 0 || float1.Length == 0 || float2.Length == 0)
                    return result;

                int n = arr1.Length;
                int m = arr2.Length;

                // DP for longest common contiguous substring over intervals where durations also match
                var dp = new int[n + 1, m + 1];
                int maxLenIntervals = 0;
                int endIndexInMelody = -1;

                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        bool intervalsEqual = arr1[i - 1] == arr2[j - 1];
                        bool durationsEqual = false;

                        // Compare the second-note durations in each interval-pair (as in original logic)
                        float patDur = float1.Length > i ? float1[i] : float.NaN;
                        float melDur = float2.Length > j ? float2[j] : float.NaN;
                        durationsEqual = !(float.IsNaN(patDur) || float.IsNaN(melDur)) && Math.Abs(patDur - melDur) <= eps;

                        if (intervalsEqual && durationsEqual)
                        {
                            dp[i, j] = dp[i - 1, j - 1] + 1;
                            if (dp[i, j] > maxLenIntervals)
                            {
                                maxLenIntervals = dp[i, j];
                                endIndexInMelody = j - 1;
                            }
                        }
                        else
                        {
                            dp[i, j] = 0;
                        }
                    }
                }

                if (maxLenIntervals == 0)
                    return result;

                // Convert interval-run len to notes count (intervals + 1)
                int len = maxLenIntervals + 1;
                int startIntervalIndex = endIndexInMelody - maxLenIntervals + 1;
                int pos = startIntervalIndex;

                var pairs = new List<(int i1, int i2)>();
                for (int k = 0; k < len; k++)
                    pairs.Add((pos + k, k));

                result = (len, pos, pairs);
                return result;
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.FindCommonSubstringByIntervalAndDuration elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        //================== Public API Methods =================//
        // Повертає (довжина збігу, початковий індекс у другому масиві, пари індексів)
        public static (int length, int position, List<(int i1, int i2)> pairs) FindLongestSubstringMatch(int[] arr1, int[] arr2)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var (len, pos) = LongestCommonSubstring(arr1, arr2);
                List<(int i1, int i2)> pairs = GetPairs(len, pos);
                return (len, pos, pairs);
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.FindLongestSubstringMatch(int[]) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        public static (int len, int pos, List<(int i1, int i2)> pairs) FindLongestSubstringMatch(float[] arr1, float[] arr2)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var (len, pos) = LongestCommonSubstring(arr1, arr2);
                var pairs = GetPairs(len, pos);
                return (len, pos, pairs);
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.FindLongestSubstringMatch(float[]) elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }

        private static List<(int i1, int i2)> GetPairs(int notesCount, int startIndex)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var pairs = new List<(int i1, int i2)>();
                if (notesCount > 0 && startIndex >= 0)
                {
                    for (int k = 0; k < notesCount; k++)
                        pairs.Add((startIndex + k, k));
                }

                return pairs;
            }
            finally
            {
                sw.Stop();
                if (LoggingManager.AlgorithmDiagnostics)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Algorythm.GetPairs elapsed: {sw.ElapsedMilliseconds} ms");
                    Console.ResetColor();
                }
            }
        }
    }
}


