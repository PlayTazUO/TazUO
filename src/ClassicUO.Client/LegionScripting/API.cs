using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
using FontStashSharp.RichText;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Xna.Framework;
using Button = ClassicUO.Game.UI.Controls.Button;
using Control = ClassicUO.Game.UI.Controls.Control;
using Label = ClassicUO.Game.UI.Controls.Label;
using RadioButton = ClassicUO.Game.UI.Controls.RadioButton;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Python scripting access point
    /// </summary>
    public class API
    {
        public API(ScriptEngine engine)
        {
            this.engine = engine;
        }

        private ScriptEngine engine;

        private ConcurrentBag<Gump> gumps = new();

        #region Python C# Queue

        public static readonly ConcurrentQueue<Action> QueuedPythonActions = new();

        private static T InvokeOnMainThread<T>(Func<T> func)
        {
            var resultEvent = new ManualResetEvent(false);
            T result = default;

            void action()
            {
                result = func();
                resultEvent.Set();
            }

            QueuedPythonActions.Enqueue(action);
            resultEvent.WaitOne(); // Wait for the main thread to complete the operation

            return result;
        }

        private static void InvokeOnMainThread(Action action)
        {
            var resultEvent = new ManualResetEvent(false);

            void wrappedAction()
            {
                action();
                resultEvent.Set();
            }

            QueuedPythonActions.Enqueue(wrappedAction);
            resultEvent.WaitOne();
        }

        #endregion

        #region Python Callback Queue

        private readonly Queue<Action> scheduledCallbacks = new();
        private static readonly ConcurrentDictionary<string, object> sharedVars = new();

        private void ScheduleCallback(Action action)
        {
            lock (scheduledCallbacks)
            {
                scheduledCallbacks.Enqueue(action);

                while (scheduledCallbacks.Count > 100)
                {
                    scheduledCallbacks.Dequeue(); //Limit callback counts
                    GameActions.Print("Python Scripting Error: Too many callbacks registered!");
                }
            }
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
        private ConcurrentQueue<JournalEntry> journalEntries = new();
        private Item backpack;
        private PlayerMobile player;

        public ConcurrentQueue<JournalEntry> JournalEntries
        {
            get { return journalEntries; }
        }

        #region Properties

        /// <summary>
        /// Get the player's backpack serial
        /// </summary>
        public uint Backpack
        {
            get
            {
                if (backpack == null)
                    backpack = InvokeOnMainThread(() => World.Player.FindItemByLayer(Game.Data.Layer.Backpack));

                return backpack;
            }
        }


        /// <summary>
        /// Returns the player character object
        /// </summary>
        public PlayerMobile Player
        {
            get
            {
                if (player == null)
                    player = InvokeOnMainThread(() => World.Player);

                return player;
            }
        }

        /// <summary>
        /// Return the player's bank container serial if open, otherwise 0
        /// </summary>
        public uint Bank {
            get
            {
                var i = InvokeOnMainThread(()=>World.Player.FindItemByLayer(Layer.Bank));
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
        public uint LastTargetSerial => InvokeOnMainThread(() => TargetManager.LastTargetInfo.Serial);

        /// <summary>
        /// The last target's position
        /// </summary>
        public Vector3 LastTargetPos => InvokeOnMainThread(() => TargetManager.LastTargetInfo.Position);

        /// <summary>
        /// The graphic of the last targeting object
        /// </summary>
        public ushort LastTargetGraphic => InvokeOnMainThread(() => TargetManager.LastTargetInfo.Graphic);

        /// <summary>
        /// The serial of the last item or mobile from the various findtype/mobile methods
        /// </summary>
        public uint Found { get; set; }

        /// <summary>
        /// Access useful player settings.
        /// </summary>
        public static PyProfile PyProfile = new();

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
        /// Set a variable that is shared between scripts.
        /// Example:
        /// ```py
        /// API.SetSharedVar("myVar", 10)
        /// ```
        /// </summary>
        /// <param name="name">Name of the var</param>
        /// <param name="value">Value, can be a number, text, or *most* other objects too.</param>
        public void SetSharedVar(string name, object value)
        {
            sharedVars[name] = value;
        }

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
            if (sharedVars.TryGetValue(name, out var v))
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
        public void RemoveSharedVar(string name)
        {
            sharedVars.TryRemove(name, out _);
        }

        /// <summary>
        /// Clear all shared vars.
        /// Example:
        /// ```py
        /// API.ClearSharedVars()
        /// ```
        /// </summary>
        public void ClearSharedVars()
        {
            sharedVars.Clear();
        }

        /// <summary>
        /// Close all gumps created by the API unless marked to remain open.
        /// </summary>
        public void CloseGumps()
        {
            int c = 0;
            while (gumps.TryTake(out var g))
            {
                if (g is { IsDisposed: false })
                    QueuedPythonActions.Enqueue(() => g?.Dispose());

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
        public void Attack(uint serial) => InvokeOnMainThread(() => GameActions.Attack(serial));

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
        public bool BandageSelf() => InvokeOnMainThread(GameActions.BandageSelf);

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
        /// <returns>The item serial that was in your hand</returns>
        public uint ClearLeftHand() => InvokeOnMainThread<uint>
        (() =>
            {
                Item i = World.Player.FindItemByLayer(Layer.OneHanded);

                if (i != null)
                {
                    var bp = World.Player.FindItemByLayer(Layer.Backpack);
                    MoveItemQueue.Instance.Enqueue(i, bp);
                    Found = i.Serial;
                    return i;
                }

                Found = 0;
                return 0;
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
        /// <returns>The item serial that was in your hand</returns>
        public uint ClearRightHand() => InvokeOnMainThread<uint>
        (() =>
            {
                Item i = World.Player.FindItemByLayer(Layer.TwoHanded);

                if (i != null)
                {
                    var bp = World.Player.FindItemByLayer(Layer.Backpack);
                    MoveItemQueue.Instance.Enqueue(i, bp);
                    Found = i.Serial;
                    return i;
                }

                Found = 0;
                return 0;
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
        public void ClickObject(uint serial) => InvokeOnMainThread(() => GameActions.SingleClick(serial));

        /// <summary>
        /// Attempt to use(double click) an object.
        /// Example:
        /// ```py
        /// API.UseObject(API.Backpack)
        /// ```
        /// </summary>
        /// <param name="serial">The serial</param>
        /// <param name="skipQueue">Defaults true, set to false to use a double click queue</param>
        public void UseObject(uint serial, bool skipQueue = true) => InvokeOnMainThread
        (() =>
            {
                if (skipQueue)
                    GameActions.DoubleClick(serial);
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
        public int Contents(uint serial) => InvokeOnMainThread<int>
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
        public void ContextMenu(uint serial, ushort entry) => InvokeOnMainThread
        (() =>
            {
                PopupMenuGump.CloseNext = serial;
                NetClient.Socket.Send_RequestPopupMenu(serial);
                NetClient.Socket.Send_PopupMenuSelection(serial, entry);
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
        public void EquipItem(uint serial) => InvokeOnMainThread
        (() =>
            {
                GameActions.PickUp(serial, 0, 0, 1);
                GameActions.Equip();
            }
        );

        /// <summary>
        /// Clear the move item que of all items.
        /// </summary>
        public void ClearMoveQueue() => InvokeOnMainThread(() => Client.Game.GetScene<GameScene>()?.MoveItemQueue.Clear());

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
        ///             API.QueMoveItem(item, barrel)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="destination"></param>
        /// <param name="amt">Amount to move</param>
        /// <param name="x">X coordinate inside a container</param>
        /// <param name="y">Y coordinate inside a container</param>
        public void QueMoveItem(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF) => InvokeOnMainThread
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
        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => InvokeOnMainThread
        (() =>
            {
                GameActions.PickUp(serial, 0, 0, amt);
                GameActions.DropItem(serial, x, y, 0, destination);
            }
        );

        /// <summary>
        /// Move an item to the ground near you.
        /// Example:
        /// ```py
        /// items = API.ItemsInContainer(API.Backpack)
        /// for item in items:
        ///   API.QueMoveItemOffset(item, 0, 1, 0, 0)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="amt">0 to grab entire stack</param>
        /// <param name="x">Offset from your location</param>
        /// <param name="y">Offset from your location</param>
        /// <param name="z">Offset from your location. Leave blank in most cases</param>
        /// <param name="OSI">True if you are playing OSI</param>
        public void QueMoveItemOffset(uint serial, ushort amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) => InvokeOnMainThread
        (() =>
            {
                World.Map.GetMapZ(World.Player.X + x, World.Player.Y + y, out sbyte gz, out sbyte gz2);

                bool useCalculatedZ = false;

                if (gz > z)
                {
                    z = gz;
                    useCalculatedZ = true;
                }
                if(gz2 > z)
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
        public void MoveItemOffset(uint serial, int amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) => InvokeOnMainThread
        (() =>
            {
                World.Map.GetMapZ(World.Player.X + x, World.Player.Y + y, out sbyte gz, out sbyte gz2);

                bool useCalculatedZ = false;

                if (gz > z)
                {
                    z = gz;
                    useCalculatedZ = true;
                }
                if(gz2 > z)
                {
                    z = gz2;
                    useCalculatedZ = true;
                }

                if (!useCalculatedZ)
                    z = World.Player.Z + z;

                GameActions.PickUp(serial, 0, 0, amt);
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
        public void UseSkill(string skillName) => InvokeOnMainThread
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
        public void CastSpell(string spellName) => InvokeOnMainThread(() => { GameActions.CastSpellByName(spellName); });

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
        public bool BuffExists(string buffName) => InvokeOnMainThread
        (() =>
            {
                foreach (BuffIcon buff in World.Player.BuffIcons.Values)
                {
                    if (buff.Title.Contains(buffName))
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
        public Buff[] ActiveBuffs() => InvokeOnMainThread(() =>
        {
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
        public void SysMsg(string message, ushort hue = 946) => InvokeOnMainThread(() => GameActions.Print(message, hue));

        /// <summary>
        /// Say a message outloud.
        /// Example:
        /// ```py
        /// API.Say("Hello friend!")
        /// ```
        /// </summary>
        /// <param name="message">The message to say</param>
        public void Msg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.SpeechHue); });

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
        public void HeadMsg(string message, uint serial, ushort hue = ushort.MaxValue) => InvokeOnMainThread
        (() =>
            {
                Entity e = World.Get(serial);

                if (e == null)
                    return;

                if (hue == ushort.MaxValue)
                    hue = ProfileManager.CurrentProfile.SpeechHue;

                MessageManager.HandleMessage(e, message, "", hue, MessageType.Label, 3, TextType.OBJECT);
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
        public void PartyMsg(string message) => InvokeOnMainThread(() => { GameActions.SayParty(message); });

        /// <summary>
        /// Send your guild a message.
        /// Example:
        /// ```py
        /// API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void GuildMsg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild); });

        /// <summary>
        /// Send a message to your alliance.
        /// Example:
        /// ```py
        /// API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void AllyMsg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance); });

        /// <summary>
        /// Whisper a message.
        /// Example:
        /// ```py
        /// API.WhisperMsg("Psst, bet you didn't see me here..")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void WhisperMsg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper); });

        /// <summary>
        /// Yell a message.
        /// Example:
        /// ```py
        /// API.YellMsg("Vendor restocked, get your fresh feathers!")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void YellMsg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.YellHue, MessageType.Yell); });

        /// <summary>
        /// Emote a message.
        /// Example:
        /// ```py
        /// API.EmoteMsg("laughing")
        /// ```
        /// </summary>
        /// <param name="message"></param>
        public void EmoteMsg(string message) => InvokeOnMainThread(() => { GameActions.Say(message, ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote); });

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
        public Item FindItem(uint serial) => InvokeOnMainThread(() =>
        {
            Item i = World.Items.Get(serial);

            Found = i != null ? i.Serial : 0;

            return i;
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
        public Item FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread
            (() =>
                {
                    List<Item> result = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range);

                    foreach (Item i in result)
                    {
                        if (i.Amount >= minamount && !ignoreList.Contains(i))
                        {
                            Found = i.Serial;
                            return i;
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
        public Item[] FindTypeAll(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            InvokeOnMainThread
                (() => Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range).Where(i => !OnIgnoreList(i) && i.Amount >= minamount).ToArray());

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
        /// <param name="layer">The layer to check, see https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs</param>
        /// <param name="serial">Optional, if not set it will check yourself, otherwise it will check the mobile requested</param>
        /// <returns>The item if it exists</returns>
        public Item FindLayer(string layer, uint serial = uint.MaxValue) => InvokeOnMainThread
        (() =>
            {
                Found = 0;
                Mobile m = serial == uint.MaxValue ? World.Player : World.Mobiles.Get(serial);

                if (m != null)
                {
                    Layer matchedLayer = Utility.GetItemLayer(layer.ToLower());
                    Item item = m.FindItemByLayer(matchedLayer);

                    if (item != null)
                        Found = item.Serial;

                    return item;
                }

                return null;
            }
        );

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
        public Item[] ItemsInContainer(uint container, bool recursive = false) => InvokeOnMainThread(() =>
        {
            if (!recursive)
                return Utility.FindItems(parentContainer: container).ToArray();

            List<Item> results = new();
            Stack<uint> containers = new();
            containers.Push(container);

            while (containers.Count > 0)
            {
                uint current = containers.Pop();

                foreach (var item in Utility.FindItems(parentContainer: current))
                {
                    results.Add(item);
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
        public void UseType(uint graphic, ushort hue = ushort.MaxValue, uint container = uint.MaxValue, bool skipQueue = true) => InvokeOnMainThread
        (() =>
            {
                var result = Utility.FindItems(graphic, hue: hue, parentContainer: container);

                foreach (Item i in result)
                {
                    if (!ignoreList.Contains(i))
                    {
                        if (skipQueue)
                            GameActions.DoubleClick(i);
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
        public void CreateCooldownBar(double seconds, string text, ushort hue) => InvokeOnMainThread
            (() => { Game.Managers.CoolDownBarManager.AddCoolDownBar(TimeSpan.FromSeconds(seconds), text, hue, false); });

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
            var pathFindStatus = InvokeOnMainThread
            (() =>
                {
                    if (z == int.MinValue)
                        z = World.Map.GetTileZ(x, y);

                    return Pathfinder.WalkTo(x, y, z, distance);
                }
            );

            if (!wait)
                return pathFindStatus;

            if(timeout > 30)
                timeout = 30;

            var expire = DateTime.Now.AddSeconds(timeout);

            while (InvokeOnMainThread(()=>Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire)
                {
                    InvokeOnMainThread(Pathfinder.StopAutoWalk);
                    return false;
                }
            }

            InvokeOnMainThread(Pathfinder.StopAutoWalk);

            return InvokeOnMainThread(()=>World.Player.DistanceFrom(new Vector2(x, y)) <= distance);
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
            var pathFindStatus = InvokeOnMainThread
            (() =>
                {
                    var mob = World.Get(entity);
                    if (mob != null)
                    {
                        x = mob.X;
                        y = mob.Y;
                        z = mob.Z;
                        return Pathfinder.WalkTo(x, y, z, distance);
                    }

                    return false;
                }
            );

            if(!wait || (x == 0 && y == 0))
                return pathFindStatus;

            if(timeout > 30)
                timeout = 30;

            var expire = DateTime.Now.AddSeconds(timeout);

            while (InvokeOnMainThread(()=>Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire)
                {
                    InvokeOnMainThread(Pathfinder.StopAutoWalk);
                    return false;
                }
            }

            InvokeOnMainThread(Pathfinder.StopAutoWalk);

            return InvokeOnMainThread(()=>World.Player.DistanceFrom(new Vector2(x, y)) <= distance);
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
        public bool Pathfinding() => InvokeOnMainThread(() => Pathfinder.AutoWalking);

        /// <summary>
        /// Cancel pathfinding.
        /// Example:
        /// ```py
        /// if API.Pathfinding():
        ///   API.CancelPathfinding()
        /// ```
        /// </summary>
        public void CancelPathfinding() => InvokeOnMainThread(Pathfinder.StopAutoWalk);

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
        public PythonList GetPath(int x, int y, int z = int.MinValue, int distance = 1) => InvokeOnMainThread(() =>
        {
            if (z == int.MinValue)
                z = World.Map.GetTileZ(x, y);

            var path = Pathfinder.GetPathTo(x, y, z, distance);
            if (path is null)
            {
                return null;
            }

            var pythonList = new PythonList();
            foreach (var p in path)
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
        public void AutoFollow(uint mobile) => InvokeOnMainThread
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
        public void CancelAutoFollow() => InvokeOnMainThread(() => ProfileManager.CurrentProfile.FollowingMode = false);

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
            InvokeOnMainThread(() => World.Player.Walk(d, true));
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
            InvokeOnMainThread(() => World.Player.Walk(d, false));
        }

        /// <summary>
        /// Turn your character a specific direction.
        /// Example:
        /// ```py
        /// API.Turn("north")
        /// ```
        /// </summary>
        /// <param name="direction">north, northeast, etc</param>
        public void Turn(string direction) => InvokeOnMainThread
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
        public void Rename(uint serial, string name) => InvokeOnMainThread(() => { GameActions.Rename(serial, name); });

        /// <summary>
        /// Attempt to dismount if mounted.
        /// Example:
        /// ```py
        /// API.Dismount()
        /// ```
        /// </summary>
        public void Dismount() => InvokeOnMainThread
        (() =>
            {
                if (World.Player.FindItemByLayer(Layer.Mount) != null)
                    GameActions.DoubleClick(World.Player);

            }
        );

        /// <summary>
        /// Attempt to mount(double click)
        /// Example:
        /// ```py
        /// API.Mount(0x12345678)
        /// ```
        /// </summary>
        /// <param name="serial"></param>
        public void Mount(uint serial) => InvokeOnMainThread(() => { GameActions.DoubleClick(serial); });

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
            var expire = DateTime.UtcNow.AddSeconds(timeout);


            TargetType targetT = TargetType.Neutral;

            switch (targetType.ToLower())
            {
                case "harmful" or "harm": targetT = TargetType.Harmful; break;
                case "beneficial" or "ben": targetT = TargetType.Beneficial; break;
            }

            while (!InvokeOnMainThread(() => { return TargetManager.IsTargeting && (TargetManager.TargetingType == targetT || targetType.ToLower() == "any"); }))
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
        public void Target(uint serial) => InvokeOnMainThread(() => TargetManager.Target(serial));

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
        public void Target(ushort x, ushort y, short z, ushort graphic = ushort.MaxValue) => InvokeOnMainThread
        (() =>
            {
                if (graphic == ushort.MaxValue)
                {
                    TargetManager.Target(0, x, y, z);
                }
                else
                {
                    TargetManager.Target(graphic, x, y, z);
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
            var expire = DateTime.Now.AddSeconds(timeout);
            InvokeOnMainThread(() => TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral));

            while (DateTime.Now < expire)
                if (!InvokeOnMainThread(() => TargetManager.IsTargeting))
                    return TargetManager.LastTargetInfo.Serial;

            InvokeOnMainThread(() => TargetManager.Reset());

            return 0;
        }

        /// <summary>
        /// Target yourself.
        /// Example:
        /// ```py
        /// API.TargetSelf()
        /// ```
        /// </summary>
        public void TargetSelf() => InvokeOnMainThread(() => TargetManager.Target(World.Player.Serial));

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
        public void TargetLandRel(int xOffset, int yOffset) => InvokeOnMainThread
        (() =>
            {
                if (!TargetManager.IsTargeting)
                    return;

                ushort x = (ushort)(World.Player.X + xOffset);
                ushort y = (ushort)(World.Player.Y + yOffset);

                World.Map.GetMapZ(x, y, out sbyte gZ, out sbyte sZ);
                TargetManager.Target(0, x, y, gZ);
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
        public void TargetTileRel(int xOffset, int yOffset, ushort graphic = ushort.MaxValue) => InvokeOnMainThread
        (() =>
            {
                if (!TargetManager.IsTargeting)
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

                TargetManager.Target(graphic, x, y, z);
            }
        );

        /// <summary>
        /// Cancel targeting.
        /// Example:
        /// ```py
        /// if API.WaitForTarget():
        ///   API.CancelTarget()
        ///   API.SysMsg("Targeting cancelled, april fools made you target something!")
        /// ```
        /// </summary>
        public void CancelTarget() => InvokeOnMainThread(TargetManager.CancelTarget);

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
        public bool HasTarget(string targetType = "any") => InvokeOnMainThread
        (() =>
            {
                TargetType targetT = TargetType.Neutral;

                switch (targetType.ToLower())
                {
                    case "harmful" or "harm": targetT = TargetType.Harmful; break;
                    case "beneficial" or "ben": targetT = TargetType.Beneficial; break;
                }

                return TargetManager.IsTargeting && (TargetManager.TargetingType == targetT || targetType.ToLower() == "any");
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
        public int GetMap() => InvokeOnMainThread(() => World.MapIndex);

        /// <summary>
        /// Set a skills lock status.
        /// Example:
        /// ```py
        /// API.SetSkillLock("Hiding", "locked")
        /// ```
        /// </summary>
        /// <param name="skill">The skill name, can be partia;</param>
        /// <param name="up_down_locked">up/down/locked</param>
        public void SetSkillLock(string skill, string up_down_locked) => InvokeOnMainThread
        (() =>
            {
                skill = skill.ToLower();
                Lock status = Lock.Up;

                switch (up_down_locked)
                {
                    case "down": status = Lock.Down; break;
                    case "locked": status = Lock.Locked; break;
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
        public void SetStatLock(string stat, string up_down_locked) => InvokeOnMainThread
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
        public void Logout() => InvokeOnMainThread(() => GameActions.Logout());

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
                var expire = DateTime.UtcNow.AddSeconds(timeout);

                while (!InvokeOnMainThread(() => World.OPL.Contains(serial)) && DateTime.UtcNow < expire)
                {
                    Thread.Sleep(100);
                }
            }

            return InvokeOnMainThread
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
        /// Check if a player has a server gump. Leave blank to check if they have any server gump.
        /// Example:
        /// ```py
        /// if API.HasGump(0x12345678):
        ///   API.SysMsg("Found a gump!")
        ///```
        /// </summary>
        /// <param name="ID">Skip to check if player has any gump from server.</param>
        /// <returns>Returns gump id if found</returns>
        public uint HasGump(uint ID = uint.MaxValue) => InvokeOnMainThread<uint>
        (() =>
            {
                if (World.Player.HasGump && (World.Player.LastGumpID == ID || ID == uint.MaxValue))
                {
                    return World.Player.LastGumpID;
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
        public bool ReplyGump(int button, uint gump = uint.MaxValue) => InvokeOnMainThread
        (() =>
            {
                Gump g = UIManager.GetGumpServer(gump == uint.MaxValue ? World.Player.LastGumpID : gump);

                if (g != null)
                {
                    GameActions.ReplyGump(g.LocalSerial, g.ServerSerial, button, new uint[0] { }, new Tuple<ushort, string>[0]);
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
        public void CloseGump(uint ID = uint.MaxValue) => InvokeOnMainThread
        (() =>
            {
                uint gump = ID != uint.MaxValue ? ID : World.Player.LastGumpID;
                UIManager.GetGumpServer(gump)?.Dispose();
            }
        );

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
        public bool GumpContains(string text, uint ID = uint.MaxValue) => InvokeOnMainThread
        (() =>
            {
                Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);

                if (g == null)
                    return false;


                bool regex = text.StartsWith("$");

                if (regex)
                    text = text.Substring(1);

                foreach (Control c in g.Children)
                {
                    if (c is Label l && (l.Text.Contains(text) || (regex && System.Text.RegularExpressions.Regex.IsMatch(l.Text, text))))
                    {
                        return true;
                    }
                    else if (c is HtmlControl ht && (ht.Text.Contains(text) || (regex && System.Text.RegularExpressions.Regex.IsMatch(ht.Text, text))))
                    {
                        return true;
                    }
                }

                return false;
            }
        );

        /// <summary>
        /// Get a gump by ID.
        /// Example:
        /// ```py
        /// gump = API.GetGump()
        /// if gump:
        ///   API.SysMsg("Found the gump!")
        ///   API.CloseGump(gump)
        /// ```
        /// </summary>
        /// <param name="ID">Leabe blank to use last gump opened from server</param>
        /// <returns></returns>
        public Gump GetGump(uint ID = uint.MaxValue) => InvokeOnMainThread
        (() =>
            {
                Gump g = UIManager.GetGumpServer(ID == uint.MaxValue ? World.Player.LastGumpID : ID);

                return g;
            }
        );

        /// <summary>
        /// Toggle flying if you are a gargoyle.
        /// Example:
        /// ```py
        /// API.ToggleFly()
        /// ```
        /// </summary>
        public void ToggleFly() => InvokeOnMainThread
        (() =>
            {
                if (World.Player.Race == RaceType.GARGOYLE)
                    NetClient.Socket.Send_ToggleGargoyleFlying();
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
            InvokeOnMainThread
            (() =>
                {
                    switch (ability.ToLower())
                    {
                        case "primary": GameActions.UsePrimaryAbility(); break;

                        case "secondary": GameActions.UseSecondaryAbility(); break;

                        case "stun": NetClient.Socket.Send_StunRequest(); break;

                        case "disarm": NetClient.Socket.Send_DisarmRequest(); break;
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
        public bool PrimaryAbilityActive() => ((byte)World.Player.PrimaryAbility & 0x80) != 0;

        /// <summary>
        /// Check if your secondary ability is active.
        /// Example:
        /// ```py
        /// if API.SecondaryAbilityActive():
        ///   API.SysMsg("Secondary ability is active!")
        /// ```
        /// </summary>
        /// <returns>true/false</returns>
        public bool SecondaryAbilityActive() => ((byte)World.Player.SecondaryAbility & 0x80) != 0;

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
        public bool InJournal(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return false;

            foreach (var je in JournalEntries.ToArray())
            {
                if (msg.StartsWith("$") && Regex.IsMatch(je.Text, msg.Substring(1)))
                    return true;

                if (je.Text.Contains(msg))
                    return true;
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
        /// <returns></returns>
        public bool InJournalAny(IList<string> msgs)
        {
            if (msgs == null || msgs.Count == 0)
                return false;

            foreach (var je in JournalEntries.ToArray())
            {
                foreach (var msg in msgs)
                {
                    if (msg.StartsWith("$") && Regex.IsMatch(je.Text, msg.Substring(1)))
                        return true;

                    if (je.Text.Contains(msg))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clear your journal(This is specific for each script).
        /// Example:
        /// ```py
        /// API.ClearJournal()
        /// ```
        /// </summary>
        public void ClearJournal()
        {
            while (JournalEntries.TryDequeue(out _))
            {
            }
        }

        /// <summary>
        /// Pause the script.
        /// Example:
        /// ```py
        /// API.Pause(5)
        /// ```
        /// </summary>
        /// <param name="seconds"></param>
        public void Pause(double seconds)
        {
            if (seconds > 2000)
                seconds = 2000;

            Thread.Sleep((int)(seconds * 1000));
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

            InvokeOnMainThread
            (() =>
                {
                    if (LegionScripting.PyThreads.TryGetValue(t, out var s))
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
        public void ToggleAutoLoot() => InvokeOnMainThread(() => { ProfileManager.CurrentProfile.EnableAutoLoot ^= true; });

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
        public void AutoLootContainer(uint container) => InvokeOnMainThread(() =>
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
                case "honor": InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x01); }); break;
                case "sacrifice": InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x02); }); break;
                case "valor": InvokeOnMainThread(() => { NetClient.Socket.Send_InvokeVirtueRequest(0x03); }); break;
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
        public Entity NearestEntity(ScanType scanType, int maxDistance = 10) => InvokeOnMainThread
        (() =>
            {
                Found = 0;
                uint m = Utility.FindNearestCheckPythonIgnore((ScanTypeObject)scanType, this);

                var e = World.Get(m);

                if (e != null && e.Distance <= maxDistance)
                {
                    Found = e.Serial;
                    return e;
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
        public Mobile NearestMobile(IList<Notoriety> notoriety, int maxDistance = 10) => InvokeOnMainThread
        (() =>
            {
                Found = 0;
                if (notoriety == null || notoriety.Count == 0)
                    return null;

                var mob =  World.Mobiles.Values.Where
                (m => !m.IsDestroyed && !m.IsDead && m.Serial != World.Player.Serial && notoriety.Contains
                     ((Notoriety)(byte)m.NotorietyFlag) && m.Distance <= maxDistance && !OnIgnoreList(m)
                ).OrderBy(m => m.Distance).FirstOrDefault();

                if(mob != null)
                    Found = mob.Serial;

                return mob;
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
        public Item NearestCorpse(int distance = 3) => InvokeOnMainThread(() =>
        {
            Found = 0;
            var c = Utility.FindNearestCorpsePython(distance, this);

            if(c != null)
                Found = c.Serial;

            return c;
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
        public Mobile[] NearestMobiles(IList<Notoriety> notoriety, int maxDistance = 10) => InvokeOnMainThread<Mobile[]>
        (() =>
            {
                if (notoriety == null || notoriety.Count == 0)
                    return null;

                return World.Mobiles.Values.Where
                (m => !m.IsDestroyed && !m.IsDead && m.Serial != World.Player.Serial && notoriety.Contains
                     ((Notoriety)(byte)m.NotorietyFlag) && m.Distance <= maxDistance && !OnIgnoreList(m)
                ).OrderBy(m => m.Distance).ToArray();
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
        public Mobile FindMobile(uint serial) => InvokeOnMainThread(() =>
        {
            Found = 0;
            var mob = World.Mobiles.Get(serial);
            if(mob != null)
                Found = mob.Serial;

            return mob;
        });

        /// <summary>
        /// Return a list of all mobiles the client is aware of.
        /// Example:
        /// ```py
        /// mobiles = API.GetAllMobiles()
        /// if mobiles:
        ///   API.SysMsg("Found " + str(len(mobiles)) + " mobiles!")
        ///   for mob in mobiles:
        ///     API.SysMsg(mob.Name)
        ///     API.Pause(0.5)
        /// ```
        /// </summary>
        /// <returns></returns>
        public Mobile[] GetAllMobiles() => InvokeOnMainThread(() => { return World.Mobiles.Values.ToArray(); });

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
        public GameObject GetTile(int x, int y) => InvokeOnMainThread(() => { return World.Map.GetTile(x, y); });

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
        public Gump CreateGump(bool acceptMouseInput = true, bool canMove = true, bool keepOpen = false)
        {
            var g = new Gump(0, 0)
            {
                AcceptMouseInput = acceptMouseInput,
                CanMove = canMove,
                WantUpdateSize = true
            };

            if (!keepOpen)
                gumps.Add(g);

            return g;
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
        public void AddGump(Gump g) => InvokeOnMainThread(() => { UIManager.Add(g); });

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
        public Checkbox CreateGumpCheckbox(string text = "", ushort hue = 0, bool isChecked = false) => new Checkbox(0x00D2, 0x00D3, text, color: hue)
        {
            CanMove = true,
            IsChecked = isChecked
        };

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
            AlphaBlendControl bc = new AlphaBlendControl(opacity);
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
            ResizableStaticPic pic = new ResizableStaticPic(graphic, width, height)
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
            Button b = new Button(0, normal, pressed, hover, caption: text, normalHue: hue, hoverHue: hue);

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
            RadioButton rb = new RadioButton(group, inactive, active, text, color: hue);
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
        public TTFTextInputField CreateGumpTextBox(string text = "", int width = 200, int height = 30, bool multiline = false)
        {
            return new TTFTextInputField(width, height, text: text, multiline: multiline, convertHtmlColors: false)
            {
                CanMove = true
            };
        }

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
        public ScrollArea CreateGumpScrollArea(int x, int y, int width, int height)
        {
            return new ScrollArea(x, y, width, height, true);
        }

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
        public GumpPic CreateGumpPic(ushort graphic, int x = 0, int y = 0, ushort hue = 0)
        {
            return new GumpPic(x, y, graphic, hue);
        }

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
        public Control AddControlOnClick(Control control, object onClick, bool leftOnly = true)
        {
            if (control == null || onClick == null || !engine.Operations.IsCallable(onClick))
                return control;

            control.AcceptMouseInput = true;

            control.MouseUp += (s, e) =>
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
                            Console.WriteLine($"Script callback error: {ex}");
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
        public Control AddControlOnDisposed(Control control, object onDispose)
        {
            if (control == null || onDispose == null || !engine.Operations.IsCallable(onDispose))
                return control;

            control.Disposed += (s, e) =>
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
        /// Get a skill from the player. See the Skill class for what properties are available: https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs
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
        public Skill GetSkill(string skill) => InvokeOnMainThread
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
        public void DisplayRange(ushort distance, ushort hue = 22) => InvokeOnMainThread
        (() =>
            {
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
        public void ToggleScript(string scriptName) => InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    throw new Exception("[ToggleScript] Script name can't be empty.");

                foreach (var script in LegionScripting.LoadedScripts)
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
        public void PlayScript(string scriptName) => InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    GameActions.Print("[PlayScript] Script name can't be empty.");

                foreach (var script in LegionScripting.LoadedScripts)
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
        public void StopScript(string scriptName) => InvokeOnMainThread
        (() =>
            {
                if (string.IsNullOrEmpty(scriptName))
                    GameActions.Print("[StopScript] Script name can't be empty.");

                foreach (var script in LegionScripting.LoadedScripts)
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
        public void AddMapMarker(string name, int x = int.MaxValue, int y = int.MaxValue, int map = int.MaxValue, string color = "purple") => InvokeOnMainThread
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
        public void RemoveMapMarker(string name) => InvokeOnMainThread
        (() => {
            WorldMapGump wmap = UIManager.GetGump<WorldMapGump>();

            if (wmap == null || string.IsNullOrEmpty(name))
                return;

            wmap.RemoveUserMarker(name);
        });

        /// <summary>
        /// Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.
        /// Example:
        /// ```py
        /// if API.IsProcessingMoveQue():
        ///   API.Pause(0.5)
        /// ```
        /// </summary>
        /// <returns></returns>
        public bool IsProcessingMoveQue() => InvokeOnMainThread(() => !MoveItemQueue.Instance.IsEmpty);

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
                GameActions.Print("Var's must have a name.", 32);
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
                GameActions.Print("Var's must have a name.", 32);
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
                GameActions.Print("Var's must have a name.", 32);
                return defaultValue;
            }

            return PersistentVars.GetVar(scope, name, defaultValue);
        }
        #endregion
    }
}
