using Music;
using System;
using System.Collections.Generic;

namespace Melodies25.Utilities
{
    public static class Algorythm
    {
        public static (int length, int startIndex) LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            //Console.WriteLine("LongestCommonSubstring starts");
            if (arr1.Length == 0 || arr2.Length == 0)
                return (0, -1);

            int[,] dp = new int[arr1.Length + 1, arr2.Length + 1];
            int maxLength = 0;
            int endIndex = -1; // Індекс останнього елемента підрядка у другій послідовності

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
                            endIndex = j - 1; // Останній збіг у `arr2`
                        }
                    }
                    else
                    {
                        dp[i, j] = 0;
                    }
                }
            }

            int startIndex = (maxLength > 0) ? (endIndex - maxLength + 1) : -1;
            return (maxLength, startIndex);
        }

        public static int LongestCommonsStart(int[] arr1, int[] arr2)
        {
            int count = 0;
            int minLength = Math.Min(arr1.Length, arr2.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (arr1[i] == arr2[i])
                    count++;
                else
                    break; // Якщо знайдено перший незбіг, зупиняємо цикл
            }

            return count;
        }

        
        public static int LongestStartSubsequence(int[] arr1, int[] arr2, int maxGap)
        {
            //Console.WriteLine("LongestStartSubsequence starts");
            int count = 0;
            int gaps = 0;
            int minLength = Math.Min(arr1.Length, arr2.Length);

            for (int i = 0, j = 0; i < minLength && j < minLength;)
            {
                if (arr1[i] == arr2[j])
                {
                    count++;
                    gaps = 0; // Скидаємо лічильник пропусків
                }
                else
                {
                    gaps++;
                    if (gaps > maxGap)
                        break; // Якщо перевищили maxGap — зупиняємо підрахунок
                }

                i++;
                j++;
            }

            return count;
        }

        // КЛАСИЧНИЙ LCS: Найдовша спільна підпослідовність (дозволяє пропуски)
        // Тепер повертає довжину та індекси збігів у обох послідовностях:
        // (length, indicesInFirst, indicesInSecond)
        public static (int length, List<int> indicesInFirst, List<int> indicesInSecond) LongestCommonSubsequence(int[] arr1, int[] arr2)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("LongestCommonSubsequence Indices starts:");
            for (int i = 0; i < arr1.Length; i++) Console.Write(arr1[i] + " ");
            Console.Write(" vs ");
            for (int i = 0; i < arr2.Length; i++) Console.Write(arr2[i] + " ");

            int m = arr1.Length;
            int n = arr2.Length;
            int[,] dp = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (arr1[i - 1] == arr2[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }

            // Збираємо пари під час backtracking: (indexInArr1, indexInArr2, valueFromArr1, valueFromArr2)
            var matches = new List<(int i1, int i2, int v1, int v2)>();
            int ii = m, jj = n;
            while (ii > 0 && jj > 0)
            {
                if (arr1[ii - 1] == arr2[jj - 1])
                {
                    matches.Add((ii - 1, jj - 1, arr1[ii - 1], arr2[jj - 1]));
                    ii--; jj--;
                }
                else if (dp[ii - 1, jj] >= dp[ii, jj - 1])
                    ii--;
                else
                    jj--;
            }

            matches.Reverse();

            // Побудова списків індексів по першому та другому масивах
            var indicesFirst = new List<int>();
            var indicesSecond = new List<int>();
            foreach (var mch in matches)
            {
                indicesFirst.Add(mch.i1);
                indicesSecond.Add(mch.i2);
            }

            // Детальний друк для діагностики: показуємо i1:i2 value
            Console.Write("\nmatches (arr1Index:arr2Index value): ");
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var mch in matches)
            {
                Console.Write($"{mch.i1}:{mch.i2} {mch.v1}/{mch.v2}  ");
            }
            Console.WriteLine();

            return (dp[m, n], indicesFirst, indicesSecond);
        }

        // LCS з обмеженням на максимальний розрив між сусідніми збігами
        // Тепер фільтрує так, щоб розрив між сусідніми збігами не перевищував maxSkipBetweenMatches
        // У ОБОХ послідовностях (arr1 та arr2). Повертає довжину та індекси збігів у ПЕРШІЙ послідовності.
        // Якщо розрив більший за maxSkipBetweenMatches в будь-якій з послідовностей — цей збіг НЕ враховується.
        public static (int length, List<int> indicesInFirst) LongestCommonSubsequenceLimitedSkips(int[] arr1, int[] arr2, int maxSkipBetweenMatches, string title = "noname")
        {
            var (len, idxFirst, idxSecond) = LongestCommonSubsequence(arr1, arr2);
            if (idxFirst.Count == 0 || idxSecond.Count == 0 || maxSkipBetweenMatches <= 0)
                return (len, idxFirst);

            var filteredFirst = new List<int> { idxFirst[0] };
            var filteredSecond = new List<int> { idxSecond[0] };

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"indexes coincide for {title}: ");

            for (int k = 1; k < idxSecond.Count; k++)
            {
                int gapSecond = idxSecond[k] - filteredSecond[^1] - 1;
                int gapFirst = idxFirst[k] - filteredFirst[^1] - 1;

                // require both gaps to be <= maxSkipBetweenMatches
                if (gapSecond <= maxSkipBetweenMatches && gapFirst <= maxSkipBetweenMatches)
                {
                    filteredSecond.Add(idxSecond[k]);
                    filteredFirst.Add(idxFirst[k]);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"{idxSecond[k]} ");
                }
                else
                {
                    // break the filtered sequence when a gap violates constraint;
                    // further matches could start a new cluster — here we keep only the first contiguous cluster
                    // If you want to allow multiple clusters, consider collecting all clusters and picking the longest.
                    break;
                }
            }
            Console.WriteLine();

            return (filteredFirst.Count, filteredFirst);
        }
    }
}
