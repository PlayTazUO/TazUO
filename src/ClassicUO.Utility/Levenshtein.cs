#nullable enable

using System;

namespace ClassicUO.Utility
{
    /// <summary>
    /// Pure, allocation-light Levenshtein (edit) distance
    /// </summary>
    public static class Levenshtein
    {
        private const int STACK_ALLOC_THRESHOLD = 256;

        /// <summary>
        /// Computes the edit distance between <paramref name="a"/> and <paramref name="b"/>,
        /// restricted to a band of <paramref name="maxDistance"/> around the diagonal. Any
        /// true distance greater than <paramref name="maxDistance"/> is reported as
        /// <c>maxDistance + 1</c> rather than its exact value (band-limited DP cannot recover
        /// the exact value for out-of-band cells, and callers only need the cutoff).
        /// </summary>
        public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int maxDistance = int.MaxValue)
        {
            if (a.Length < b.Length)
            {
                ReadOnlySpan<char> t = a;
                a = b;
                b = t;
            }

            int n = a.Length;
            int m = b.Length;

            if (maxDistance < 0)
            {
                maxDistance = 0;
            }

            if (n - m > maxDistance)
            {
                return maxDistance + 1;
            }

            // Band radius never needs to exceed the longer string's length.
            int k = Math.Min(maxDistance, n);
            int sentinel = k + 1;

            int width = m + 1;
            Span<int> prevBuf = width <= STACK_ALLOC_THRESHOLD ? stackalloc int[width] : new int[width];
            Span<int> currBuf = width <= STACK_ALLOC_THRESHOLD ? stackalloc int[width] : new int[width];

            for (int j = 0; j < width; j++)
            {
                prevBuf[j] = j <= k ? j : sentinel;
            }

            for (int i = 1; i <= n; i++)
            {
                int lo = Math.Max(1, i - k);
                int hi = Math.Min(m, i + k);

                currBuf[0] = i <= k ? i : sentinel;

                if (lo > 1)
                {
                    currBuf[lo - 1] = sentinel;
                }

                int rowMin = currBuf[0];

                for (int j = lo; j <= hi; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    int costLeft = currBuf[j - 1] + 1;
                    int costUp = prevBuf[j] + 1;
                    int costDiag = prevBuf[j - 1] + cost;

                    int value = Math.Min(costLeft, Math.Min(costUp, costDiag));
                    if (value > sentinel)
                    {
                        value = sentinel;
                    }

                    currBuf[j] = value;

                    if (value < rowMin)
                    {
                        rowMin = value;
                    }
                }

                if (hi < m)
                {
                    currBuf[hi + 1] = sentinel;
                }

                if (rowMin > maxDistance)
                {
                    // Every reachable cell in this row's band already exceeds the cutoff,
                    // and cells outside the band can only be more expensive.
                    return maxDistance + 1;
                }

                Span<int> tmp = prevBuf;
                prevBuf = currBuf;
                currBuf = tmp;
            }

            int result = prevBuf[m];
            return result > maxDistance ? maxDistance + 1 : result;
        }
    }
}
