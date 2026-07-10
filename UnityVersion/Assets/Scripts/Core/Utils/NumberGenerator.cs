using System;

namespace SichuanMahjong.Core.Utils
{
    public static class NumberGenerator
    {
        private static readonly Random random = new Random();

        public static int[] GenerateNumbers(int num, int sum)
        {
            int[] numbers = new int[num];
            int total = 0;

            for (int i = 0; i < num; i++)
            {
                numbers[i] = random.Next(sum) + 1;
                total += numbers[i];
            }

            for (int i = 0; i < num; i++)
            {
                numbers[i] = (int)Math.Round((float)numbers[i] * sum / total, MidpointRounding.AwayFromZero);
            }

            int adjust = sum - Sum(numbers);
            numbers[num - 1] += adjust;

            return numbers;
        }

        public static int Sum(int[] numbers)
        {
            int total = 0;
            foreach (int number in numbers)
            {
                total += number;
            }
            return total;
        }
    }
}
