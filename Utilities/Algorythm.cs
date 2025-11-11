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
        // Повертає довжину та індекси збігів у другій послідовності
        public static (int length, List<int> indicesInSecond) LongestCommonSubsequence(int[] arr1, int[] arr2)
        {
            //Console.WriteLine("LongestCommonSubsequence Indices starts");

            int m = arr1.Length;
            int n = arr2.Length;
            int[,] dp = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (arr1[i - 1] == arr2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            var indices = new List<int>();
            int ii = m, jj = n;
            while (ii > 0 && jj > 0)
            {
                if (arr1[ii - 1] == arr2[jj - 1])
                {
                    indices.Add(jj - 1);
                    ii--; jj--;
                }
                else if (dp[ii - 1, jj] >= dp[ii, jj - 1])
                {
                    ii--;
                }
                else
                {
                    jj--;
                }
            }
            indices.Reverse();
            return (dp[m, n], indices);
        }

        // LCS з обмеженням на максимальний розрив між сусідніми збігами у другій послідовності
        // Повертає довжину та індекси збігів у другій послідовності
        // Якщо розрив більший за maxSkipBetweenMatches, такий збіг ігнорується
        // title - назва для виводу в консоль
        // Використовується для аналізу мелодій з обмеженням на пропуски нот
        //
        public static (int length, List<int> indicesInSecond) LongestCommonSubsequenceLimitedSkips(int[] arr1, int[] arr2, int maxSkipBetweenMatches, string title = "noname")
        {
            var (len, idx) = LongestCommonSubsequence(arr1, arr2);
            if (idx.Count <= 2 || maxSkipBetweenMatches <= 0)
                return (len, idx);

            var filtered = new List<int> { idx[0] };
            Console.Write($"indexes coincide for {title}: ");
            for (int k = 1; k < idx.Count; k++)
            {
                if (idx[k] - filtered[^1] - 1 <= maxSkipBetweenMatches)
                {
                    filtered.Add(idx[k]);
                    Console.Write($"{idx[k]} ");
                }
            }
            Console.WriteLine();
            return (filtered.Count, filtered);
        }
    }
}
