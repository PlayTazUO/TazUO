using System;
using System.Diagnostics;
using System.Threading;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    /// <summary>Polling helper for asserting on state mutated by a background <see cref="Threading.Timer"/> callback.</summary>
    internal static class TestWait
    {
        public static bool Until(Func<bool> condition, int timeoutMs = 2000)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                    return true;

                Thread.Sleep(5);
            }

            return condition();
        }
    }
}
