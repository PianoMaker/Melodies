namespace Melodies25.Utilities
{
    public static class Algorythm
    {

        
        public static int LongestCommonSubstring(int[] arr1, int[] arr2)
        {
            int[,] dp = new int[arr1.Length + 1, arr2.Length + 1];
            int maxLength = 0;

            for (int i = 1; i <= arr1.Length; i++)
            {
                for (int j = 1; j <= arr2.Length; j++)
                {
                    if (arr1[i - 1] == arr2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                        maxLength = Math.Max(maxLength, dp[i, j]);
                    }
                }
            }

            return maxLength;
        }

    }
}
