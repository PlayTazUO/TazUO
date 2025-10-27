using System.Collections.Concurrent;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    public class MoveItemQueue
    {
        public static MoveItemQueue Instance { get; private set; }

        public bool IsEmpty => _isEmpty;

        private bool _isEmpty = true;
        private readonly ConcurrentQueue<MoveRequest> _queue = new();
        private World world;

        public MoveItemQueue(World world)
        {
            this.world = world;
            Instance = this;
        }

        public void Enqueue(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF, int z = 0)
        {
            if(amt == 0)
            {
                Item i = world.Items.Get(serial);

                if (i != null)
                    amt = i.Amount;
                else
                    amt = 1;
            }

            _queue.Enqueue(new MoveRequest(serial, destination, amt, x, y, z));
            _isEmpty = false;
        }

        public void EnqueueQuick(Item item)
        {
            Item backpack = world.Player.Backpack;

            if (backpack == null)
            {
                return;
            }

            uint bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;

            Enqueue(item.Serial, bag, item.Amount, 0xFFFF, 0xFFFF);
        }

        public void EnqueueEquipSingle(uint serial, Layer layer)
        {
            Item i = world.Items.Get(serial);

            if (i == null) return;

            _queue.Enqueue(new MoveRequest(serial, uint.MaxValue, 1, 0xFFFF, 0xFFFF, 0, layer));
            _isEmpty = false;
        }

        public void EnqueueQuick(uint serial)
        {
            Item i = world.Items.Get(serial);
            if(i != null)
                EnqueueQuick(i);
        }

        public void ProcessQueue()
        {
            if (_isEmpty)
                return;

            if (GlobalActionCooldown.IsOnCooldown)
                return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return;

            if (!_queue.TryDequeue(out MoveRequest request))
                return;

            AsyncNetClient.Socket.Send_PickUpRequest(request.Serial, request.Amount);

            if(request.Destination != uint.MaxValue)
            {
                GameActions.DropItem(request.Serial, request.X, request.Y, request.Z, request.Destination, true);
            }
            else
            {
                AsyncNetClient.Socket.Send_EquipRequest(request.Serial, request.Layer, world.Player);
            }

            GlobalActionCooldown.BeginCooldown();
            _isEmpty = _queue.IsEmpty;
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out MoveRequest _))
            {
            }
            _isEmpty = true;
        }

        private readonly struct MoveRequest(uint serial, uint destination, ushort amount, int x, int y, int z, Layer layer = Layer.Invalid)
        {
            public uint Serial { get; } = serial;
            public uint Destination { get; } = destination;
            public ushort Amount { get; } = amount;
            public int X { get; } = x;
            public int Y { get; } = y;
            public int Z { get; } = z;

            public Layer Layer { get; } = layer;
        }
    }
}
