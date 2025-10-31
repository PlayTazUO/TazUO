// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Utility
{
    public static class RandomHelper
    {
        private static readonly Random _random = new Random();

        /// <summary>
        ///     Returns a random number between low and high, inclusive of both low and high.
        /// </summary>
        public static int GetValue(int low, int high) => _random.Next(low, high + 1);

        /// <summary>
        ///     Returns a non-negative random integer.
        /// </summary>
        public static int GetValue() => _random.Next();

        public static int RandomList(params int[] list) => list[_random.Next(list.Length)];

        public static bool RandomBool() => _random.NextDouble() >= 0.5;
    }
}