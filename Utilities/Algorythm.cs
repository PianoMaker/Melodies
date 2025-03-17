namespace Melodies25.Utilities
{
    public static class Algorythm
    {
        public static (int length, int startIndex) LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            if (arr1.Length == 0 || arr2.Length == 0)
                return (0, -1);

            int[,] dp = new int[arr1.Length + 1, arr2.Length + 1];
            int maxLength = 0;
            int endIndex = -1; // Індекс останнього елемента підрядка

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


        public static int LongestCommonSubsequence(int[] arr1, int[] arr2, int maxGap)
        {
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

                        // Дозволяємо дірки
                        for (int gap = 1; gap <= maxGap; gap++)
                        {
                            if (i - gap >= 0) dp[i, j] = Math.Max(dp[i, j], dp[i - gap, j]);
                            if (j - gap >= 0) dp[i, j] = Math.Max(dp[i, j], dp[i, j - gap]);
                        }
                    }
                }
            }

            return dp[m, n];
        }


        public static int LongestStartSubsequence(int[] arr1, int[] arr2, int maxGap)
        {
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




    }
}
