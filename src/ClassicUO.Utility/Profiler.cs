// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility
{
    public static class Profiler
    {
        private const string PACKET_CONTEXT_PREFIX = "Packet";

        public const int ProfileTimeCount = 60;
        public const double SpikeThresholdMultiplier = 3.0;
        public const double MinimumTimeForSpikeDetection = 2.0;
        private static readonly List<ContextAndTick> m_Context;
        private static readonly List<Tuple<string[], double>> m_ThisFrameData;
        private static readonly List<ProfileData> m_AllFrameData;
        private static readonly ProfileData m_TotalTimeData;
        private static readonly Stopwatch _timer;
        private static long m_BeginFrameTicks;

        public static List<ProfileData> AllFrameData => m_AllFrameData;

        static Profiler()
        {
            m_Context = new List<ContextAndTick>();
            m_ThisFrameData = new List<Tuple<string[], double>>();
            m_AllFrameData = new List<ProfileData>();
            m_TotalTimeData = new ProfileData(null, 0d);
            _timer = Stopwatch.StartNew();
        }

        public static double LastFrameTimeMS { get; private set; }

        public static double TrackedTime => m_TotalTimeData.TimeInContext;

        public static bool Enabled = false;

        //[Conditional("DEBUG")]
        public static void BeginFrame()
        {
            if (!Enabled)
            {
                return;
            }

            if (m_ThisFrameData.Count > 0)
            {
                foreach (Tuple<string[], double> t in m_ThisFrameData)
                {
                    bool added = false;

                    foreach (ProfileData t1 in m_AllFrameData)
                    {
                        if (t1.MatchesContext(t.Item1))
                        {
                            t1.AddNewHitLength(t.Item2);
                            added = true;

                            break;
                        }
                    }

                    if (!added)
                    {
                        m_AllFrameData.Add(new ProfileData(t.Item1, t.Item2));
                    }
                }

                m_ThisFrameData.Clear();
            }

            m_BeginFrameTicks = _timer.ElapsedTicks;
        }

        //[Conditional("DEBUG")]
        public static void EndFrame()
        {
            if (!Enabled)
            {
                return;
            }

            LastFrameTimeMS = (_timer.ElapsedTicks - m_BeginFrameTicks) * 1000d / Stopwatch.Frequency;
            m_TotalTimeData.AddNewHitLength(LastFrameTimeMS);
        }

        //[Conditional("DEBUG")]
        public static void EnterContext(string contextName)
        {
            if (!Enabled)
                return;

            m_Context.Add(new ContextAndTick(contextName, _timer.ElapsedTicks));
        }

        /// <summary>
        ///     Enters into a 'packet' context
        /// </summary>
        /// <remarks>
        ///     This is used to track the time spent processing a packet (generally an incoming one).
        ///     It differs from <see cref="EnterContext" /> in that it performs string allocation/interpolation only when
        ///     necessary, allow hot-path callers to maintain performance.
        /// </remarks>
        /// <param name="id">The packet's ID</param>
        public static void EnterPacketContext(byte id)
        {
            if (!Enabled)
                return;
            EnterContext($"{PACKET_CONTEXT_PREFIX} 0x{id:X2}");
        }

        /// <summary>
        ///     Exits from a 'packet' context
        /// </summary>
        /// <remarks>
        ///     This is used to track the time spent processing a packet (generally an incoming one).
        ///     It differs from <see cref="EnterContext" /> in that it performs string allocation/interpolation only when
        ///     necessary, allow hot-path callers to maintain performance.
        /// </remarks>
        /// <param name="id">The packet's ID</param>
        public static void ExitPacketContext(byte id)
        {
            if (!Enabled)
                return;
            ExitContext($"{PACKET_CONTEXT_PREFIX} 0x{id:X2}");
        }

        //[Conditional("DEBUG")]
        public static void ExitContext(string contextName, bool errorNotInContext = false)
        {
            if (!Enabled)
            {
                return;
            }

            if (m_Context.Count == 0 || m_Context[m_Context.Count - 1].Name != contextName)
            {
                if(errorNotInContext)
                    Log.Error("Profiler.ExitProfiledContext: context_name does not match current context.");
                return;
            }

            string[] context = new string[m_Context.Count];

            for (int i = 0; i < m_Context.Count; i++)
            {
                context[i] = m_Context[i].Name;
            }

            double ms = (_timer.ElapsedTicks - m_Context[m_Context.Count - 1].Tick) * 1000d / Stopwatch.Frequency;

            m_ThisFrameData.Add(new Tuple<string[], double>(context, ms));
            m_Context.RemoveAt(m_Context.Count - 1);
        }

        public static bool InContext(string context_name)
        {
            if (!Enabled)
                return false;

            if (m_Context.Count == 0)
                return false;

            return m_Context[m_Context.Count - 1].Name == context_name;
        }

        public static ProfileData GetContext(string context_name)
        {
            if (!Enabled)
            {
                return ProfileData.Empty;
            }

            for (int i = 0; i < m_AllFrameData.Count; i++)
            {
                if (m_AllFrameData[i].Context[m_AllFrameData[i].Context.Length - 1] == context_name)
                {
                    return m_AllFrameData[i];
                }
            }

            return ProfileData.Empty;
        }

        public static void Reset()
        {
            m_AllFrameData.Clear();
            m_ThisFrameData.Clear();
            m_Context.Clear();
        }

        public class ProfileData
        {
            public static ProfileData Empty = new ProfileData(null, 0d);
            private uint m_LastIndex;
            private readonly double[] m_LastTimes = new double[ProfileTimeCount];
            private double m_PeakTime;

            public ProfileData(string[] context, double time)
            {
                Context = context;
                m_LastIndex = 0;
                AddNewHitLength(time);
            }

            public double LastTime => m_LastTimes[m_LastIndex % ProfileTimeCount];
            public double PeakTime => m_PeakTime;

            public double TimeInContext
            {
                get
                {
                    double time = 0;

                    for (int i = 0; i < ProfileTimeCount; i++)
                    {
                        time += m_LastTimes[i];
                    }

                    return time;
                }
            }

            public double AverageTime => TimeInContext / ProfileTimeCount;
            public string[] Context;

            public bool MatchesContext(string[] context)
            {
                if (Context.Length != context.Length)
                {
                    return false;
                }

                for (int i = 0; i < Context.Length; i++)
                {
                    if (Context[i] != context[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public void AddNewHitLength(double time)
            {
                if (m_LastIndex >= ProfileTimeCount && time >= MinimumTimeForSpikeDetection)
                {
                    double currentAverage = AverageTime;
                    if (time > currentAverage * SpikeThresholdMultiplier)
                    {
                        string contextName = Context != null ? string.Join(":", Context) : "Unknown";
                        if(time < 20)
                            Log.Warn($"Performance spike detected in '{contextName}': {time:F2}ms (avg: {currentAverage:F2}ms, threshold: {currentAverage * SpikeThresholdMultiplier:F2}ms)");
                        else
                            Log.Error($"Major spike detected in '{contextName}': {time:F2}ms (avg: {currentAverage:F2}ms, threshold: {currentAverage * SpikeThresholdMultiplier:F2}ms)");
                    }
                }

                m_LastTimes[m_LastIndex % ProfileTimeCount] = time;
                m_LastIndex++;
                if (time > m_PeakTime)
                    m_PeakTime = time;
            }

            public override string ToString()
            {
                string name = string.Empty;

                for (int i = 0; i < Context.Length; i++)
                {
                    if (name != string.Empty)
                    {
                        name += ":";
                    }

                    name += Context[i];
                }

                return $"{name} - {TimeInContext:0.0}ms";
            }
        }

        private readonly struct ContextAndTick
        {
            public readonly string Name;
            public readonly long Tick;

            public ContextAndTick(string name, long tick)
            {
                Name = name;
                Tick = tick;
            }

            public override string ToString() => string.Format("{0} [{1}]", Name, Tick);
        }
    }
}
