// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Network
{
    /// <summary>
    /// Walk-pipeline counters for diagnosing request churn (gargoyle flight / speed-control cases).
    /// Totals are monotonic; <see cref="Update"/> snapshots 500 ms deltas for the network stats gump.
    /// </summary>
    public static class WalkDiagnostics
    {
        private static uint _nextUpdate;
        private static uint _lastWalkRequests,
            _lastDenyMatched,
            _lastDenyReset,
            _lastResyncs,
            _lastUpdatePlayer,
            _lastMovePlayer;

        /// <summary>Outgoing <c>0x02</c> walk requests.</summary>
        public static uint WalkRequests { get; private set; }

        /// <summary><see cref="Game.Managers.WalkerManager.DenyWalk"/> calls that matched a queued step.</summary>
        public static uint DenyMatched { get; private set; }

        /// <summary><see cref="Game.Managers.WalkerManager.DenyWalk"/> calls with no matching step (e.g. sequence <c>0xFF</c>), which clear the send gate.</summary>
        public static uint DenyReset { get; private set; }

        /// <summary><c>Send_Resync</c> calls from a bad confirm sequence.</summary>
        public static uint Resyncs { get; private set; }

        /// <summary>Incoming <c>0x20</c> player-state updates.</summary>
        public static uint UpdatePlayerPackets { get; private set; }

        /// <summary>Incoming <c>0x97</c> server-driven player moves.</summary>
        public static uint MovePlayerPackets { get; private set; }

        public static uint DeltaWalkRequests { get; private set; }
        public static uint DeltaDenyMatched { get; private set; }
        public static uint DeltaDenyReset { get; private set; }
        public static uint DeltaResyncs { get; private set; }
        public static uint DeltaUpdatePlayerPackets { get; private set; }
        public static uint DeltaMovePlayerPackets { get; private set; }

        /// <summary>Latest <c>LastStepRequestTime - Time.Ticks</c> in ms; negative means the gate is open.</summary>
        public static long GateAheadMs { get; set; }

        /// <summary>Step delay used by the last sent request, in ms. Equals the turn delay for pure turns.</summary>
        public static ushort LastWalkTimeMs { get; private set; }

        public static void OnWalkRequestSent(ushort walkTime)
        {
            WalkRequests++;
            LastWalkTimeMs = walkTime;
        }
        public static void OnDenyMatched() => DenyMatched++;
        public static void OnDenyReset() => DenyReset++;
        public static void OnResyncSent() => Resyncs++;
        public static void OnUpdatePlayerReceived() => UpdatePlayerPackets++;
        public static void OnMovePlayerReceived() => MovePlayerPackets++;

        public static void Reset()
        {
            _nextUpdate = 0;
            _lastWalkRequests =
                _lastDenyMatched =
                _lastDenyReset =
                _lastResyncs =
                _lastUpdatePlayer =
                _lastMovePlayer = 0;
            WalkRequests = DenyMatched = DenyReset = Resyncs = UpdatePlayerPackets = MovePlayerPackets = 0;
            DeltaWalkRequests = DeltaDenyMatched = DeltaDenyReset = DeltaResyncs = DeltaUpdatePlayerPackets = DeltaMovePlayerPackets = 0;
            GateAheadMs = 0;
            LastWalkTimeMs = 0;
        }

        public static void Update()
        {
            if (_nextUpdate > Time.Ticks)
            {
                return;
            }

            _nextUpdate = Time.Ticks + 500;

            DeltaWalkRequests = WalkRequests - _lastWalkRequests;
            DeltaDenyMatched = DenyMatched - _lastDenyMatched;
            DeltaDenyReset = DenyReset - _lastDenyReset;
            DeltaResyncs = Resyncs - _lastResyncs;
            DeltaUpdatePlayerPackets = UpdatePlayerPackets - _lastUpdatePlayer;
            DeltaMovePlayerPackets = MovePlayerPackets - _lastMovePlayer;

            _lastWalkRequests = WalkRequests;
            _lastDenyMatched = DenyMatched;
            _lastDenyReset = DenyReset;
            _lastResyncs = Resyncs;
            _lastUpdatePlayer = UpdatePlayerPackets;
            _lastMovePlayer = MovePlayerPackets;
        }
    }
}
