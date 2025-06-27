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
        
        private static long delay = 1000;
        private readonly ConcurrentQueue<MoveRequest> _queue = new();
        private long nextMove;
        private World world;

        public MoveItemQueue(World world)
        {
            this.world = world;
            delay = ProfileManager.CurrentProfile.MoveMultiObjectDelay;
            Instance = this;
        }

        public void Enqueue(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF, int z = 0)
        {
            _queue.Enqueue(new MoveRequest(serial, destination, amt, x, y, z));
        }

        public void EnqueueQuick(Item item)
        {
            Item backpack = world.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return;
            }

            uint bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;
                
            Enqueue(item.Serial, bag, 0, 0xFFFF, 0xFFFF);
        }

        public void ProcessQueue()
        {
            if (Time.Ticks < nextMove)
                return;
            
            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return;

            if (!_queue.TryDequeue(out var request))
                return;

            GameActions.PickUp(world, request.Serial, 0, 0, request.Amount);
            GameActions.DropItem(request.Serial, request.X, request.Y, request.Z, request.Destination);
            
            nextMove = Time.Ticks + delay;
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