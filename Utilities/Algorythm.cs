using Music;
using System;
using System.Collections.Generic;
using static Music.Messages;

namespace Melodies25.Utilities
{
    public static class Algorythm
    {
        public static (int length, int startIndex) LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            
            MessageL(7, " arr1: " + string.Join(", ", arr1) + ",\narr2: " + string.Join(", ", arr2));
            if (arr1.Length ==0 || arr2.Length ==0)
                return (0, -1);

            int[,] dp = new int[arr1.Length +1, arr2.Length +1];
            int maxLength =0;
            int endIndex = -1; // Індекс останнього елемента підрядка у другій послідовності

            for (int i =1; i <= arr1.Length; i++)
            {
                for (int j =1; j <= arr2.Length; j++)
                {
                    if (arr1[i -1] == arr2[j -1])
                    {
                        dp[i, j] = dp[i -1, j -1] +1;
                        if (dp[i, j] > maxLength)
                        {
                            maxLength = dp[i, j];
                            endIndex = j -1; // Останній збіг у `arr2`
                        }
                    }
                    else
                    {
                        dp[i, j] =0;
                    }
                }
            }

            int startIndex = (maxLength >0) ? (endIndex - maxLength +1) : -1;

            // NOTE:
            // arr1/arr2 are arrays of intervals (length = notes-1). The number of matching notes
            // corresponding to maxLength matching intervals is maxLength +1. Return length in
            // terms of notes (not intervals) so callers and UI highlighting operate on note counts.
            int notesCount = (maxLength >0) ? (maxLength +1) :0;

            
            return (notesCount, startIndex);
        }


        public static (int length, List<int> indicesInFirst, List<int> indicesInSecond) LongestCommonSubsequence(int[] arr1, int[] arr2, int gap)
        {
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
            Console.Write($"\nmatches (arr1Index:arr2Index value) with gap={gap}: ");
            foreach (var k in Enumerable.Range(0, bestI.Count))
            {
                Console.Write($"{bestI[k]}:{bestJ[k]} {arr1[bestI[k]]}/{arr2[bestJ[k]]} ");
            }
            Console.WriteLine();

            return (bestLen, bestI, bestJ);
        }

        private static void LogInput(int[] arr1, int[] arr2)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("LongestCommonSubsequence Indices starts:");
            for (int i = 0; i < arr1.Length; i++) Console.Write(arr1[i] + " ");
            Console.Write(" vs ");
            for (int i = 0; i < arr2.Length; i++) Console.Write(arr2[i] + " ");
        }

        
    }
}
