using System.Collections.Concurrent;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Managers
{
    public class MoveItemQueue
    {
        public static MoveItemQueue Instance { get; private set; }
        
        public bool IsEmpty => _queue.IsEmpty;
        
        private readonly ConcurrentQueue<MoveRequest> _queue = new();

        public MoveItemQueue()
        {
            Instance = this;
        }

        public void Enqueue(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF, int z = 0)
        {
            _queue.Enqueue(new MoveRequest(serial, destination, amt, x, y, z));
        }

        public void EnqueueQuick(Item item)
        {
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return;
            }

            uint bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;
                
            Enqueue(item.Serial, bag, 0, 0xFFFF, 0xFFFF);
        }
        
        public void EnqueueQuick(uint serial)
        {
            Item i = World.Items.Get(serial);
            if(i != null)
                EnqueueQuick(i);
        }

        public void ProcessQueue()
        {
            if (GlobalActionCooldown.IsOnCooldown)
                return;
            
            if (Client.Game.GameCursor.ItemHold.Enabled)
                return;

            if (!_queue.TryDequeue(out var request))
                return;

            GameActions.PickUp(request.Serial, 0, 0, request.Amount);
            GameActions.DropItem(request.Serial, request.X, request.Y, request.Z, request.Destination);
            
            GlobalActionCooldown.BeginCooldown();
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out var _))
            {
            }
        }

        private readonly struct MoveRequest(uint serial, uint destination, ushort amount, int x, int y, int z)
        {
            public uint Serial { get; } = serial;
            public uint Destination { get; } = destination;
            public ushort Amount { get; } = amount;
            public int X { get; } = x;
            public int Y { get; } = y;
            public int Z { get; } = z;
        }
    }
}