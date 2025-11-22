using Music;
using System;
using System.Collections.Generic;
using System.Linq;
using static Music.Messages;

namespace Music
{
    public static class Algorythm
    {
        public static (int length, int startIndex) LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            
            MessageL(7, " arr1: " + string.Join(", ", arr1) + ",\narr2: " + string.Join(", ", arr2));
            if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
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

            // NOTE:
            // arr1/arr2 are arrays of intervals (length = notes-1). The number of matching notes
            // corresponding to maxLength matching intervals is maxLength +1. Return length in
            // terms of notes (not intervals) so callers and UI highlighting operate on note counts.
            int notesCount = (maxLength > 0) ? (maxLength + 1) : 0;


            return (notesCount, startIndex);
        }

        // Overload for float arrays with epsilon tolerance (useful when values are floats)
        public static (int length, int startIndex) LongestCommonSubstring(float[] arr1, float[] arr2)
        {
            MessageL(7, " arr1: " + string.Join(", ", arr1) + ",\narr2: " + string.Join(", ", arr2));
            if (arr1 == null || arr2 == null || arr1.Length == 0 || arr2.Length == 0)
                return (0, -1);

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
            for (var k = 0; k < bestI.Count; k++)
            {
                Console.Write($"{bestI[k]}:{bestJ[k]} {arr1[bestI[k]]}/{arr2[bestJ[k]]} ");
            }
            Console.WriteLine();

            return (bestLen, bestI, bestJ);
        }

        private static void LogInput(int[] arr1, int[] arr2)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("SimilarByIntervalsWithGap Indices starts:");
            for (int i = 0; i < arr1.Length; i++) Console.Write(arr1[i] + " ");
            Console.Write(" vs ");
            for (int i = 0; i < arr2.Length; i++) Console.Write(arr2[i] + " ");
        }

        // New --- moved helpers so Pages/Melodies/Search.cshtml.cs can call them directly:
        public static (int length, int position, List<(int i1, int i2)> pairs) FindLongestSubstringMatch(int[] patternShape, int[] melodyShape)
        {
            // Use existing SimilarByIntervals which returns (notesCount, startIndex)
            var (notesCount, startIndex) = LongestCommonSubstring(patternShape, melodyShape);
            var pairs = new List<(int i1, int i2)>();
            if (notesCount > 0 && startIndex >= 0)
            {
                for (int k = 0; k < notesCount; k++)
                    pairs.Add((startIndex + k, k));
            }
            return (notesCount, startIndex, pairs);
        }

        public static (int len, int pos, List<(int i1, int i2)> pairs) FindLongestSubstringMatch(float[] patternShape, float[] melodyShape)
        {
            // Call canonical Algorythm implementation to get (length, startIndex)
            var result = LongestCommonSubstring(patternShape, melodyShape);
            int notesCount = result.length;
            int startIndex = result.startIndex;

            var pairs = new List<(int i1, int i2)>();
            if (notesCount > 0 && startIndex >= 0)
            {
                for (int k = 0; k < notesCount; k++)
                    pairs.Add((startIndex + k, k));
            }

            return (notesCount, startIndex, pairs);
        }
        public static (int length, int position, List<(int i1, int i2)> pairs, int bestShift) FindBestSubsequenceMatch(int[] melodyNotes, int[] patternNotes, int maxGap)
        {
            int clamped = Math.Clamp(maxGap, 1, 3);
            int bestLen = 0;
            int bestPos = -1;
            int bestShift = 0;
            List<(int i1, int i2)> bestPairsForMelody = new();

            // Перебираємо усі 12 транспозицій (півтонів)
            for (int shift = 0; shift < Globals.NotesInOctave; shift++)
            {
                var transposedPattern = patternNotes.Select(p => (p + shift) % Globals.NotesInOctave).ToArray();

                LogInput(transposedPattern, melodyNotes);
                var (lcsTotalLen, idxFirstAll, idxSecondAll) = LongestCommonSubsequence(melodyNotes, transposedPattern, maxGap);
                if (idxSecondAll == null || idxSecondAll.Count == 0) continue;

                // Побудова всіх "кластерів" збігів, де розриви в обох масивах <= clamped.
                // Обираємо найдовший кластер. При рівній довжині зберігаємо перший знайдений (найраніший).
                var curFirst = new List<int>();
                var curSecond = new List<int>();

                void FinalizeCluster()
                {
                    if (curFirst.Count == 0) return;

                    int curLen = curFirst.Count;
                    if (curLen > bestLen)
                    {
                        bestLen = curLen;
                        bestPos = curFirst[0];
                        bestShift = shift;

                        bestPairsForMelody = new List<(int i1, int i2)>();
                        for (int t = 0; t < curFirst.Count; t++)
                            bestPairsForMelody.Add((curFirst[t], curSecond[t]));
                    }
                }

                for (int k = 0; k < idxSecondAll.Count; k++)
                {
                    if (curFirst.Count == 0)
                    {
                        curFirst.Add(idxFirstAll[k]);
                        curSecond.Add(idxSecondAll[k]);
                        continue;
                    }

                    int gapSecond = idxSecondAll[k] - curSecond[^1] - 1;
                    int gapFirst  = idxFirstAll[k]  - curFirst[^1]  - 1;

                    if (gapSecond <= clamped && gapFirst <= clamped)
                    {
                        curFirst.Add(idxFirstAll[k]);
                        curSecond.Add(idxSecondAll[k]);
                    }
                    else
                    {
                        // Закриваємо поточний кластер і починаємо новий з поточного елемента
                        FinalizeCluster();
                        curFirst.Clear(); curSecond.Clear();
                        curFirst.Add(idxFirstAll[k]);
                        curSecond.Add(idxSecondAll[k]);
                    }
                }

                // фіналізуємо останній відкритий кластер
                FinalizeCluster();

                // ранній вихід, якщо знайдено повний збіг довжини патерна
                if (bestLen >= patternNotes.Length) break;
            }

            return (bestLen, bestPos, bestPairsForMelody, bestShift);
        }


        
        
    }
}
