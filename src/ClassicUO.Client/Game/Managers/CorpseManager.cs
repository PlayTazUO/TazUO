// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    public sealed class CorpseManager
    {
        /// <summary>Cap on tracked opened corpse serials so the set cannot grow without bound.</summary>
        private const int MAX_OPENED_CORPSES = 10000;

        /// <summary>Serials of corpses already opened this session, used to honor DoNotReopenCorpses.</summary>
        private static readonly HashSet<uint> _openedCorpses = new HashSet<uint>();
        private static readonly Queue<uint> _openedCorpseOrder = new Queue<uint>();

        private readonly Deque<CorpseInfo> _corpses = new Deque<CorpseInfo>();
        private readonly World _world;

        public CorpseManager(World world)
        {
            _world = world;
        }

        /// <summary>Records a corpse as opened. Evicts the oldest entry once the cap is reached.</summary>
        public static void MarkCorpseOpened(uint serial)
        {
            if (serial == 0) return;

            if (!_openedCorpses.Add(serial)) return;

            _openedCorpseOrder.Enqueue(serial);

            while (_openedCorpseOrder.Count > MAX_OPENED_CORPSES && _openedCorpseOrder.TryDequeue(out uint oldest))
                _openedCorpses.Remove(oldest);
        }

        /// <summary>Whether the given corpse has already been opened this session.</summary>
        public static bool IsCorpseOpened(uint serial) => _openedCorpses.Contains(serial);

        public void Add(uint corpse, uint obj, Direction dir, bool run)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse)
                {
                    return;
                }
            }

            _corpses.AddToBack(new CorpseInfo(corpse, obj, dir, run));
        }

        public void Remove(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count;)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    if (corpse != 0)
                    {
                        Item item = _world.Items.Get(corpse);

                        if (item != null)
                        {
                            item.Layer = (Layer) ((c.Direction & Direction.Mask) | (c.IsRunning ? Direction.Running : 0));
                        }
                    }

                    _corpses.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public bool Exists(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    return true;
                }
            }

            return false;
        }

        public Item GetCorpseObject(uint serial)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.ObjectSerial == serial)
                {
                    return _world.Items.Get(c.CorpseSerial);
                }
            }

            return null;
        }

        public void Clear() => _corpses.Clear();
    }

    public struct CorpseInfo
    {
        public CorpseInfo(uint corpseSerial, uint objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public uint CorpseSerial, ObjectSerial;
        public Direction Direction;
        public bool IsRunning;
    }
}