﻿
using System;

namespace PSUtility.Math
{
    public static class PrimeHelper
    {
        private static readonly int[] s_Primes = {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };

        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int num = (int)System.Math.Sqrt(candidate);
                for (int i = 3; i <= num; i += 2)
                {
                    if (candidate % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            return candidate == 2;
        }

        public static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException("min < 0");
            }

            for (int i = 0; i < s_Primes.Length; i++)
            {
                int prime = s_Primes[i];
                if (prime >= min)
                {
                    return prime;
                }
            }
            for (int i = min | 1; i < int.MaxValue; i += 2)
            {
                if (IsPrime(i) && (i - 1) % 101 != 0)
                {
                    return i;
                }
            }
            return min;
        }

        public static int ExpandPrime(int oldSize)
        {
            int num = 2 * oldSize;
            if (num > int.MaxValue && int.MaxValue > oldSize)
            {
                return int.MaxValue;
            }
            return GetPrime(num);
        }
    }
}