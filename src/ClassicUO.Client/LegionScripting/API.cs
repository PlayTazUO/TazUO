using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.LegionScripting.PyClasses;
using ClassicUO.Network;
using ClassicUO.Utility;
using FontStashSharp.RichText;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using Microsoft.Xna.Framework;
using Button = ClassicUO.Game.UI.Controls.Button;
using Control = ClassicUO.Game.UI.Controls.Control;
using Label = ClassicUO.Game.UI.Controls.Label;
using Lock = ClassicUO.Game.Data.Lock;
using RadioButton = ClassicUO.Game.UI.Controls.RadioButton;
using CUOKeyboard = ClassicUO.Input.Keyboard;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Python scripting access point
    /// </summary>
    public class API : IDisposable
    {
        private volatile bool disposed = false;

        public API(ScriptEngine engine)
        {
            this.engine = engine;
            Events = new Events(engine, this);
        }

        internal ScriptEngine engine;

        private ConcurrentBag<Gump> gumps = new();

        #region Python Callback Queue

        private readonly Queue<Action> scheduledCallbacks = new();
        private static readonly ConcurrentDictionary<string, object> sharedVars = new();
        private readonly ConcurrentDictionary<string, object> hotkeyCallbacks = new();
        private readonly ConcurrentDictionary<string, bool> pressedKeys = new();

        internal void ScheduleCallback(Action action)
        {
            lock (scheduledCallbacks)
            {
                scheduledCallbacks.Enqueue(action);

                while (scheduledCallbacks.Count > 100)
                {
                    scheduledCallbacks.Dequeue(); //Limit callback counts
                    GameActions.Print(World, "Python Scripting Error: Too many callbacks registered!");
                }
            }
        }

        internal void ScheduleCallback(object callback, params object[] args)
        {
            if (callback == null || !engine.Operations.IsCallable(callback))
                return;

            ScheduleCallback(() =>
            {
                try
                {
                    engine.Operations.Invoke(callback, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Script callback error: {ex}");
                }
            });
        }

        /// <summary>
        /// Use this when you need to wait for players to click buttons.
        /// Example:
        /// ```py
        /// while True:
        ///   API.ProcessCallbacks()
        ///   API.Pause(0.1)
        /// ```
        /// </summary>
        public void ProcessCallbacks()
        {
            while (true)
            {
                Action next = null;

                lock (scheduledCallbacks)
                {
                    if (scheduledCallbacks.Count > 0)
                        next = scheduledCallbacks.Dequeue();
                }

                if (next != null)
                    next();
                else
                    break;
            }
        }

        #endregion

        private ConcurrentBag<uint> ignoreList = new();
        private ConcurrentQueue<PyJournalEntry> journalEntries = new();
        private World World = Client.Game.UO.World;
        private Item backpack;
        private PyPlayer player;
        private bool keyboardHooked = false;
        private readonly object hookLock = new object();

        private void EnsureKeyboardHook()
        {
            lock (hookLock)
            {
                if (keyboardHooked) return;

                CUOKeyboard.KeyDownEvent += OnKeyDown;
                CUOKeyboard.KeyUpEvent += OnKeyUp;

                keyboardHooked = true;
            }
        }

        private void OnKeyDown(string hotkey)
        {
            if (disposed) return;

            if (pressedKeys.TryAdd(hotkey, true) && hotkeyCallbacks.TryGetValue(hotkey, out object callback))
            {
                ScheduleCallback(callback);
            }
        }

        private void OnKeyUp(string hotkey)
        {
            if (disposed) return;

            pressedKeys.TryRemove(hotkey, out _);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (keyboardHooked)
            {
                CUOKeyboard.KeyDownEvent -= OnKeyDown;
                CUOKeyboard.KeyUpEvent -= OnKeyUp;
                keyboardHooked = false;
            }

            hotkeyCallbacks.Clear();
            pressedKeys.Clear();

        }

        public ConcurrentQueue<PyJournalEntry> JournalEntries => journalEntries;

        #region Properties

        /// <summary>
        /// Get the player's backpack serial
        /// </summary>
        public uint Backpack
        {
            get
            {
                if (backpack == null)
                    backpack = MainThreadQueue.InvokeOnMainThread(() => World.Player.Backpack);

                return backpack;
            }
        }


        /// <summary>
        /// Returns the player character object
        /// </summary>
        public PyPlayer Player
        {
            get
            {
                if (player == null)
                    player = MainThreadQueue.InvokeOnMainThread(() => new PyPlayer(World.Player));

                return player;
            }
        }

        /// <summary>
        /// Return the player's bank container serial if open, otherwise 0
        /// </summary>
        public uint Bank
        {
            get
            {
                Item i = MainThreadQueue.InvokeOnMainThread(() => World.Player.FindItemByLayer(Layer.Bank));
                return i != null ? i.Serial : 0;
            }
        }

        /// <summary>
        /// Can be used for random numbers.
        /// `API.Random.Next(1, 100)` will return a number between 1 and 100.
        /// `API.Random.Next(100)` will return a number between 0 and 100.
        /// </summary>
        public Random Random { get; set; } = new();

        /// <summary>
        /// The serial of the last target, if it has a serial.
        /// </summary>
        public uint LastTargetSerial => MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.LastTargetInfo.Serial);

        /// <summary>
        /// The last target's position
        /// </summary>
        public Vector3Int LastTargetPos => MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.LastTargetInfo.Position);

        /// <summary>
        /// The graphic of the last targeting object
        /// </summary>
        public ushort LastTargetGraphic => MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.LastTargetInfo.Graphic);

        /// <summary>
        /// The serial of the last item or mobile from the various findtype/mobile methods
        /// </summary>
        public uint Found { get; set; }

        /// <summary>
        /// Access useful player settings.
        /// </summary>
        public static PyProfile PyProfile = new();

        public Events Events;

        /// <summary>
        /// Check if the script has been requested to stop.
        /// ```py
        /// while not API.StopRequested:
        ///   DoSomeStuff()
        /// ```
        /// </summary>
        public volatile bool StopRequested;

        public CancellationTokenSource CancellationToken = new();

        #endregion

        #region Enum

        public enum ScanType
        {
            Hostile = 0,
            Party,
            Followers,
            Objects,
            Mobiles
        }

        public enum Notoriety : byte
        {
            Unknown = 0x00,
            Innocent = 0x01,
            Ally = 0x02,
            Gray = 0x03,
            Criminal = 0x04,
            Enemy = 0x05,
            Murderer = 0x06,
            Invulnerable = 0x07
        }

        public enum PersistentVar
        {
            Char,
            Account,
            Server,
            Global
        }

        #endregion

        #region Methods

        /// <summary>
        /// Register or unregister a Python callback for a hotkey.
        /// ### Register:
        /// ```py
        /// def on_shift_a():
        ///     API.SysMsg("SHIFT+A pressed!")
        /// API.OnHotKey("SHIFT+A", on_shift_a)
        /// while True:
        ///   API.ProcessCallbacks()
        ///   API.Pause(0.1)
        /// ```
        /// ### Unregister:
        /// ```py
        /// API.OnHotKey("SHIFT+A")
        /// ```
        /// The <paramref name="key"/> can include modifiers (CTRL, SHIFT, ALT),
        /// for example: "CTRL+SHIFT+F1" or "ALT+A".
        /// </summary>
        /// <param name="key">Key combination to listen for, e.g. "CTRL+SHIFT+F1".</param>
        /// <param name="callback">
        /// Python function to invoke when the hotkey is pressed.
        /// If <c>null</c>, the hotkey will be unregistered.
        /// </param>
        public void OnHotKey(string key, object callback = null)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (engine == null || engine.Operations == null)
            {
                return;
            }

            string normalized = CUOKeyboard.NormalizeKeyString(key);
            EnsureKeyboardHook();

            if (callback == null || !engine.Operations.IsCallable(callback))
            {
                hotkeyCallbacks.TryRemove(normalized, out _);
                return;
            }

            hotkeyCallbacks[normalized] = callback;
        }

        /// <summary>
        /// Set a variable that is shared between scripts.
        /// Example:
        /// ```py
        /// API.SetSharedVar("myVar", 10)
        /// ```
        /// </summary>
        /// <param name="name">Name of the var</param>
        /// <param name="value">Value, can be a number, text, or *most* other objects too.</param>
        public void SetSharedVar(string name, object value) => sharedVars[name] = value;

        /// <summary>
        /// Get the value of a shared variable.
        /// Example:
        /// ```py
        /// myVar = API.GetSharedVar("myVar")
        /// if myVar:
        ///  API.SysMsg(f"myVar is {myVar}")
        /// ```
        /// </summary>
        /// <param name="name">Name of the var</param>
        /// <returns></returns>
        public object GetSharedVar(string name)
        {
            if (sharedVars.TryGetValue(name, out object v))
                return v;
            return null;
        }

        /// <summary>
        /// Try to remove a shared variable.
        /// Example:
        /// ```py
        /// API.RemoveSharedVar("myVar")
        /// ```
        /// </summary>
        /// <param name="name">Name of the var</param>
        public void RemoveSharedVar(string name) => sharedVars.TryRemove(name, out _);

        /// <summary>
        /// Clear all shared vars.
        /// Example:
        /// ```py
        /// API.ClearSharedVars()
        /// ```
        /// </summary>
        public void ClearSharedVars() => sharedVars.Clear();

        /// <summary>
        /// Close all gumps created by the API unless marked to remain open.
        /// </summary>
        public void CloseGumps()
        {
            int c = 0;
            while (gumps.TryTake(out Gump g))
            {
                if (g is { IsDisposed: false })
                    MainThreadQueue.EnqueueAction(() => g?.Dispose());

                c++;

                if (c > 1000)
                    break; //Prevent infinite loop just in case.
            }
        }

        /// <summary>
        /// Attack a mobile
        /// Example:
        /// ```py
        /// enemy = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
        /// if enemy:
        ///   API.Attack(enemy)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        public void Attack(uint serial) => MainThreadQueue.InvokeOnMainThread(() => GameActions.Attack(World, serial));


        /// <summary>
        /// Sets the player's war mode state (peace/war toggle).
        /// </summary>
        /// <param name="enabled">True to enable war mode, false to disable war mode</param>
        public void SetWarMode(bool enabled) => MainThreadQueue.InvokeOnMainThread(() => GameActions.RequestWarMode(World.Player, enabled));

        /// <summary>
        /// Attempt to bandage yourself. Older clients this will not work, you will need to find a bandage, use it, and target yourself.
        /// Example:
        /// ```py
        /// if player.HitsMax - player.Hits > 10 or player.IsPoisoned:
        ///   if API.BandageSelf():
        ///     API.CreateCooldownBar(delay, "Bandaging...", 21)
        ///     API.Pause(8)
        ///   else:
        ///     API.SysMsg("WARNING: No bandages!", 32)
        ///     break
        /// ```
        /// </summary>
        /// <returns>True if bandages found and used</returns>
        public bool BandageSelf() => MainThreadQueue.InvokeOnMainThread(() => GameActions.BandageSelf(World));

        /// <summary>
        /// If you have an item in your left hand, move it to your backpack
        /// Sets API.Found to the item's serial.
        /// Example:
        /// ```py
        /// leftHand = API.ClearLeftHand()
        /// if leftHand:
        ///   API.SysMsg("Cleared left hand: " + leftHand.Name)
        /// ```
        /// </summary>
        /// <returns>The item that was in your hand</returns>
        public PyItem ClearLeftHand() => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Item i = World.Player.FindItemByLayer(Layer.OneHanded);

                if (i != null)
                {
                    Item bp = World.Player.Backpack;
                    MoveItemQueue.Instance.Enqueue(i, bp);
                    Found = i.Serial;
                    return new PyItem(i);
                }

                Found = 0;
                return null;
            }
        );

        /// <summary>
        /// If you have an item in your right hand, move it to your backpack
        /// Sets API.Found to the item's serial.
        /// Example:
        /// ```py
        /// rightHand = API.ClearRightHand()
        /// if rightHand:
        ///   API.SysMsg("Cleared right hand: " + rightHand.Name)
        ///  ```
        /// </summary>
        /// <returns>The item that was in your hand</returns>
        public PyItem ClearRightHand() => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Item i = World.Player.FindItemByLayer(Layer.TwoHanded);

                if (i != null)
                {
                    Item bp = World.Player.Backpack;
                    MoveItemQueue.Instance.Enqueue(i, bp);
                    Found = i.Serial;
                    return new PyItem(i);
                }

                Found = 0;
                return null;
            }
        );

        /// <summary>
        /// Single click an object
        /// Example:
        /// ```py
        /// API.ClickObject(API.Player)
        /// ```
        /// </summary>
        /// <param name="serial">Serial, or item/mobile reference</param>
        public void ClickObject(uint serial) => MainThreadQueue.InvokeOnMainThread(() => GameActions.SingleClick(World, serial));

        /// <summary>
        /// Attempt to use(double click) an object.
        /// Example:
        /// ```py
        /// API.UseObject(API.Backpack)
        /// ```
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void UseObject(uint serial, bool skipQueue = true) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (skipQueue)
                    GameActions.DoubleClick(World, serial);
                else
                    GameActions.DoubleClickQueued(serial);
            }
        );

        /// <summary>
        /// Get an item count for the contents of a container
        /// Example:
        /// ```py
        /// count = API.Contents(API.Backpack)
        /// if count > 0:
        ///   API.SysMsg(f"You have {count} items in your backpack")
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>The amount of items in a container. Does **not** include sub-containers, or item amounts. (100 Gold = 1 item if it's in a single stack)</returns>
        public int Contents(uint serial) => MainThreadQueue.InvokeOnMainThread<int>
        (() =>
            {
                Item i = World.Items.Get(serial);

                if (i != null)
                    return (int)Utility.ContentsCount(i);

                return 0;
            }
        );

        /// <summary>
        /// Send a context menu(right click menu) response.
        /// This does not open the menu, you do not need to open the menu first. This handles both in one action.
        /// Example:
        /// ```py
        /// API.ContextMenu(API.Player, 1)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="entry">Entries start at 0, the top entry will be 0, then 1, 2, etc. (Usually)</param>
        public void ContextMenu(uint serial, ushort entry) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                PopupMenuGump.CloseNext = serial;
                AsyncNetClient.Socket.Send_RequestPopupMenu(serial);
                AsyncNetClient.Socket.Send_PopupMenuSelection(serial, entry);
            }
        );

        /// <summary>
        /// Attempt to equip an item. Layer is automatically detected.
        /// Example:
        /// ```py
        /// lefthand = API.ClearLeftHand()
        /// API.Pause(2)
        /// API.EquipItem(lefthand)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        public void EquipItem(uint serial) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                GameActions.PickUp(World, serial, 0, 0, 1);
                GameActions.Equip(World);
            }
        );

        /// <summary>
        /// Clear the move item que of all items.
        /// </summary>
        public void ClearMoveQueue() => MainThreadQueue.InvokeOnMainThread(() => Client.Game.GetScene<GameScene>()?.MoveItemQueue.Clear());

        /// <summary>
        /// Move an item to another container.
        /// Use x, and y if you don't want items stacking in the desination container.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        ///
        /// API.SysMsg("Target your fish barrel", 32)
        /// barrel = API.RequestTarget()
        ///
        ///
        /// if len(items) > 0 and barrel:
        ///     for item in items:
        ///         data = API.ItemNameAndProps(item)
        ///         if data and "An Exotic Fish" in data:
        ///             API.QueueMoveItem(item, barrel)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="destination"></param>
        /// <param name="amt">Amount to move</param>
        /// <param name="x">X coordinate inside a container</param>
        /// <param name="y">Y coordinate inside a container</param>
        public void QueueMoveItem(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Client.Game.GetScene<GameScene>()?.MoveItemQueue.Enqueue(serial, destination, amt, x, y);
            }
        );

        /// <summary>
        /// Move an item to another container.
        /// Use x, and y if you don't want items stacking in the desination container.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        ///
        /// API.SysMsg("Target your fish barrel", 32)
        /// barrel = API.RequestTarget()
        ///
        ///
        /// if len(items) > 0 and barrel:
        ///     for item in items:
        ///         data = API.ItemNameAndProps(item)
        ///         if data and "An Exotic Fish" in data:
        ///             API.MoveItem(item, barrel)
        ///             API.Pause(0.75)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="destination"></param>
        /// <param name="amt">Amount to move</param>
        /// <param name="x">X coordinate inside a container</param>
        /// <param name="y">Y coordinate inside a container</param>
        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                GameActions.PickUp(World, serial, 0, 0, amt);
                GameActions.DropItem(serial, x, y, 0, destination);
            }
        );

        /// <summary>
        /// Move an item to the ground near you.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        /// for item in items:
        ///   API.QueueMoveItemOffset(item, 0, 1, 0, 0)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="amt">0 to grab entire stack</param>
        /// <param name="x">Offset from your location</param>
        /// <param name="y">Offset from your location</param>
        /// <param name="z">Offset from your location. Leave blank in most cases</param>
        /// <param name="OSI">True if you are playing OSI</param>
        public void QueueMoveItemOffset(uint serial, ushort amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                World.Map.GetMapZ(World.Player.X + x, World.Player.Y + y, out sbyte gz, out sbyte gz2);

                bool useCalculatedZ = false;

                if (gz > z)
                {
                    z = gz;
                    useCalculatedZ = true;
                }
                if (gz2 > z)
                {
                    z = gz2;
                    useCalculatedZ = true;
                }

                if (!useCalculatedZ)
                    z = World.Player.Z + z;

                Client.Game.GetScene<GameScene>()?.MoveItemQueue.Enqueue(serial, OSI ? uint.MaxValue : 0, amt, World.Player.X + x, World.Player.Y + y, z);
            }
        );

        /// <summary>
        /// Move an item to the ground near you.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        /// for item in items:
        ///   API.MoveItemOffset(item, 0, 1, 0, 0)
        ///   API.Pause(0.75)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="amt">0 to grab entire stack</param>
        /// <param name="x">Offset from your location</param>
        /// <param name="y">Offset from your location</param>
        /// <param name="z">Offset from your location. Leave blank in most cases</param>
        /// <param name="OSI">True if you are playing OSI</param>
        public void MoveItemOffset(uint serial, int amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                World.Map.GetMapZ(World.Player.X + x, World.Player.Y + y, out sbyte gz, out sbyte gz2);

                bool useCalculatedZ = false;

                if (gz > z)
                {
                    z = gz;
                    useCalculatedZ = true;
                }
                if (gz2 > z)
                {
                    z = gz2;
                    useCalculatedZ = true;
                }

                if (!useCalculatedZ)
                    z = World.Player.Z + z;

                GameActions.PickUp(World, serial, 0, 0, amt);
                GameActions.DropItem(serial, World.Player.X + x, World.Player.Y + y, z, OSI ? uint.MaxValue : 0);
            }
        );

        /// <summary>
        /// Use a skill.
        /// Example:
        /// ```py
        /// API.UseSkill("Hiding")
        /// API.Pause(11)
        /// ```
        /// </summary>
        /// <param name="skillName">Can be a partial match. Will match the first skill containing this text.</param>
        public void UseSkill(string skillName) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (skillName.Length > 0)
                {
                    for (int i = 0; i < World.Player.Skills.Length; i++)
                    {
                        if (World.Player.Skills[i].Name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            GameActions.UseSkill(World.Player.Skills[i].Index);

                            break;
                        }
                    }
                }
            }
        );

        /// <summary>
        /// Attempt to cast a spell by its name.
        /// Example:
        /// ```py
        /// API.CastSpell("Fireball")
        /// API.WaitForTarget()
        /// API.Target(API.Player)
        /// ```
        /// </summary>
        /// <param name="spellName">This can be a partial match. Fireba will cast Fireball.</param>
        public void CastSpell(string spellName) => MainThreadQueue.InvokeOnMainThread(() => { GameActions.CastSpellByName(spellName); });

        /// <summary>
        /// Dress from a saved dress configuration.
        /// Example:
        /// ```py
        /// API.Dress("PvP Gear")
        /// ```
        /// </summary>
        /// <param name="name">The name of the dress configuration</param>
        public void Dress(string name) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (string.IsNullOrEmpty(name))
                return;

            DressConfig config = DressAgentManager.Instance.CurrentPlayerConfigs
                .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (config != null)
            {
                DressAgentManager.Instance.DressFromConfig(config);
            }
        });

        /// <summary>
        /// Get all available dress configurations.
        /// Example:
        /// ```py
        /// outfits = API.GetAvailableDressOutfits()
        /// if outfits:
        ///   Dress(outfits[0])
        /// ```
        /// </summary>
        /// <returns>Returns a list of outfit names for use with Dress(outfitname)</returns>
        public PythonList GetAvailableDressOutfits() => MainThreadQueue.InvokeOnMainThread(() =>
        {
            PythonList list = new();
            foreach (DressConfig config in DressAgentManager.Instance.CurrentPlayerConfigs)
            {
                list.Add(config.Name);
            }

            return list;
        });

        /// <summary>
        /// Runs an organizer agent to move items between containers.
        /// Example:
        /// ```py
        /// # Run organizer with default containers
        /// API.Organizer("MyOrganizer")
        ///
        /// # Run organizer with specific source and destination
        /// API.Organizer("MyOrganizer", 0x40001234, 0x40005678)
        /// ```
        /// </summary>
        /// <param name="name">The name of the organizer configuration to run</param>
        /// <param name="source">Optional serial of the source container (0 for default)</param>
        /// <param name="destination">Optional serial of the destination container (0 for default)</param>
        public void Organizer(string name, uint source = 0, uint destination = 0)
        {
            if (string.IsNullOrEmpty(name))
            {
                GameActions.Print("Invalid organizer name", 32);
                return;
            }

            OrganizerAgent.Instance.RunOrganizer(name, source, destination);
        }

        /// <summary>
        /// Executes a client command as if typed in the game console
        /// </summary>
        /// <param name="command">The command to execute (including any arguments)</param>
        public void ClientCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                GameActions.Print("Command can't be empty", 32);
                return;
            }

            string[] split = command.Split(' ');

            World.Instance.CommandManager.Execute(split[0], split);
        }

        /// <summary>
        /// Check if a buff is active.
        /// Example:
        /// ```py
        /// if API.BuffExists("Bless"):
        ///   API.SysMsg("You are blessed!")
        /// ```
        /// </summary>
        /// <param name="buffName">The name/title of the buff</param>
        /// <returns></returns>
        public bool BuffExists(string buffName) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(buffName) || World == null || World.Player == null)
                    return false;

                foreach (BuffIcon buff in World.Player.BuffIcons.Values)
                {
                    if (buff == null) continue;

                    if (buff.Title.Contains(buffName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        );

        /// <summary>
        /// Get a list of all buffs that are active.
        /// See [Buff](Buff.md) to see what attributes are available.
        /// Buff does not get updated after you access it in python, you will need to call this again to get the latest buff data.
        /// Example:
        /// ```py
        /// buffs = API.ActiveBuffs()
        /// for buff in buffs:
        ///     API.SysMsg(buff.Title)
        /// ```
        /// </summary>
        /// <returns></returns>
        public Buff[] ActiveBuffs() => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World == null || World.Player == null) return new Buff[]{};

            List<Buff> buffs = new();

            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
            {
                buffs.Add(new Buff(buff));
            }

            return buffs.ToArray();
        });

        /// <summary>
        /// Show a system message(Left side of screen).
        /// Example:
        /// ```py
        /// API.SysMsg("Script started!")
        /// ```
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="hue">Color of the message</param>
        public void SysMsg(string message, ushort hue = 946)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => GameActions.Print(World, message, hue));
        }

        /// <summary>
        /// Say a message outloud.
        /// Example:
        /// ```py
        /// API.Say("Hello friend!")
        /// ```
        /// </summary>
        /// <param name="message">The message to say</param>
        public void Msg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.SpeechHue); });
        }

        /// <summary>
        /// Show a message above a mobile or item, this is only visible to you.
        /// Example:
        /// ```py
        /// API.HeadMsg("Only I can see this!", API.Player)
        /// ```
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="serial">The item or mobile</param>
        /// <param name="hue">Message hue</param>
        public void HeadMsg(string message, uint serial, ushort hue = ushort.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Entity e = World.Get(serial);

                if (e == null)
                    return;

                if (hue == ushort.MaxValue)
                    hue = ProfileManager.CurrentProfile.SpeechHue;

                World.MessageManager.HandleMessage(e, message, "", hue, MessageType.Label, 3, TextType.OBJECT);
            }
        );

        /// <summary>
        /// Send a message to your party.
        /// Example:
        /// ```py
        /// API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")
        /// ```
        /// </summary>
        /// <param name="message">The message</param>
        public void PartyMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.SayParty(message); });
        }

        /// <summary>
        /// Send your guild a message.
        /// Example:
        /// ```py
        /// API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void GuildMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild); });
        }

        /// <summary>
        /// Send a message to your alliance.
        /// Example:
        /// ```py
        /// API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void AllyMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance); });
        }

        /// <summary>
        /// Whisper a message.
        /// Example:
        /// ```py
        /// API.WhisperMsg("Psst, bet you didn't see me here..")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void WhisperMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper); });
        }

        /// <summary>
        /// Yell a message.
        /// Example:
        /// ```py
        /// API.YellMsg("Vendor restocked, get your fresh feathers!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void YellMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.YellHue, MessageType.Yell); });
        }

        /// <summary>
        /// Emote a message.
        /// Example:
        /// ```py
        /// API.EmoteMsg("laughing")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void EmoteMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote); });
        }

        /// <summary>
        /// Send a chat message via the global chat msg system ( ,message here ).
        /// </summary>
        /// <param name="message"></param>
        public void GlobalMsg(string message)
        {
            if (!string.IsNullOrEmpty(message))
                MainThreadQueue.InvokeOnMainThread(() => { AsyncNetClient.Socket.Send_ChatMessageCommand(message); });
        }

        /// <summary>
        /// Send a response to a server prompt(Like renaming a rune for example).
        /// </summary>
        /// <param name="message"></param>
        public void PromptResponse(string message) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.MessageManager.PromptData.Prompt != ConsolePrompt.None)
            {
                if (World.MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                {
                    AsyncNetClient.Socket.Send_ASCIIPromptResponse(World, message, message.Length < 1);
                }
                else if (World.MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                {
                    AsyncNetClient.Socket.Send_UnicodePromptResponse(World, message, Settings.GlobalSettings.Language, message.Length < 1);
                }

                World.MessageManager.PromptData = default;
            }
        });

        /// <summary>
        /// Try to get an item by its serial.
        /// Sets API.Found to the serial of the item found.
        /// Example:
        /// ```py
        /// donkey = API.RequestTarget()
        /// item = API.FindItem(donkey)
        /// if item:
        ///   API.SysMsg("Found the donkey!")
        ///   API.UseObject(item)
        /// ```
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <returns>The item object</returns>
        public PyItem FindItem(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Item i = World.Items.Get(serial);

            if (i != null)
            {
                Found = i.Serial;
                return new PyItem(i);
            }

            Found = 0;
            return null;
        });

        /// <summary>
        /// Attempt to find an item by type(graphic).
        /// Sets API.Found to the serial of the item found.
        /// Example:
        /// ```py
        /// item = API.FindType(0x0EED, API.Backpack)
        /// if item:
        ///   API.SysMsg("Found the item!")
        ///   API.UseObject(item)
        /// ```
        /// </summary>
        /// <param name="graphic">Graphic/Type of item to find</param>
        /// <param name="container">Container to search</param>
        /// <param name="range">Max range of item</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="minamount">Only match if item stack is at least this much</param>
        /// <returns>Returns the first item found that matches</returns>
        public PyItem FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);

                    foreach (Item i in result)
                    {
                        if (i.Amount >= minamount && !ignoreList.Contains(i))
                        {
                            Found = i.Serial;
                            return new PyItem(i);
                        }
                    }

                    Found = 0;
                    return null;
                }
            );

        /// <summary>
        /// Return a list of items matching the parameters set.
        /// Example:
        /// ```py
        /// items = API.FindTypeAll(0x0EED, API.Backpack)
        /// if items:
        ///   API.SysMsg("Found " + str(len(items)) + " items!")
        /// ```
        /// </summary>
        /// <param name="graphic">Graphic/Type of item to find</param>
        /// <param name="container">Container to search</param>
        /// <param name="range">Max range of item(if on ground)</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="minamount">Only match if item stack is at least this much</param>
        /// <returns></returns>
        public PyItem[] FindTypeAll(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            MainThreadQueue.InvokeOnMainThread
                (() =>
                {
                    Item[] list = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range)
                        .Where(i => !OnIgnoreList(i) && i.Amount >= minamount).ToArray();

                    List<PyItem> result = new();
                    foreach (Item item in list)
                    {
                        result.Add(new PyItem(item));
                    }

                    return result.ToArray();
                });

        /// <summary>
        /// Attempt to find an item on a layer.
        /// Sets API.Found to the serial of the item found.
        /// Example:
        /// ```py
        /// item = API.FindLayer("Helmet")
        /// if item:
        ///   API.SysMsg("Wearing a helmet!")
        /// ```
        /// </summary>
        /// <param name="layer">The layer to check, see https://github.com/PlayTazUO/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs</param>
        /// <param name="serial">Optional, if not set it will check yourself, otherwise it will check the mobile requested</param>
        /// <returns>The item if it exists</returns>
        public PyItem FindLayer(string layer, uint serial = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Found = 0;
                Mobile m = serial == uint.MaxValue ? World.Player : World.Mobiles.Get(serial);

                if (m != null)
                {
                    Layer matchedLayer = Utility.GetItemLayer(layer.ToLower());
                    Item item = m.FindItemByLayer(matchedLayer);

                    if (item != null)
                    {
                        Found = item.Serial;
                        return new PyItem(item);
                    }
                }

                return null;
            }
        );

        /// <summary>
        /// Get all items on the ground within specified range.
        /// Example:
        /// ```py
        /// items = API.GetItemsOnGround(10)  # All items within 10 tiles
        /// if items:
        ///   API.SysMsg("Found " + str(len(items)) + " items on ground!")
        /// ```
        /// </summary>
        /// <param name="distance">Optional max distance to search (default: no limit)</param>
        /// <param name="graphic">Optional graphic/type filter (default: no filter)</param>
        /// <param name="IsOSI">If true, looks for items in container with uint.MaxValue serial (OSI standard)</param>
        /// <returns>PythonList of items on ground, or null if none found</returns>
        public PythonList GetItemsOnGround(int distance = int.MaxValue, uint graphic = uint.MaxValue) =>
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                var resultList = new List<PyItem>();

                foreach (Item item in World.Items.Values)
                {
                    if (item.IsDestroyed || !item.OnGround || OnIgnoreList(item))
                        continue;

                    // Check distance if specified
                    if (distance != int.MaxValue && item.Distance > distance)
                        continue;

                    // Check graphic if specified
                    if (graphic != uint.MaxValue && item.Graphic != graphic)
                        continue;

                    resultList.Add(new PyItem(item));
                }

                if (resultList.Count == 0)
                    return null;

                var pythonList = new PythonList();
                foreach (PyItem item in resultList)
                {
                    pythonList.Add(item);
                }

                return pythonList;
            });

        /// <summary>
        /// Get all items in a container.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        /// if items:
        ///   API.SysMsg("Found " + str(len(items)) + " items!")
        ///   for item in items:
        ///     API.SysMsg(item.Name)
        ///     API.Pause(0.5)
        /// ```
        /// </summary>
        /// <param name="container"></param>
        /// <param name="recursive">Search sub containers also?</param>
        /// <returns>A list of items in the container</returns>
        public PyItem[] ItemsInContainer(uint container, bool recursive = false) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (!recursive)
            {
                Item[] list = Utility.FindItems(parentContainer: container).ToArray();
                List<PyItem> result = new();
                foreach (Item item in list)
                {
                    result.Add(new PyItem(item));
                }
                return result.ToArray();
            }

            List<PyItem> results = new();
            Stack<uint> containers = new();
            containers.Push(container);

            while (containers.Count > 0)
            {
                uint current = containers.Pop();

                foreach (Item item in Utility.FindItems(parentContainer: current))
                {
                    results.Add(new PyItem(item));
                    containers.Push(item.Serial);
                }
            }

            return results.ToArray();
        });

        /// <summary>
        /// Attempt to use the first item found by graphic(type).
        /// Example:
        /// ```py
        /// API.UseType(0x3434, API.Backpack)
        /// API.WaitForTarget()
        /// API.Target(API.Player)
        /// ```
        /// </summary>
        /// <param name="graphic">Graphic/Type</param>
        /// <param name="hue">Hue of item</param>
        /// <param name="container">Parent container</param>
        /// <param name="skipQueue">Defaults to true, set to false to queue the double click</param>
        public void UseType(uint graphic, ushort hue = ushort.MaxValue, uint container = uint.MaxValue, bool skipQueue = true) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                List<Item> result = Utility.FindItems(graphic, hue: hue, parentContainer: container);

                foreach (Item i in result)
                {
                    if (!ignoreList.Contains(i))
                    {
                        if (skipQueue)
                            GameActions.DoubleClick(World, i);
                        else
                            GameActions.DoubleClickQueued(i);

                        return;
                    }
                }
            }
        );

        /// <summary>
        /// Create a cooldown bar.
        /// Example:
        /// ```py
        /// API.CreateCooldownBar(5, "Healing", 21)
        /// ```
        /// </summary>
        /// <param name="seconds">Duration in seconds for the cooldown bar</param>
        /// <param name="text">Text on the cooldown bar</param>
        /// <param name="hue">Hue to color the cooldown bar</param>
        public void CreateCooldownBar(double seconds, string text, ushort hue) => MainThreadQueue.InvokeOnMainThread
            (() => { Game.Managers.CoolDownBarManager.AddCoolDownBar(World, TimeSpan.FromSeconds(seconds), text, hue, false); });

        /// <summary>
        /// Adds an item or mobile to your ignore list.
        /// These are unique lists per script. Ignoring an item in one script, will not affect other running scripts.
        /// Example:
        /// ```py
        /// for item in ItemsInContainer(API.Backpack):
        ///   if item.Name == "Dagger":
        ///   API.IgnoreObject(item)
        /// ```
        /// </summary>
        /// <param name="serial">The item/mobile serial</param>
        public void IgnoreObject(uint serial) => ignoreList.Add(serial);

        /// <summary>
        /// Clears the ignore list. Allowing functions to see those items again.
        /// Example:
        /// ```py
        /// API.ClearIgnoreList()
        /// ```
        /// </summary>
        public void ClearIgnoreList() => ignoreList = new();

        /// <summary>
        /// Check if a serial is on the ignore list.
        /// Example:
        /// ```py
        /// if API.OnIgnoreList(API.Backpack):
        ///   API.SysMsg("Currently ignoring backpack")
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>True if on the ignore list.</returns>
        public bool OnIgnoreList(uint serial) => ignoreList.Contains(serial);

        /// <summary>
        /// Attempt to pathfind to a location.  This will fail with large distances.
        /// Example:
        /// ```py
        /// API.Pathfind(1414, 1515)
        /// ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="distance">Distance away from goal to stop.</param>
        /// <param name="wait">True/False if you want to wait for pathfinding to complete or time out</param>
        /// <param name="timeout">Seconds to wait before cancelling waiting</param>
        /// <returns>true/false if a path was generated</returns>
        public bool Pathfind(int x, int y, int z = int.MinValue, int distance = 1, bool wait = false, int timeout = 10)
        {
            bool pathFindStatus = MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    if (z == int.MinValue)
                        z = World.Map.GetTileZ(x, y);

                    return World.Player.Pathfinder.WalkTo(x, y, z, distance);
                }
            );

            if (!wait)
                return pathFindStatus;

            if (timeout > 30)
                timeout = 30;

            DateTime expire = DateTime.Now.AddSeconds(timeout);

            while (MainThreadQueue.InvokeOnMainThread(() => World.Player.Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire)
                {
                    MainThreadQueue.InvokeOnMainThread(World.Player.Pathfinder.StopAutoWalk);
                    return false;
                }
            }

            MainThreadQueue.InvokeOnMainThread(World.Player.Pathfinder.StopAutoWalk);

            return MainThreadQueue.InvokeOnMainThread(() => World.Player.DistanceFrom(new Vector2(x, y)) <= distance);
        }

        /// <summary>
        /// Attempt to pathfind to a mobile or item.
        /// Example:
        /// ```py
        /// mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
        /// if mob:
        ///   API.PathfindEntity(mob)
        /// ```
        /// </summary>
        /// <param name="entity">The mobile or item</param>
        /// <param name="distance">Distance to stop from goal</param>
        /// <param name="wait">True/False if you want to wait for pathfinding to complete or time out</param>
        /// <param name="timeout">Seconds to wait before cancelling waiting</param>
        /// <returns>true/false if a path was generated</returns>
        public bool PathfindEntity(uint entity, int distance = 1, bool wait = false, int timeout = 10)
        {
            int x = 0, y = 0, z = 0;
            bool pathFindStatus = MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    Entity mob = World.Get(entity);
                    if (mob != null)
                    {
                        x = mob.X;
                        y = mob.Y;
                        z = mob.Z;
                        return World.Player.Pathfinder.WalkTo(x, y, z, distance);
                    }

                    return false;
                }
            );

            if (!wait || (x == 0 && y == 0))
                return pathFindStatus;

            if (timeout > 30)
                timeout = 30;

            DateTime expire = DateTime.Now.AddSeconds(timeout);

            while (MainThreadQueue.InvokeOnMainThread(() => World.Player.Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire)
                {
                    MainThreadQueue.InvokeOnMainThread(World.Player.Pathfinder.StopAutoWalk);
                    return false;
                }
            }

            MainThreadQueue.InvokeOnMainThread(World.Player.Pathfinder.StopAutoWalk);

            return MainThreadQueue.InvokeOnMainThread(() => World.Player.DistanceFrom(new Vector2(x, y)) <= distance);
        }

        /// <summary>
        /// Check if you are already pathfinding.
        /// Example:
        /// ```py
        /// if API.Pathfinding():
        ///   API.SysMsg("Pathfinding...!")
        ///   API.Pause(0.25)
        /// ```
        /// </summary>
        /// <returns>true/false</returns>
        public bool Pathfinding() => MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (World == null || World.Player == null)
                    return false;

                return World.Player.Pathfinder.AutoWalking;
            }
        );

        /// <summary>
        /// Cancel pathfinding.
        /// Example:
        /// ```py
        /// if API.Pathfinding():
        ///   API.CancelPathfinding()
        /// ```
        /// </summary>
        public void CancelPathfinding() => MainThreadQueue.InvokeOnMainThread(World.Player.Pathfinder.StopAutoWalk);

        /// <summary>
        /// Attempt to build a path to a location.  This will fail with large distances.
        /// Example:
        /// ```py
        /// API.RequestTarget()
        /// path = API.GetPath(int(API.LastTargetPos.X), int(API.LastTargetPos.Y))
        /// if path is not None:
        ///     for x, y, z in path:
        ///         tile = API.GetTile(x, y)
        ///         tile.Hue = 53
        /// ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="distance">Distance away from goal to stop.</param>
        /// <returns>Returns a list of positions to reach the goal. Returns null if cannot find path.</returns>
        public PythonList GetPath(int x, int y, int z = int.MinValue, int distance = 1) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (z == int.MinValue)
                z = World.Map.GetTileZ(x, y);

            List<(int X, int Y, int Z)> path = World.Player.Pathfinder.GetPathTo(x, y, z, distance);
            if (path is null)
            {
                return null;
            }

            var pythonList = new PythonList();
            foreach ((int X, int Y, int Z) p in path)
            {
                var tuple = new PythonTuple(new object[] { p.X, p.Y, p.Z });
                pythonList.Add(tuple);
            }

            return pythonList;
        });

        /// <summary>
        /// Automatically follow a mobile. This is different than pathfinding. This will continune to follow the mobile.
        /// Example:
        /// ```py
        /// mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
        /// if mob:
        ///   API.AutoFollow(mob)
        /// ```
        /// </summary>
        /// <param name="mobile">The mobile</param>
        public void AutoFollow(uint mobile) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                ProfileManager.CurrentProfile.FollowingMode = true;
                ProfileManager.CurrentProfile.FollowingTarget = mobile;
            }
        );

        /// <summary>
        /// Cancel auto follow mode.
        /// Example:
        /// ```py
        /// if API.Pathfinding():
        ///   API.CancelAutoFollow()
        /// ```
        /// </summary>
        public void CancelAutoFollow() => MainThreadQueue.InvokeOnMainThread(() => ProfileManager.CurrentProfile.FollowingMode = false);

        /// <summary>
        /// Run in a direction.
        /// Example:
        /// ```py
        /// API.Run("north")
        /// ```
        /// </summary>
        /// <param name="direction">north/northeast/south/west/etc</param>
        public void Run(string direction)
        {
            Direction d = Utility.GetDirection(direction);
            MainThreadQueue.InvokeOnMainThread(() => World.Player.Walk(d, true));
        }

        /// <summary>
        /// Walk in a direction.
        /// Example:
        /// ```py
        /// API.Walk("north")
        /// ```
        /// </summary>
        /// <param name="direction">north/northeast/south/west/etc</param>
        public void Walk(string direction)
        {
            Direction d = Utility.GetDirection(direction);
            MainThreadQueue.InvokeOnMainThread(() => World.Player.Walk(d, false));
        }

        /// <summary>
        /// Turn your character a specific direction.
        /// Example:
        /// ```py
        /// API.Turn("north")
        /// ```
        /// </summary>
        /// <param name="direction">north, northeast, etc</param>
        public void Turn(string direction) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Direction d = Utility.GetDirection(direction);

                if (d != Direction.NONE && World.Player.Direction != d)
                    World.Player.Walk(d, false);
            }
        );

        /// <summary>
        /// Attempt to rename something like a pet.
        /// Example:
        /// ```py
        /// API.Rename(0x12345678, "My Handsome Pet")
        /// ```
        /// </summary>
        /// <param name="serial">Serial of the mobile to rename</param>
        /// <param name="name">The new name</param>
        public void Rename(uint serial, string name) => MainThreadQueue.InvokeOnMainThread(() => { GameActions.Rename(serial, name); });

        /// <summary>
        /// Attempt to dismount if mounted.
        /// Example:
        /// ```py
        /// API.Dismount()
        /// ```
        /// </summary>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void Dismount(bool skipQueue = true) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player.FindItemByLayer(Layer.Mount) != null)
                {
                    if (skipQueue)
                        GameActions.DoubleClick(World, World.Player);
                    else
                        GameActions.DoubleClickQueued(World.Player);
                }
            }
        );

        /// <summary>
        /// Attempt to mount(double click)
        /// Example:
        /// ```py
        /// API.Mount(0x12345678)
        /// ```
        /// </summary>
        /// <param name="serial">Defaults to saved mount</param>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void Mount(uint serial = uint.MaxValue, bool skipQueue = true) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (serial == uint.MaxValue)
                    serial = ProfileManager.CurrentProfile.SavedMountSerial;

                if (skipQueue)
                    GameActions.DoubleClick(World, serial);
                else
                    GameActions.DoubleClickQueued(serial);
            }
        );

        /// <summary>
        /// This will set your saved mount for this character.
        /// </summary>
        /// <param name="serial"></param>
        public void SetMount(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            ProfileManager.CurrentProfile.SavedMountSerial = serial;
        });

        /// <summary>
        /// Wait for a target cursor.
        /// Example:
        /// ```py
        /// API.WaitForTarget()
        /// ```
        /// </summary>
        /// <param name="targetType">neutral/harmful/beneficial/any/harm/ben</param>
        /// <param name="timeout">Max duration in seconds to wait</param>
        /// <returns>True if target was matching the type, or false if not/timed out</returns>
        public bool WaitForTarget(string targetType = "any", double timeout = 5)
        {
            //Can't use Time.Ticks due to threading concerns
            DateTime expire = DateTime.UtcNow.AddSeconds(timeout);


            TargetType targetT = TargetType.Neutral;

            switch (targetType.ToLower())
            {
                case "harmful" or "harm": targetT = TargetType.Harmful; break;
                case "beneficial" or "ben": targetT = TargetType.Beneficial; break;
            }

            while (!MainThreadQueue.InvokeOnMainThread(() => { return World.TargetManager.IsTargeting && (World.TargetManager.TargetingType == targetT || targetType.ToLower() == "any"); }))
            {
                if (DateTime.UtcNow > expire)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Target an item or mobile.
        /// Example:
        /// ```py
        /// if API.WaitForTarget():
        ///   API.Target(0x12345678)
        /// ```
        /// </summary>
        /// <param name="serial">Serial of the item/mobile to target</param>
        public void Target(uint serial) => MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.Target(serial));

        /// <summary>
        /// Target a location. Include graphic if targeting a static.
        /// Example:
        /// ```py
        /// if API.WaitForTarget():
        ///   API.Target(1243, 1337, 0)
        ///  ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="graphic">Graphic of the static to target</param>
        public void Target(ushort x, ushort y, short z, ushort graphic = ushort.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (graphic == ushort.MaxValue)
                {
                    World.TargetManager.Target(0, x, y, z);
                }
                else
                {
                    World.TargetManager.Target(graphic, x, y, z);
                }
            }
        );

        /// <summary>
        /// Request the player to target something.
        /// Example:
        /// ```py
        /// target = API.RequestTarget()
        /// if target:
        ///   API.SysMsg("Targeted serial: " + str(target))
        /// ```
        /// </summary>
        /// <param name="timeout">Mac duration to wait for them to target something.</param>
        /// <returns>The serial of the object targeted</returns>
        public uint RequestTarget(double timeout = 5)
        {
            DateTime expire = DateTime.Now.AddSeconds(timeout);
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                World.TargetManager.LastTargetInfo.Clear();
                World.TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral);
            });

            while (DateTime.Now < expire)
                if (!MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.IsTargeting))
                    return World.TargetManager.LastTargetInfo.Serial;

            MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.Reset());

            return 0;
        }

        /// <summary>
        /// Prompts the player to target any object in the game world, including an <c>Item</c>, <c>Mobile</c>, <c>Land</c> tile, <c>Static</c>, or <c>Multi</c>.
        /// Waits for the player to select a target within a given timeout period.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time, in seconds, to wait for a valid target selection.
        /// If the timeout expires without a selection, the method returns <c>null</c>.
        /// </param>
        /// <returns>
        /// Returns a Python wrapper (<see cref="PyGameObject"/>) for the selected target:
        /// <list type="bullet">
        ///   <item><description><see cref="PyMobile"/> if a mobile (e.g. NPC, player) is targeted</description></item>
        ///   <item><description><see cref="PyItem"/> if an item is targeted</description></item>
        ///   <item><description><see cref="PyStatic"/> if a static tile (e.g. tree, building) is targeted</description></item>
        ///   <item><description><see cref="PyMulti"/> if a multi tile (e.g. a player house, boat) is targeted</description></item>
        ///   <item><description><see cref="PyLand"/> if a land tile (e.g. a base map tile at a coordinate) is targeted</description></item>
        ///   <item><description><c>null</c> if no valid target was selected within the timeout</description></item>
        /// </list>
        /// </returns>
        /// <example>
        /// Example usage in Python:
        /// <code>
        /// target = API.RequestAnyTarget()
        /// if target:
        ///     API.SysMsg(f"Targeted GameObject: {target}")
        /// else:
        ///     API.SysMsg("No target selected.")
        /// </code>
        /// </example>
        public PyGameObject RequestAnyTarget(double timeout = 5)
        {
            DateTime expire = DateTime.Now.AddSeconds(timeout);
            MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral));

            while (DateTime.Now < expire)
            {
                if (MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.IsTargeting))
                {
                    continue;
                }

                return MainThreadQueue.InvokeOnMainThread<PyGameObject>(() =>
                {
                    LastTargetInfo info = World.TargetManager.LastTargetInfo;
                    if (info.IsEntity)
                    {
                        if (SerialHelper.IsMobile(info.Serial))
                        {
                            Mobile mobile = World.Mobiles.Get(info.Serial);
                            return mobile is null ? null : new PyMobile(mobile);
                        }
                        else
                        {
                            Item item = World.Items.Get(info.Serial);
                            return item is null ? null : new PyItem(item);
                        }
                    }

                    if (info.IsStatic)
                    {
                        return World.GetStaticOrMulti(info.Graphic, info.X, info.Y, info.Z) switch
                        {
                            Static @static => new PyStatic(@static),
                            Multi multi => new PyMulti(multi),
                            _ => null
                        };
                    }

                    if (info.IsLand)
                    {
                        var land = World.Map.GetTile(info.X, info.Y) as Land;
                        return land is null ? null : new PyLand(land);
                    }

                    return null;
                });
            }

            MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.Reset());

            return null;
        }

        /// <summary>
        /// Target yourself.
        /// Example:
        /// ```py
        /// API.TargetSelf()
        /// ```
        /// </summary>
        public void TargetSelf() => MainThreadQueue.InvokeOnMainThread(() => World.TargetManager.Target(World.Player.Serial));

        /// <summary>
        /// Target a land tile relative to your position.
        /// If this doesn't work, try TargetTileRel instead.
        /// Example:
        /// ```py
        /// API.TargetLand(1, 1)
        /// ```
        /// </summary>
        /// <param name="xOffset">X from your position</param>
        /// <param name="yOffset">Y from your position</param>
        public void TargetLandRel(int xOffset, int yOffset) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (!World.TargetManager.IsTargeting)
                    return;

                ushort x = (ushort)(World.Player.X + xOffset);
                ushort y = (ushort)(World.Player.Y + yOffset);

                World.Map.GetMapZ(x, y, out sbyte gZ, out sbyte sZ);
                World.TargetManager.Target(0, x, y, gZ);
            }
        );

        /// <summary>
        /// Target a tile relative to your location.
        /// If this doesn't work, try TargetLandRel instead.'
        /// Example:
        /// ```py
        /// API.TargetTileRel(1, 1)
        /// ```
        /// </summary>
        /// <param name="xOffset">X Offset from your position</param>
        /// <param name="yOffset">Y Offset from your position</param>
        /// <param name="graphic">Optional graphic, will try to use the graphic of the tile at that location if left empty.</param>
        public void TargetTileRel(int xOffset, int yOffset, ushort graphic = ushort.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (!World.TargetManager.IsTargeting)
                    return;

                ushort x = (ushort)(World.Player.X + xOffset);
                ushort y = (ushort)(World.Player.Y + yOffset);
                short z = World.Player.Z;
                GameObject g = World.Map.GetTile(x, y);

                if (graphic == ushort.MaxValue && g != null)
                {
                    graphic = g.Graphic;
                    z = g.Z;
                }

                World.TargetManager.Target(graphic, x, y, z);
            }
        );

        /// <summary>
        /// This will attempt to use an item and target a resource, some servers may not support this.
        /// ```
        /// 0: ore
        /// 1: sand
        /// 2: wood
        /// 3: graves
        /// 4: red_mushrooms
        /// ```
        /// Example:
        /// ```py
        /// API.TargetResource(MY_SHOVEL_SERIAL, 0)
        /// ```
        /// </summary>
        /// <param name="itemSerial"></param>
        /// <param name="resource"></param>
        public void TargetResource(uint itemSerial, uint resource) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            AsyncNetClient.Socket.Send_TargetByResource(itemSerial, resource);
        });

        /// <summary>
        /// Cancel targeting.
        /// Example:
        /// ```py
        /// if API.WaitForTarget():
        ///   API.CancelTarget()
        ///   API.SysMsg("Targeting cancelled, april fools made you target something!")
        /// ```
        /// </summary>
        public void CancelTarget() => MainThreadQueue.InvokeOnMainThread(World.TargetManager.CancelTarget);

        /// <summary>
        /// Sets a pre-target that will be automatically applied when the next targeting request comes from the server.
        /// This is useful for automating actions that require targeting, like using bandages or spells.
        /// Example:
        /// ```py
        /// # Pre-target self for healing
        /// API.PreTarget(API.Player.Serial, "beneficial")
        /// API.UseObject(bandage_item)  # This will automatically target self when targeting request comes
        ///
        /// # Pre-target an enemy for attack spells
        /// enemy = API.FindMobile(mobile_serial)
        /// API.PreTarget(enemy.Serial, "harmful")
        /// API.CastSpell("Lightning")  # This will automatically target the enemy
        /// ```
        /// </summary>
        /// <param name="serial">Serial of the entity to pre-target</param>
        /// <param name="targetType">Type of target: "neutral"/"neut"/"n", "harmful"/"harm"/"h", "beneficial"/"ben"/"heal"/"b" (default: "neutral")</param>
        public void PreTarget(uint serial, string targetType = "neutral") => MainThreadQueue.InvokeOnMainThread(() =>
        {
            TargetType type;
            switch (targetType.ToLower())
            {
                case "harmful":
                case "harm":
                case "h":
                    type = TargetType.Harmful;
                    break;
                case "beneficial":
                case "ben":
                case "heal":
                case "b":
                    type = TargetType.Beneficial;
                    break;
                default:
                    type = TargetType.Neutral;
                    break;
            }

            TargetManager.SetAutoTarget(serial, type);
        });

        /// <summary>
        /// Cancels any active pre-target.
        /// Example:
        /// ```py
        /// API.PreTarget(enemy.Serial, "harmful")
        /// # Changed my mind, cancel the pre-target
        /// API.CancelPreTarget()
        /// ```
        /// </summary>
        public void CancelPreTarget() => MainThreadQueue.InvokeOnMainThread(() =>
        {
            TargetManager.NextAutoTarget.Clear();
        });

        /// <summary>
        /// Check if the player has a target cursor.
        /// Example:
        /// ```py
        /// if API.HasTarget():
        ///     API.CancelTarget()
        /// ```
        /// </summary>
        /// <param name="targetType">neutral/harmful/beneficial/any/harm/ben</param>
        /// <returns></returns>
        public bool HasTarget(string targetType = "any") => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                TargetType targetT = TargetType.Neutral;

                switch (targetType.ToLower())
                {
                    case "harmful" or "harm": targetT = TargetType.Harmful; break;
                    case "beneficial" or "ben": targetT = TargetType.Beneficial; break;
                }

                return World.TargetManager.IsTargeting && (World.TargetManager.TargetingType == targetT || targetType.ToLower() == "any");
            }
        );

        /// <summary>
        /// Get the current map index.
        /// Standard maps are:
        /// 0 = Fel
        /// 1 = Tram
        /// 2 = Ilshenar
        /// 3 = Malas
        /// 4 = Tokuno
        /// 5 = TerMur
        /// </summary>
        /// <returns></returns>
        public int GetMap() => MainThreadQueue.InvokeOnMainThread(() => World.MapIndex);

        /// <summary>
        /// Set a skills lock status.
        /// Example:
        /// ```py
        /// API.SetSkillLock("Hiding", "locked")
        /// ```
        /// </summary>
        /// <param name="skill">The skill name, can be partia;</param>
        /// <param name="up_down_locked">up/down/locked</param>
        public void SetSkillLock(string skill, string up_down_locked) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                skill = skill.ToLower();
                Game.Data.Lock status = Game.Data.Lock.Up;

                switch (up_down_locked)
                {
                    case "down": status = Game.Data.Lock.Down; break;
                    case "locked": status = Game.Data.Lock.Locked; break;
                }

                for (int i = 0; i < World.Player.Skills.Length; i++)
                {
                    if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                    {
                        World.Player.Skills[i].Lock = status;

                        break;
                    }
                }
            }
        );

        /// <summary>
        /// Set a skills lock status.
        /// Example:
        /// ```py
        /// API.SetStatLock("str", "locked")
        /// ```
        /// </summary>
        /// <param name="stat">The stat name, str, dex, int; Defaults to str.</param>
        /// <param name="up_down_locked">up/down/locked</param>
        public void SetStatLock(string stat, string up_down_locked) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                stat = stat.ToLower();
                Lock status = Lock.Up;

                switch (up_down_locked)
                {
                    case "down": status = Lock.Down; break;
                    case "locked": status = Lock.Locked; break;
                }

                byte statB = 0;

                switch (stat)
                {
                    case "dex": statB = 1; break;
                    case "int": statB = 2; break;
                }

                GameActions.ChangeStatLock(statB, status);
            }
        );

        /// <summary>
        /// Logout of the game.
        /// Example:
        /// ```py
        /// API.Logout()
        /// ```
        /// </summary>
        public void Logout() => MainThreadQueue.InvokeOnMainThread(() => GameActions.Logout(World));

        /// <summary>
        /// Gets item name and properties.
        /// This returns the name and properties in a single string. You can split it by new line if you want to separate them.
        /// Example:
        /// ```py
        /// data = API.ItemNameAndProps(0x12345678, True)
        /// if data:
        ///   API.SysMsg("Item data: " + data)
        ///   if "An Exotic Fish" in data:
        ///     API.SysMsg("Found an exotic fish!")
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="wait">True or false to wait for name and props</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <returns>Item name and properties, or empty if we don't have them.</returns>
        public string ItemNameAndProps(uint serial, bool wait = false, int timeout = 10)
        {
            if (wait)
            {
                DateTime expire = DateTime.UtcNow.AddSeconds(timeout);

                while (!MainThreadQueue.InvokeOnMainThread(() => World.OPL.Contains(serial)) && DateTime.UtcNow < expire)
                {
                    Thread.Sleep(100);
                }
            }

            return MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    if (World.OPL.TryGetNameAndData(serial, out string n, out string d))
                    {
                        return n + "\n" + d;
                    }

                    return string.Empty;
                }
            );
        }

        /// <summary>
        /// Requests Object Property List (OPL) data for the specified serials.
        /// If the OPL data doesn't already exist, it will be requested from the server.
        /// OPL consists of item name and tooltip text(properties).
        /// </summary>
        /// <param name="serials">A list of object serials to request OPL data for</param>
        public void RequestOPLData(IList<uint> serials) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            foreach (uint s in serials)
                World.OPL.Contains(s); //Check if it already exists, if not request it
        });

        /// <summary>
        /// Check if a player has a server gump. Leave blank to check if they have any server gump.
        /// Example:
        /// ```py
        /// if API.HasGump(0x12345678):
        ///   API.SysMsg("Found a gump!")
        ///```
        /// </summary>
        /// <param name="ID">Skip to check if player has any gump from server.</param>
        /// <returns>Returns gump id if found</returns>
        public uint HasGump(uint ID = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread<uint>
        (() =>
            {
                if (World.Player != null && World.Player.HasGump && (World.Player.LastGumpID == ID || ID == uint.MaxValue))
                {
                    if(UIManager.GetGumpServer(World.Player.LastGumpID) is { IsDisposed:false })
                        return World.Player.LastGumpID;

                    //Gump doesn't exist, let's reset this to false
                    World.Player.HasGump = false;
                }

                return 0;
            }
        );

        /// <summary>
        /// Reply to a gump.
        /// Example:
        /// ```py
        /// API.ReplyGump(21)
        /// ```
        /// </summary>
        /// <param name="button">Button ID</param>
        /// <param name="gump">Gump ID, leave blank to reply to last gump</param>
        /// <returns>True if gump was found, false if not</returns>
        public bool ReplyGump(int button, uint gump = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player == null)
                    return false;

                Gump g = UIManager.GetGumpServer(gump == uint.MaxValue ? World.Player.LastGumpID : gump);

                if (g != null)
                {
                    GameActions.ReplyGump(World, g.LocalSerial, g.ServerSerial, button, new uint[0] { }, new Tuple<ushort, string>[0]);
                    g.Dispose();

                    return true;
                }

                return false;
            }
        );

        /// <summary>
        /// Close the last gump open, or a specific gump.
        /// Example:
        /// ```py
        /// API.CloseGump()
        /// ```
        /// </summary>
        /// <param name="ID">Gump ID</param>
        public void CloseGump(uint ID = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player == null || ID == 0) //0 Prevents weird behaviour closing system chat gump
                    return;

                uint gump = ID != uint.MaxValue ? ID : World.Player.LastGumpID;
                UIManager.GetGumpServer(gump)?.Dispose();
            }
        );

        /// <summary>
        /// Configure how the next gump should be handled.
        /// Example:
        /// ```py
        /// # Position gump at coordinates
        /// API.ConfigNextGump(x=100, y=200)
        ///
        /// # Auto-close any gump
        /// API.ConfigNextGump(autoClose=True)
        ///
        /// # Auto-respond to specific gump
        /// API.ConfigNextGump(serial=0x12345678, autoRespond=True, autoRespondButton=1)
        ///
        /// # Clear configuration
        /// API.ConfigNextGump()
        ///
        /// Note: This is only applied once. You cannot stack multiple configs. This is reset after successfully applied and only applies to server-sent gumps.
        /// ```
        /// </summary>
        /// <param name="serial">Gump serial to match (0 = match any gump)</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="isVisible">Whether gump should be visible</param>
        /// <param name="autoClose">Automatically close the gump</param>
        /// <param name="autoRespond">Automatically respond to the gump</param>
        /// <param name="autoRespondButton">Button ID to use for auto-response</param>
        public void ConfigNextGump(
            uint? serial = null,
            int? x = null,
            int? y = null,
            bool? isVisible = null,
            bool? autoClose = null,
            bool? autoRespond = null,
            int? autoRespondButton = null
        ) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            // If no parameters are set, reset/clear the configuration
            if (serial == null && x == null && y == null && isVisible == null &&
                autoClose == null && autoRespond == null && autoRespondButton == null)
            {
                NextGumpConfig.Reset();
                return;
            }

            // Enable the configuration and apply provided parameters
            NextGumpConfig.Enabled = true;

            if (serial.HasValue)
                NextGumpConfig.Serial = serial.Value;

            if (x.HasValue)
                NextGumpConfig.X = x.Value;

            if (y.HasValue)
                NextGumpConfig.Y = y.Value;

            if (isVisible.HasValue)
                NextGumpConfig.IsVisible = isVisible.Value;

            if (autoClose.HasValue)
                NextGumpConfig.AutoClose = autoClose.Value;

            if (autoRespond.HasValue)
                NextGumpConfig.AutoRespond = autoRespond.Value;

            if (autoRespondButton.HasValue)
            {
                NextGumpConfig.AutoRespondButton = autoRespondButton.Value;
                NextGumpConfig.AutoRespond = true;
            }
        });

        /// <summary>
        /// Check if a gump contains a specific text.
        /// Example:
        /// ```py
        /// if API.GumpContains("Hello"):
        ///   API.SysMsg("Found the text!")
        /// ```
        /// </summary>
        /// <param name="text">Can be regex if you start with $, otherwise it's just regular search. Case Sensitive.</param>
        /// <param name="ID">Gump ID, blank to use the last gump.</param>
        /// <returns></returns>
        public bool GumpContains(string text, uint ID = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player == null)
                    return false;

                Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);

                if (g == null)
                    return false;

                bool regex = text.StartsWith("$");

                if (regex)
                    text = text.Substring(1);

                string allControlsText = string.Empty;

                foreach (Control c in g.Children)
                {
                    if (c is Label l)
                        allControlsText += l.Text + " ";
                    else if (c is HtmlControl ht)
                        allControlsText += ht.Text + " ";
                }

                if (allControlsText.Contains(text) || (regex && RegexHelper.GetRegex(text).IsMatch(allControlsText)))
                    return true;

                return false;
            }
        );

        /// <summary>
        /// This will return a string of all the text in a server-side gump.
        /// </summary>
        /// <param name="ID">Gump ID, blank to use the last gump.</param>
        /// <returns></returns>
        public string GetGumpContents(uint ID = uint.MaxValue)
        {
            if (World.Player == null)
                return string.Empty;

            Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);

            if (g == null)
                return string.Empty;

            string allControlsText = string.Empty;

            foreach (Control c in g.Children)
            {
                if (c is Label l)
                    allControlsText += l.Text + " ";
                else if (c is HtmlControl ht)
                    allControlsText += ht.Text + " ";
            }

            return allControlsText;
        }

        /// <summary>
        /// Get a gump by ID.
        /// Example:
        /// ```py
        /// gump = API.GetGump()
        /// if gump:
        ///   API.SysMsg("Found the gump!")
        ///   gump.Dispose() #Close it
        /// ```
        /// </summary>
        /// <param name="ID">Leave blank to use last gump opened from server</param>
        /// <returns></returns>
        public Gump GetGump(uint ID = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player == null)
                    return null;

                Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);

                return g;
            }
        );

        /// <summary>
        /// Gets all currently open server-side gumps.
        /// </summary>
        /// <returns>A PythonList containing all open server gumps, or null if none are open</returns>
        public PythonList GetAllGumps() => MainThreadQueue.InvokeOnMainThread<PythonList>(() =>
        {
            Gump[] serverGumps = UIManager.Gumps.Where(g => g.ServerSerial > 0).ToArray();

            if (!serverGumps.Any())
                return null;

            PythonList list = new();
            list.AddRange(serverGumps);
            return list;
        });

        /// <summary>
        /// Wait for a server-side gump.
        /// Example:
        /// ```py
        /// if API.WaitForGump(1951773915):
        ///   API.HeadMsg("SUCCESS", API.Player, 62)
        /// else:
        ///  API.HeadMsg("FAILURE", API.Player, 32)
        /// ```
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="delay">Seconds to wait</param>
        /// <returns></returns>
        public bool WaitForGump(uint ID = uint.MaxValue, double delay = 5)
        {
            if (World.Player == null)
                return false;

            DateTime expire = DateTime.UtcNow.AddSeconds(delay);

            if (ID == uint.MaxValue)
                ID = World.Player.LastGumpID;

            while (!MainThreadQueue.InvokeOnMainThread(() => UIManager.GetGumpServer(ID) != null))
            {
                if (DateTime.UtcNow > expire)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Toggle flying if you are a gargoyle.
        /// Example:
        /// ```py
        /// API.ToggleFly()
        /// ```
        /// </summary>
        public void ToggleFly() => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (World.Player != null && World.Player.Race == RaceType.GARGOYLE)
                    AsyncNetClient.Socket.Send_ToggleGargoyleFlying();
            }
        );

        /// <summary>
        /// Toggle an ability.
        /// Example:
        /// ```py
        /// if not API.PrimaryAbilityActive():
        ///   API.ToggleAbility("primary")
        /// ```
        /// </summary>
        /// <param name="ability">primary/secondary/stun/disarm</param>
        public void ToggleAbility(string ability) =>
            MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    switch (ability.ToLower())
                    {
                        case "primary": GameActions.UsePrimaryAbility(World); break;

                        case "secondary": GameActions.UseSecondaryAbility(World); break;

                        case "stun": AsyncNetClient.Socket.Send_StunRequest(); break;

                        case "disarm": AsyncNetClient.Socket.Send_DisarmRequest(); break;
                    }
                }
            );

        /// <summary>
        /// Check if your primary ability is active.
        /// Example:
        /// ```py
        /// if API.PrimaryAbilityActive():
        ///   API.SysMsg("Primary ability is active!")
        /// ```
        /// </summary>
        /// <returns>true/false</returns>
        public bool PrimaryAbilityActive() => World.Player != null && ((byte)World.Player.PrimaryAbility & 0x80) != 0;

        /// <summary>
        /// Check if your secondary ability is active.
        /// Example:
        /// ```py
        /// if API.SecondaryAbilityActive():
        ///   API.SysMsg("Secondary ability is active!")
        /// ```
        /// </summary>
        /// <returns>true/false</returns>
        public bool SecondaryAbilityActive() => World.Player != null && ((byte)World.Player.SecondaryAbility & 0x80) != 0;

        /// <summary>
        /// Check if your journal contains a message.
        /// Example:
        /// ```py
        /// if API.InJournal("You have been slain"):
        ///   API.SysMsg("You have been slain!")
        /// ```
        /// </summary>
        /// <param name="msg">The message to check for. Can be regex, prepend your msg with $</param>
        /// <returns>True if message was found</returns>
        public bool InJournal(string msg, bool clearMatches = false)
        {
            if (string.IsNullOrEmpty(msg))
                return false;

            foreach (PyJournalEntry je in JournalEntries.ToArray())
            {
                if (je.Disposed) continue;

                if (msg.StartsWith("$") && Regex.IsMatch(je.Text, msg.Substring(1)))
                {
                    if (clearMatches)
                        je.Disposed = true;
                    return true;
                }

                if (je.Text.Contains(msg))
                {
                    if (clearMatches)
                        je.Disposed = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the journal contains *any* of the strings in this list.
        /// Can be regex, prepend your msgs with $
        /// Example:
        /// ```py
        /// if API.InJournalAny(["You have been slain", "You are dead"]):
        ///   API.SysMsg("You have been slain or dead!")
        /// ```
        /// </summary>
        /// <param name="msgs"></param>
        /// <param name="clearMatches"></param>
        /// <returns></returns>
        public bool InJournalAny(IList<string> msgs, bool clearMatches = false)
        {
            if (msgs == null || msgs.Count == 0)
                return false;

            foreach (PyJournalEntry je in JournalEntries.ToArray())
            {
                if (je.Disposed) continue;

                foreach (string msg in msgs)
                {
                    if (msg.StartsWith("$") && Regex.IsMatch(je.Text, msg.Substring(1)))
                    {
                        if (clearMatches)
                            je.Disposed = true;
                        return true;
                    }

                    if (je.Text.Contains(msg))
                    {
                        if (clearMatches)
                            je.Disposed = true;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get all the journal entires in the last X seconds.
        /// matchingText supports regex with $ prepended.
        /// Example:
        /// ```py
        /// list = API.GetJournalEntries(30)
        /// if list:
        ///   for entry in list:
        ///     entry.Text # Do something with this
        /// ```
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="matchingText">Only add if text matches</param>
        /// <returns>A list of JournalEntry's</returns>
        public PythonList GetJournalEntries(double seconds, string matchingText = "")
        {
            var entries = new PythonList();

            DateTime cutoff = DateTime.Now - TimeSpan.FromSeconds(seconds);

            bool checkMatches = !string.IsNullOrEmpty(matchingText);

            foreach (PyJournalEntry je in JournalEntries)
            {
                if (je.Time < cutoff || je.Disposed)
                    continue;

                if (!checkMatches)
                {
                    entries.Add(je);
                    continue;
                }

                if (matchingText.StartsWith("$") && RegexHelper.GetRegex(matchingText.Substring(1)).IsMatch(je.Text))
                {
                    entries.Add(je);
                    continue;
                }

                if (je.Text.Contains(matchingText))
                {
                    entries.Add(je);
                }
            }

            if (entries.Count > 0)
                return entries;

            return null;
        }

        /// <summary>
        /// Clear your journal(This is specific for each script).
        /// Supports regex matching if prefixed with $
        /// Example:
        /// ```py
        /// API.ClearJournal()
        /// ```
        /// </summary>
        /// <param name=""></param>
        /// <param name="matchingEntries">String or regex to match with. If this is set, only matching entries will be removed.</param>
        public void ClearJournal(string matchingEntries = "")
        {
            if (string.IsNullOrEmpty(matchingEntries))
            {
                while (JournalEntries.TryDequeue(out _))
                {
                }
            }
            else
            {
                ConcurrentQueue<PyJournalEntry> newQueue = new();

                foreach (PyJournalEntry je in JournalEntries.ToArray())
                {
                    if (matchingEntries.StartsWith("$") && RegexHelper.GetRegex(matchingEntries.Substring(1)).IsMatch(je.Text))
                    {
                        je.Disposed = true;
                        continue;
                    }

                    if (je.Text.Contains(matchingEntries))
                    {
                        je.Disposed = true;
                        continue;
                    }

                    newQueue.Enqueue(je);
                }

                Interlocked.Exchange(ref journalEntries, newQueue);
            }
        }

        /// <summary>
        /// Pause the script.
        /// Example:
        /// ```py
        /// API.Pause(5)
        /// ```
        /// </summary>
        /// <param name="seconds">0-30 seconds.</param>
        public void Pause(double seconds)
        {
            seconds = Math.Clamp(seconds, 0, 30);

            Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: CancellationToken.Token).Wait(cancellationToken: CancellationToken.Token);

            if (StopRequested)
                throw new ThreadInterruptedException();
        }

        /// <summary>
        /// Stops the current script.
        /// Example:
        /// ```py
        /// API.Stop()
        /// ```
        /// </summary>
        public void Stop()
        {
            int t = Thread.CurrentThread.ManagedThreadId;

            MainThreadQueue.InvokeOnMainThread
            (() =>
                {
                    if (LegionScripting.PyThreads.TryGetValue(t, out ScriptFile s))
                        LegionScripting.StopScript(s);
                }
            );
        }

        /// <summary>
        /// Toggle autolooting on or off.
        /// Example:
        /// ```py
        /// API.ToggleAutoLoot()
        /// ```
        /// </summary>
        public void ToggleAutoLoot() => MainThreadQueue.InvokeOnMainThread(() => { ProfileManager.CurrentProfile.EnableAutoLoot ^= true; });

        /// <summary>
        /// Use autoloot on a specific container.
        /// Example:
        /// ```py
        /// targ = API.RequestTarget()
        /// if targ:
        ///   API.AutoLootContainer(targ)
        /// ```
        /// </summary>
        /// <param name="container"></param>
        public void AutoLootContainer(uint container) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            AutoLootManager.Instance?.ForceLootContainer(container);
        });

        /// <summary>
        /// Use a virtue.
        /// Example:
        /// ```py
        /// API.Virtue("honor")
        /// ```
        /// </summary>
        /// <param name="virtue">honor/sacrifice/valor</param>
        public void Virtue(string virtue)
        {
            switch (virtue.ToLower())
            {
                case "honor": MainThreadQueue.InvokeOnMainThread(() => { AsyncNetClient.Socket.Send_InvokeVirtueRequest(0x01); }); break;
                case "sacrifice": MainThreadQueue.InvokeOnMainThread(() => { AsyncNetClient.Socket.Send_InvokeVirtueRequest(0x02); }); break;
                case "valor": MainThreadQueue.InvokeOnMainThread(() => { AsyncNetClient.Socket.Send_InvokeVirtueRequest(0x03); }); break;
            }
        }

        /// <summary>
        /// Find the nearest item/mobile based on scan type.
        /// Sets API.Found to the serial of the item/mobile.
        /// Example:
        /// ```py
        /// item = API.NearestEntity(API.ScanType.Item, 5)
        /// if item:
        ///   API.SysMsg("Found an item!")
        ///   API.UseObject(item)
        ///   # You can use API.FindItem or API.FindMobile(item.Serial) to determine if it's an item or mobile
        /// ```
        /// </summary>
        /// <param name="scanType"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public PyEntity NearestEntity(ScanType scanType, int maxDistance = 10) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Found = 0;
                uint m = Utility.FindNearestCheckPythonIgnore((ScanTypeObject)scanType, this);

                Entity e = World.Get(m);

                if (e != null && e.Distance <= maxDistance)
                {
                    Found = e.Serial;
                    return new PyEntity(e);
                }

                return null;
            }
        );

        /// <summary>
        /// Get the nearest mobile by Notoriety.
        /// Sets API.Found to the serial of the mobile.
        /// Example:
        /// ```py
        /// mob = API.NearestMobile([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)
        /// if mob:
        ///   API.SysMsg("Found a criminal!")
        ///   API.Msg("Guards!")
        ///   API.Attack(mob)
        ///   ```
        /// </summary>
        /// <param name="notoriety">List of notorieties</param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public PyMobile NearestMobile(IList<Notoriety> notoriety, int maxDistance = 10) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                Found = 0;
                if (notoriety == null || notoriety.Count == 0)
                    return null;

                Mobile mob = World.Mobiles.Values.Where
                (m => !m.IsDestroyed && !m.IsDead && m.Serial != World.Player.Serial && notoriety.Contains
                     ((Notoriety)(byte)m.NotorietyFlag) && m.Distance <= maxDistance && !OnIgnoreList(m)
                ).OrderBy(m => m.Distance).FirstOrDefault();

                if (mob != null)
                {
                    Found = mob.Serial;
                    return new PyMobile(mob);
                }

                return null;
            }
        );

        /// <summary>
        /// Get the nearest corpse within a distance.
        /// Sets API.Found to the serial of the corpse.
        /// Example:
        /// ```py
        /// corpse = API.NearestCorpse()
        /// if corpse:
        ///   API.SysMsg("Found a corpse!")
        ///   API.UseObject(corpse)
        /// ```
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public PyItem NearestCorpse(int distance = 3) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Found = 0;
            Item c = Utility.FindNearestCorpsePython(distance, this);

            if (c != null)
            {
                Found = c.Serial;
                return new PyItem(c);
            }

            return null;
        });

        /// <summary>
        /// Get all mobiles matching Notoriety and distance.
        /// Example:
        /// ```py
        /// mob = API.NearestMobiles([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)
        /// if len(mob) > 0:
        ///   API.SysMsg("Found enemies!")
        ///   API.Msg("Guards!")
        ///   API.Attack(mob[0])
        ///   ```
        /// </summary>
        /// <param name="notoriety">List of notorieties</param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public PyMobile[] NearestMobiles(IList<Notoriety> notoriety, int maxDistance = 10) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (notoriety == null || notoriety.Count == 0)
                    return null;

                Mobile[] list = World.Mobiles.Values.Where
                (m => !m.IsDestroyed && !m.IsDead && m.Serial != World.Player.Serial && notoriety.Contains
                     ((Notoriety)(byte)m.NotorietyFlag) && m.Distance <= maxDistance && !OnIgnoreList(m)
                ).OrderBy(m => m.Distance).ToArray();

                return list.Select(m => new PyMobile(m)).ToArray();
            }
        );

        /// <summary>
        /// Get a mobile from its serial.
        /// Sets API.Found to the serial of the mobile.
        /// Example:
        /// ```py
        /// mob = API.FindMobile(0x12345678)
        /// if mob:
        ///   API.SysMsg("Found the mobile!")
        ///   API.UseObject(mob)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>The mobile or null</returns>
        public PyMobile FindMobile(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Found = 0;

            Mobile mob = World.Mobiles.Get(serial);

            if (mob != null)
            {
                Found = mob.Serial;
                return new PyMobile(mob);
            }

            return null;
        });

        /// <summary>
        /// Return a list of all mobiles the client is aware of, optionally filtered by graphic, distance, and/or notoriety.
        /// Example:
        /// ```py
        /// # Get all mobiles
        /// mobiles = API.GetAllMobiles()
        /// # Get all mobiles with graphic 400
        /// humans = API.GetAllMobiles(400)
        /// # Get all humans within 5 tiles
        /// nearby_humans = API.GetAllMobiles(400, 5)
        /// # Get all enemies (murderers and criminals) within 15 tiles
        /// enemies = API.GetAllMobiles(distance=15, notoriety=[API.Notoriety.Murderer, API.Notoriety.Criminal])
        /// ```
        /// </summary>
        /// <param name="graphic">Optional graphic ID to filter by</param>
        /// <param name="distance">Optional maximum distance from player</param>
        /// <param name="notoriety">Optional list of notoriety flags to filter by</param>
        /// <returns></returns>
        public PyMobile[] GetAllMobiles(ushort? graphic = null, int? distance = null, IList<Notoriety> notoriety = null) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            IEnumerable<Mobile> mobiles = World.Mobiles.Values.AsEnumerable();

            if (graphic.HasValue)
                mobiles = mobiles.Where(m => m.Graphic == graphic.Value);

            if (distance.HasValue)
                mobiles = mobiles.Where(m => m.Distance <= distance.Value);

            if (notoriety != null && notoriety.Count > 0)
                mobiles = mobiles.Where(m => notoriety.Contains((Notoriety)(byte)m.NotorietyFlag));

            return mobiles.Select(m => new PyMobile(m)).ToArray();
        });

        /// <summary>
        /// Get the tile at a location.
        /// Example:
        /// ```py
        /// tile = API.GetTile(1414, 1515)
        /// if tile:
        ///   API.SysMsg(f"Found a tile with graphic: {tile.Graphic}")
        /// ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>A GameObject of that location.</returns>
        public PyGameObject GetTile(int x, int y) => MainThreadQueue.InvokeOnMainThread(() => { return new PyGameObject(World.Map.GetTile(x, y)); });

        /// <summary>
        /// Gets all static objects at a specific position (x, y coordinates).
        /// This includes trees, vegetation, buildings, and other non-movable scenery.
        /// Example:
        /// ```py
        /// statics = API.GetStaticsAt(1000, 1000)
        /// for s in statics:
        ///     API.SysMsg(f"Static Graphic: {s.Graphic}, Z: {s.Z}")
        /// ```
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>List of PyStatic objects at the specified position</returns>
        public List<PyStatic> GetStaticsAt(int x, int y) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var statics = new List<PyStatic>();

            if (World.Map is null) return new List<PyStatic>();

            Game.Map.Chunk chunk = World.Map.GetChunk(x, y, false);

            if (chunk != null)
            {
                GameObject obj = chunk.GetHeadObject(x % 8, y % 8);

                while (obj != null)
                {
                    if (obj is Static staticObj)
                    {
                        statics.Add(new PyStatic(staticObj));
                    }
                    obj = obj.TNext;
                }
            }

            return statics;
        });

        /// <summary>
        /// Gets all static objects within a rectangular area defined by coordinates.
        /// This includes trees, vegetation, buildings, and other non-movable scenery.
        /// Example:
        /// ```py
        /// statics = API.GetStaticsInArea(1000, 1000, 1010, 1010)
        /// API.SysMsg(f"Found {len(statics)} statics in area")
        /// for s in statics:
        ///     if s.IsVegetation:
        ///         API.SysMsg(f"Vegetation Graphic: {s.Graphic} at {s.X}, {s.Y}")
        /// ```
        /// </summary>
        /// <param name="x1">Starting X coordinate</param>
        /// <param name="y1">Starting Y coordinate</param>
        /// <param name="x2">Ending X coordinate</param>
        /// <param name="y2">Ending Y coordinate</param>
        /// <returns>List of PyStatic objects within the specified area</returns>
        public List<PyStatic> GetStaticsInArea(int x1, int y1, int x2, int y2) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var statics = new List<PyStatic>();

            if (World.Map is null) return new List<PyStatic>();

            // Ensure coordinates are in correct order
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Game.Map.Chunk chunk = World.Map.GetChunk(x, y, false);

                    if (chunk != null)
                    {
                        GameObject obj = chunk.GetHeadObject(x % 8, y % 8);

                        while (obj != null)
                        {
                            if (obj is Static staticObj)
                            {
                                statics.Add(new PyStatic(staticObj));
                            }
                            obj = obj.TNext;
                        }
                    }
                }
            }

            return statics;
        });

        /// <summary>
        /// Gets all multi objects at a specific position (x, y coordinates).
        /// This includes server-side house data.
        /// Example:
        /// ```py
        /// multis = API.GetMultisAt(1000, 1000)
        /// for m in multis:
        ///     API.SysMsg(f"Multi Graphic: {m.Graphic}, Z: {m.Z}")
        /// ```
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>List of PyMulti objects at the specified position</returns>
        public List<PyMulti> GetMultisAt(int x, int y) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var multis = new List<PyMulti>();

            if (World.Map is null) return new List<PyMulti>();

            // Check server-side houses for Multi components
            if (World.HouseManager != null)
            {
                foreach (House house in World.HouseManager.Houses)
                {
                    IEnumerable<Multi> houseMultis = house.GetMultiAt(x, y);
                    foreach (Multi houseMulti in houseMultis)
                    {
                        multis.Add(new PyMulti(houseMulti));
                    }
                }
            }

            return multis;
        });

        /// <summary>
        /// Gets all multi objects within a rectangular area defined by coordinates.
        /// This includes server-side house data.
        /// Example:
        /// ```py
        /// multis = API.GetMultisInArea(1000, 1000, 1010, 1010)
        /// API.SysMsg(f"Found {len(multis)} multis in area")
        /// for m in multis:
        ///     API.SysMsg(f"Multi Graphic: {m.Graphic} at {m.X}, {m.Y}")
        /// ```
        /// </summary>
        /// <param name="x1">Starting X coordinate</param>
        /// <param name="y1">Starting Y coordinate</param>
        /// <param name="x2">Ending X coordinate</param>
        /// <param name="y2">Ending Y coordinate</param>
        /// <returns>List of PyMulti objects within the specified area</returns>
        public List<PyMulti> GetMultisInArea(int x1, int y1, int x2, int y2) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var multis = new List<PyMulti>();

            if (World.Map is null) return new List<PyMulti>();

            // Ensure coordinates are in correct order
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            // Check server-side houses for Multi components in the area
            if (World.HouseManager != null)
            {
                foreach (House house in World.HouseManager.Houses)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            IEnumerable<Multi> houseMultis = house.GetMultiAt(x, y);
                            foreach (Multi houseMulti in houseMultis)
                            {
                                multis.Add(new PyMulti(houseMulti));
                            }
                        }
                    }
                }
            }

            return multis;
        });

        #region Friends List

        /// <summary>
        /// Check if a mobile is in the friends list.
        /// Example:
        /// ```py
        /// if API.IsFriend(player.Serial):
        ///     API.SysMsg("This player is your friend!")
        /// ```
        /// </summary>
        /// <param name="serial">Serial number of the mobile to check</param>
        /// <returns>True if the mobile is in the friends list, false otherwise</returns>
        public bool IsFriend(uint serial) => MainThreadQueue.InvokeOnMainThread(() => FriendsListManager.Instance.IsFriend(serial));

        /// <summary>
        /// Add a mobile to the friends list by serial number.
        /// Example:
        /// ```py
        /// mobile = API.FindMobile(0x12345)
        /// if mobile:
        ///     API.AddFriend(mobile.Serial)
        /// ```
        /// </summary>
        /// <param name="serial">Serial number of the mobile to add</param>
        /// <returns>True if the friend was added successfully, false if already exists or invalid</returns>
        public bool AddFriend(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Mobile mobile = World.Mobiles.Get(serial);
            return mobile != null && FriendsListManager.Instance.AddFriend(mobile);
        });

        /// <summary>
        /// Remove a mobile from the friends list by serial number.
        /// Example:
        /// ```py
        /// API.RemoveFriend(0x12345)
        /// ```
        /// </summary>
        /// <param name="serial">Serial number of the mobile to remove</param>
        /// <returns>True if the friend was removed successfully, false if not found</returns>
        public bool RemoveFriend(uint serial) => MainThreadQueue.InvokeOnMainThread(() => FriendsListManager.Instance.RemoveFriend(serial));

        /// <summary>
        /// Get all friends as an array of serials.
        /// Example:
        /// ```py
        /// friends = API.GetAllFriends()
        /// for friend in friends:
        ///     API.FindMobile(friend)
        /// ```
        /// </summary>
        /// <returns>Array of serials representing all friends</returns>
        public PythonList GetAllFriends() => MainThreadQueue.InvokeOnMainThread(() => FriendsListManager.Instance.GetAllFriends());

        #endregion

        #region Gumps

        /// <summary>
        /// Get a blank gump.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// g.Add(API.CreateGumpLabel("Hello World!"))
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="acceptMouseInput">Allow clicking the gump</param>
        /// <param name="canMove">Allow the player to move this gump</param>
        /// <param name="keepOpen">If true, the gump won't be closed if the script stops. Otherwise, it will be closed when the script is stopped. Defaults to false.</param>
        /// <returns>A new, empty gump</returns>
        public PyBaseGump CreateGump(bool acceptMouseInput = true, bool canMove = true, bool keepOpen = false)
        {
            var g = new Gump(World, 0, 0)
            {
                AcceptMouseInput = acceptMouseInput,
                CanMove = canMove,
                WantUpdateSize = true
            };

            PyBaseGump pyGump = new(g);

            if (!keepOpen)
                gumps.Add(g);

            return pyGump;
        }

        /// <summary>
        /// Add a gump to the players screen.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// g.Add(API.CreateGumpLabel("Hello World!"))
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="g">The gump to add</param>
        public void AddGump(object g) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (g is Gump gump)
                UIManager.Add(gump);

            if (g is IPyGump { Gump: not null } pyGump)
                UIManager.Add(pyGump.Gump);
        });

        /// <summary>
        /// Create a checkbox for gumps.
        /// /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// cb = API.CreateGumpCheckbox("Check me?!")
        /// g.Add(cb)
        /// API.AddGump(g)
        ///
        /// API.SysMsg("Checkbox checked: " + str(cb.IsChecked))
        /// ```
        /// </summary>
        /// <param name="text">Optional text label</param>
        /// <param name="hue">Optional hue</param>
        /// <param name="isChecked">Default false, set to true if you want this checkbox checked on creation</param>
        /// <returns>The checkbox</returns>
        public PyCheckbox CreateGumpCheckbox(string text = "", ushort hue = 0, bool isChecked = false) =>
            new PyCheckbox(new Checkbox(0x00D2, 0x00D3, text, color: hue) { CanMove = true, IsChecked = isChecked }
            );

        /// <summary>
        /// Create a label for a gump.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// g.Add(API.CreateGumpLabel("Hello World!"))
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="hue">The hue of the text</param>
        /// <returns></returns>
        public Label CreateGumpLabel(string text, ushort hue = 996) => new Label(text, true, hue)
        {
            CanMove = true
        };

        /// <summary>
        /// Get a transparent color box for gumps.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// cb = API.CreateGumpColorBox(0.5, "#000000")
        /// cb.SetWidth(200)
        /// cb.SetHeight(200)
        /// g.Add(cb)
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="opacity">0.5 = 50%</param>
        /// <param name="color">Html color code like #000000</param>
        /// <returns></returns>
        public AlphaBlendControl CreateGumpColorBox(float opacity = 0.7f, string color = "#000000")
        {
            var bc = new AlphaBlendControl(opacity);
            bc.BaseColor = Utility.GetColorFromHex(color);

            return bc;
        }

        /// <summary>
        /// Create a picture of an item.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ResizableStaticPic CreateGumpItemPic(uint graphic, int width, int height)
        {
            var pic = new ResizableStaticPic(graphic, width, height)
            {
                AcceptMouseInput = false
            };

            return pic;
        }

        /// <summary>
        /// Create a button for gumps.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// button = API.CreateGumpButton("Click Me!")
        /// g.Add(button)
        /// API.AddGump(g)
        ///
        /// while True:
        ///   API.SysMsg("Button currently clicked?: " + str(button.IsClicked))
        ///   API.SysMsg("Button clicked since last check?: " + str(button.HasBeenClicked()))
        ///   API.Pause(0.2)
        /// ```
        /// </summary>
        /// <param name="text"></param>
        /// <param name="hue"></param>
        /// <param name="normal">Graphic when not clicked or hovering</param>
        /// <param name="pressed">Graphic when pressed</param>
        /// <param name="hover">Graphic on hover</param>
        /// <returns></returns>
        public Button CreateGumpButton(string text = "", ushort hue = 996, ushort normal = 0x00EF, ushort pressed = 0x00F0, ushort hover = 0x00EE)
        {
            var b = new Button(0, normal, pressed, hover, caption: text, normalHue: hue, hoverHue: hue);

            return b;
        }

        /// <summary>
        /// Create a simple button, does not use graphics.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// button = API.CreateSimpleButton("Click Me!", 100, 20)
        /// g.Add(button)
        /// API.AddGump(g)
        /// ```
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public NiceButton CreateSimpleButton(string text, int width, int height)
        {
            NiceButton b = new(0, 0, width, height, ButtonAction.Default, text);
            b.AlwaysShowBackground = true;

            return b;
        }

        /// <summary>
        /// Create a radio button for gumps, use group numbers to only allow one item to be checked at a time.
        /// Example:
        /// ```py
        /// g = API.CreateGump()
        /// g.SetRect(100, 100, 200, 200)
        /// rb = API.CreateGumpRadioButton("Click Me!", 1)
        /// g.Add(rb)
        /// API.AddGump(g)
        /// API.SysMsg("Radio button checked?: " + str(rb.IsChecked))
        /// ```
        /// </summary>
        /// <param name="text">Optional text</param>
        /// <param name="group">Group ID</param>
        /// <param name="inactive">Unchecked graphic</param>
        /// <param name="active">Checked graphic</param>
        /// <param name="hue">Text color</param>
        /// <param name="isChecked">Defaults false, set to true if you want this button checked by default.</param>
        /// <returns></returns>
        public RadioButton CreateGumpRadioButton(string text = "", int group = 0, ushort inactive = 0x00D0, ushort active = 0x00D1, ushort hue = 0xFFFF, bool isChecked = false)
        {
            var rb = new RadioButton(group, inactive, active, text, color: hue);
            rb.IsChecked = isChecked;
            return rb;
        }

        /// <summary>
        /// Create a text area control.
        /// Example:
        /// ```py
        /// w = 500
        /// h = 600
        ///
        /// gump = API.CreateGump(True, True)
        /// gump.SetWidth(w)
        /// gump.SetHeight(h)
        /// gump.CenterXInViewPort()
        /// gump.CenterYInViewPort()
        ///
        /// bg = API.CreateGumpColorBox(0.7, "#D4202020")
        /// bg.SetWidth(w)
        /// bg.SetHeight(h)
        ///
        /// gump.Add(bg)
        ///
        /// textbox = API.CreateGumpTextBox("Text example", w, h, True)
        ///
        /// gump.Add(textbox)
        ///
        /// API.AddGump(gump)
        /// ```
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="multiline"></param>
        /// <returns></returns>
        public TTFTextInputField CreateGumpTextBox(string text = "", int width = 200, int height = 30, bool multiline = false) => new TTFTextInputField(width, height, text: text, multiline: multiline, convertHtmlColors: false)
        {
            CanMove = true
        };

        /// <summary>
        /// Create a TTF label with advanced options.
        /// Example:
        /// ```py
        /// gump = API.CreateGump()
        /// gump.SetRect(100, 100, 200, 200)
        ///
        /// ttflabel = API.CreateGumpTTFLabel("Example label", 25, "#F100DD", "alagard")
        /// ttflabel.SetRect(10, 10, 180, 30)
        /// gump.Add(ttflabel)
        ///
        /// API.AddGump(gump) #Add the gump to the players screen
        /// ```
        /// </summary>
        /// <param name="text"></param>
        /// <param name="size">Font size</param>
        /// <param name="color">Hex color: #FFFFFF. Must begin with #.</param>
        /// <param name="font">Must have the font installed in TazUO</param>
        /// <param name="aligned">left/center/right. Must set a max width for this to work.</param>
        /// <param name="maxWidth">Max width before going to the next line</param>
        /// <param name="applyStroke">Uses players stroke settings, this turns it on or off</param>
        /// <returns></returns>
        public TextBox CreateGumpTTFLabel
            (string text, float size, string color = "#FFFFFF", string font = TrueTypeLoader.EMBEDDED_FONT, string aligned = "left", int maxWidth = 0, bool applyStroke = false)
        {
            var opts = TextBox.RTLOptions.Default();

            switch (aligned.ToLower())
            {
                case "left": opts.Align = TextHorizontalAlignment.Left; break;

                case "middle":
                case "center": opts.Align = TextHorizontalAlignment.Center; break;

                case "right": opts.Align = TextHorizontalAlignment.Right; break;
            }

            if (applyStroke)
                opts.StrokeEffect = true;

            if (maxWidth > 0)
                opts.Width = maxWidth;

            return TextBox.GetOne(text, font, size, Utility.GetColorFromHex(color), opts);
        }

        /// <summary>
        /// Create a progress bar. Can be updated as needed with `bar.SetProgress(current, max)`.
        /// Example:
        /// ```py
        /// gump = API.CreateGump()
        /// gump.SetRect(100, 100, 400, 200)
        ///
        /// pb = API.CreateGumpSimpleProgressBar(400, 200)
        /// gump.Add(pb)
        ///
        /// API.AddGump(gump)
        ///
        /// cur = 0
        /// max = 100
        ///
        /// while True:
        ///   pb.SetProgress(cur, max)
        ///   if cur >= max:
        ///   break
        ///   cur += 1
        ///   API.Pause(0.5)
        /// ```
        /// </summary>
        /// <param name="width">The width of the bar</param>
        /// <param name="height">The height of the bar</param>
        /// <param name="backgroundColor">The background color(Hex color like #616161)</param>
        /// <param name="foregroundColor">The foreground color(Hex color like #212121)</param>
        /// <param name="value">The current value, for example 70</param>
        /// <param name="max">The max value(or what would be 100%), for example 100</param>
        /// <returns></returns>
        public SimpleProgressBar CreateGumpSimpleProgressBar
            (int width, int height, string backgroundColor = "#616161", string foregroundColor = "#212121", int value = 100, int max = 100)
        {
            SimpleProgressBar bar = new(backgroundColor, foregroundColor, width, height);
            bar.SetProgress(value, max);

            return bar;
        }

        /// <summary>
        /// Create a scrolling area, add and position controls to it directly.
        /// Example:
        /// ```py
        /// sa = API.CreateGumpScrollArea(0, 60, 200, 140)
        /// gump.Add(sa)
        ///
        /// for i in range(10):
        ///     label = API.CreateGumpTTFLabel(f"Label {i + 1}", 20, "#FFFFFF", "alagard")
        ///     label.SetRect(5, i * 20, 180, 20)
        ///     sa.Add(label)
        /// ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public PyScrollArea CreateGumpScrollArea(int x, int y, int width, int height) => new PyScrollArea(new ScrollArea(x, y, width, height, true));

        /// <summary>
        /// Create a gump pic(Use this for gump art, not item art)
        /// Example:
        /// ```py
        /// gumpPic = API.CreateGumpPic(0xafb)
        /// gump.Add(gumpPic)
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public GumpPic CreateGumpPic(ushort graphic, int x = 0, int y = 0, ushort hue = 0) => new GumpPic(x, y, graphic, hue);

        /// <summary>
        /// Creates a dropdown control (combobox) with the specified width and items.
        /// </summary>
        /// <param name="width">The width of the dropdown control</param>
        /// <param name="items">Array of strings to display as dropdown options</param>
        /// <param name="selectedIndex">The initially selected item index (default: 0)</param>
        /// <returns>A PyControlDropDown wrapper containing the combobox control</returns>
        public PyControlDropDown CreateDropDown(int width, string[] items, int selectedIndex = 0) => new PyControlDropDown(new Combobox(0, 0, width, items, selectedIndex));

        /// <summary>
        /// Creates a modern nine-slice gump using ModernUIConstants for consistent styling.
        /// The gump uses the standard modern UI panel texture and border size internally.
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Initial width</param>
        /// <param name="height">Initial height</param>
        /// <param name="resizable">Whether the gump can be resized by dragging corners (default: true)</param>
        /// <param name="minWidth">Minimum width (default: 50)</param>
        /// <param name="minHeight">Minimum height (default: 50)</param>
        /// <param name="onResized">Optional callback function called when the gump is resized</param>
        /// <returns>A PyNineSliceGump wrapper containing the nine-slice gump control</returns>
        public PyNineSliceGump CreateModernGump(int x, int y, int width, int height, bool resizable = true, int minWidth = 50, int minHeight = 50, object onResized = null) => new PyNineSliceGump(this, x, y, width, height, resizable, minWidth, minHeight, onResized);

        /// <summary>
        /// Add an onClick callback to a control.
        /// Example:
        /// ```py
        /// def myfunc:
        ///   API.SysMsg("Something clicked!")
        /// bg = API.CreateGumpColorBox(0.7, "#D4202020")
        /// API.AddControlOnClick(bg, myfunc)
        /// while True:
        ///   API.ProcessCallbacks()
        /// ```
        /// </summary>
        /// <param name="control">The control listening for clicks</param>
        /// <param name="onClick">The callback function</param>
        /// <param name="leftOnly">Only accept left mouse clicks?</param>
        /// <returns>Returns the control so methods can be chained.</returns>
        public object AddControlOnClick(object control, object onClick, bool leftOnly = true)
        {
            if (control == null || onClick == null || !engine.Operations.IsCallable(onClick))
                return control;

            Control wControl = null;

            if(control is Control)
                wControl = (Control)control;
            else if (control is PyBaseControl pbc && pbc.Control != null)
                wControl = pbc.Control;

            if (wControl == null)
                return control;

            wControl.AcceptMouseInput = true;

            wControl.MouseUp += (s, e) =>
            {
                if (leftOnly && e.Button != MouseButtonType.Left)
                    return;

                this?.ScheduleCallback
                (() =>
                    {
                        try
                        {
                            engine.Operations.Invoke(onClick);
                        }
                        catch (Exception ex)
                        {
                            GameActions.Print($"Script callback error: {ex}", 32);
                        }
                    }
                );
            };

            return control;
        }

        /// <summary>
        /// Add onDispose(Closed) callback to a control.
        /// Example:
        /// ```py
        /// def onClose():
        ///     API.Stop()
        ///
        /// gump = API.CreateGump()
        /// gump.SetRect(100, 100, 200, 200)
        ///
        /// bg = API.CreateGumpColorBox(opacity=0.7, color="#000000")
        /// gump.Add(bg.SetRect(0, 0, 200, 200))
        ///
        /// API.AddControlOnDisposed(gump, onClose)
        /// ```
        /// </summary>
        /// <param name="control"></param>
        /// <param name="onDispose"></param>
        /// <returns></returns>
        public PyBaseControl AddControlOnDisposed(PyBaseControl control, object onDispose)
        {
            if (control == null || onDispose == null || control.Control == null || !engine.Operations.IsCallable(onDispose))
                return control;

            control.Control.Disposed += (s, e) =>
            {
                this?.ScheduleCallback
                (() =>
                    {
                        try
                        {
                            engine.Operations.Invoke(onDispose);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                );
            };

            return control;
        }

        #endregion

        /// <summary>
        /// Get a skill from the player. See the Skill class for what properties are available: https://github.com/PlayTazUO/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs
        /// Example:
        /// ```py
        /// skill = API.GetSkill("Hiding")
        /// if skill:
        ///   API.SysMsg("Skill: " + skill.Name)
        ///   API.SysMsg("Skill Value: " + str(skill.Value))
        ///   API.SysMsg("Skill Cap: " + str(skill.Cap))
        ///   API.SysMsg("Skill Lock: " + str(skill.Lock))
        ///   ```
        /// </summary>
        /// <param name="skill">Skill name, case-sensitive</param>
        /// <returns></returns>
        public Skill GetSkill(string skill) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(skill))
                    return null;

                foreach (Skill s in World.Player.Skills)
                {
                    if (s.Name.Contains(skill))
                        return s;
                }

                return null;
            }
        );

        /// <summary>
        /// Show a radius around the player.
        /// Example:
        /// ```py
        /// API.DisplayRange(7, 32)
        /// ```
        /// </summary>
        /// <param name="distance">Distance from the player</param>
        /// <param name="hue">The color to change the tiles at that distance</param>
        public void DisplayRange(ushort distance, ushort hue = 22) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (ProfileManager.CurrentProfile == null) return;

                if (distance == 0)
                {
                    ProfileManager.CurrentProfile.DisplayRadius = false;

                    return;
                }

                ProfileManager.CurrentProfile.DisplayRadius = true;
                ProfileManager.CurrentProfile.DisplayRadiusDistance = distance;
                ProfileManager.CurrentProfile.DisplayRadiusHue = hue;
            }
        );

        /// <summary>
        /// Toggle another script on or off.
        /// Example:
        /// ```py
        /// API.ToggleScript("MyScript.py")
        /// ```
        /// </summary>
        /// <param name="scriptName">Full name including extension. Can be .py or .lscript.</param>
        /// <exception cref="Exception"></exception>
        public void ToggleScript(string scriptName) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    throw new Exception("[ToggleScript] Script name can't be empty.");

                foreach (ScriptFile script in LegionScripting.LoadedScripts)
                {
                    if (script.FileName == scriptName)
                    {
                        if (script.IsPlaying)
                            LegionScripting.StopScript(script);
                        else
                            LegionScripting.PlayScript(script);

                        return;
                    }
                }
            }
        );

        /// <summary>
        /// Play a legion script.
        /// </summary>
        /// <param name="scriptName">This is the file name including extension.</param>
        public void PlayScript(string scriptName) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    GameActions.Print(World, "[PlayScript] Script name can't be empty.");

                foreach (ScriptFile script in LegionScripting.LoadedScripts)
                {
                    if (script.FileName == scriptName)
                    {
                        LegionScripting.PlayScript(script);
                        return;
                    }
                }
            }
        );

        /// <summary>
        /// Stop a legion script.
        /// </summary>
        /// <param name="scriptName">This is the file name including extension.</param>
        public void StopScript(string scriptName) => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    GameActions.Print(World, "[StopScript] Script name can't be empty.");

                foreach (ScriptFile script in LegionScripting.LoadedScripts)
                {
                    if (script.FileName == scriptName)
                    {
                        LegionScripting.StopScript(script);
                        return;
                    }
                }
            }
        );

        /// <summary>
        /// Add a marker to the current World Map (If one is open)
        /// Example:
        /// ```py
        /// API.AddMapMarker("Death")
        /// ```
        /// </summary>
        /// <param name="name"></param>
        /// <param name="x">Defaults to current player X.</param>
        /// <param name="y">Defaults to current player Y.</param>
        /// <param name="map">Defaults to current map.</param>
        /// <param name="color">red/green/blue/purple/black/yellow/white. Default purple.</param>
        public void AddMapMarker(string name, int x = int.MaxValue, int y = int.MaxValue, int map = int.MaxValue, string color = "purple") => MainThreadQueue.InvokeOnMainThread
        (() =>
            {
                WorldMapGump wmap = UIManager.GetGump<WorldMapGump>();

                if (wmap == null || string.IsNullOrEmpty(name))
                    return;

                if (map == int.MaxValue)
                    map = World.MapIndex;

                if (x == int.MaxValue)
                    x = World.Player.X;

                if (y == int.MaxValue)
                    y = World.Player.Y;

                wmap.AddUserMarker(name, x, y, map, color);
            }
        );

        /// <summary>
        /// Remove a marker from the world map.
        /// Example:
        /// ```py
        /// API.RemoveMapMarker("Death")
        /// ```
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapMarker(string name) => MainThreadQueue.InvokeOnMainThread
        (() =>
        {
            WorldMapGump wmap = UIManager.GetGump<WorldMapGump>();

            if (wmap == null || string.IsNullOrEmpty(name))
                return;

            wmap.RemoveUserMarker(name);
        });

        /// <summary>
        /// Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.
        /// Example:
        /// ```py
        /// if API.IsProcessingMoveQueue():
        ///   API.Pause(0.5)
        /// ```
        /// </summary>
        /// <returns></returns>
        public bool IsProcessingMoveQueue() => MainThreadQueue.InvokeOnMainThread(() => !MoveItemQueue.Instance.IsEmpty);

        /// <summary>
        /// Check if the use item queue is being processed. You can use this to prevent actions if the queue is being processed.
        /// Example:
        /// ```py
        /// if API.IsProcessingUseItemQueue():
        ///   API.Pause(0.5)
        /// ```
        /// </summary>
        /// <returns></returns>
        public bool IsProcessingUseItemQueue() => MainThreadQueue.InvokeOnMainThread(() => !UseItemQueue.Instance.IsEmpty);

        /// <summary>
        /// Check if the global cooldown is currently active. This applies to actions like moving or using items,
        /// and prevents new actions from executing until the cooldown has expired.
        ///
        /// Example:
        /// ```py
        /// if API.IsGlobalCooldownActive():
        ///     API.Pause(0.5)
        /// ```
        /// </summary>
        /// <returns>True if the global cooldown is active; otherwise, false.</returns>
        public bool IsGlobalCooldownActive() => MainThreadQueue.InvokeOnMainThread(() => GlobalActionCooldown.IsOnCooldown);

        /// <summary>
        /// Save a variable that persists between sessions and scripts.
        /// Example:
        /// ```py
        /// API.SavePersistentVar("TotalKills", "5", API.PersistentVar.Char)
        /// ```
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="scope"></param>
        public void SavePersistentVar(string name, string value, PersistentVar scope)
        {
            if (string.IsNullOrEmpty(name))
            {
                GameActions.Print(World, "Var's must have a name.", 32);
                return;
            }

            PersistentVars.SaveVar(scope, name, value);
        }

        /// <summary>
        /// Delete/remove a persistent variable.
        /// Example:
        /// ```py
        /// API.RemovePersistentVar("TotalKills", API.PersistentVar.Char)
        /// ```
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        public void RemovePersistentVar(string name, PersistentVar scope)
        {
            if (string.IsNullOrEmpty(name))
            {
                GameActions.Print(World, "Var's must have a name.", 32);
                return;
            }

            PersistentVars.DeleteVar(scope, name);
        }

        /// <summary>
        /// Get a persistent variable.
        /// Example:
        /// ```py
        /// API.GetPersistentVar("TotalKills", "0", API.PersistentVar.Char)
        /// ```
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue">The value returned if no value was saved</param>
        /// <param name="scope"></param>
        public string GetPersistentVar(string name, string defaultValue, PersistentVar scope)
        {
            if (string.IsNullOrEmpty(name))
            {
                GameActions.Print(World, "Var's must have a name.", 32);
                return defaultValue;
            }

            return PersistentVars.GetVar(scope, name, defaultValue);
        }

        /// <summary>
        /// Mark a tile with a specific hue.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="hue"></param>
        /// <param name="map">Defaults to current map</param>
        public void MarkTile(int x, int y, ushort hue, int map = -1) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (map < 0)
                map = World.Map.Index;

            TileMarkerManager.Instance.AddTile(x, y, map, hue);
        });

        /// <summary>
        /// Remove a marked tile. See MarkTile for more info.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="map"></param>
        public void RemoveMarkedTile(int x, int y, int map = -1) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (map < 0)
                map = World.Map.Index;

            TileMarkerManager.Instance.RemoveTile(x, y, map);
        });

        /// <summary>
        /// Create a tracking arrow pointing towards a location.
        /// Set x or y to a negative value to close existing tracker arrow.
        /// ```py
        /// API.TrackingArrow(400, 400)
        /// ```
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="identifier">An identified number if you want multiple arrows.</param>
        public void TrackingArrow(int x, int y, uint identifier = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            UIManager.GetGump<QuestArrowGump>(identifier)?.Dispose();

            if (x > 0 && y > 0)
            {
                var arrow = new QuestArrowGump(World, identifier, x, y) { CanCloseWithRightClick = true };
                UIManager.Add(arrow);
            }
        });

        #endregion
    }
}
