using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.Managers
{
    public class EventSink
    {
        /// <summary>
        /// Invoked when the player is created
        /// </summary>
        public static event EventHandler<EventArgs> OnPlayerCreated;
        public static void InvokeOnPlayerCreated() => OnPlayerCreated?.Invoke(null, EventArgs.Empty);

        /// <summary>
        /// Invoked when an item is added to the client, sender is the Item
        /// </summary>
        internal static event EventHandler<EventArgs> OnItemCreated;
        internal static event EventHandler<uint> PyOnItemCreated;
        internal static void InvokeOnItemCreated(Item sender)
        {
            OnItemCreated?.Invoke(sender, EventArgs.Empty);
            PyOnItemCreated?.Invoke(sender, sender.Serial);
        }

        /// <summary>
        /// Invoked when an item is already in the client but has been updated, sender is the Item
        /// </summary>
        internal static event EventHandler<EventArgs> OnItemUpdated;
        internal static void InvokeOnItemUpdated(Item sender) => OnItemUpdated?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when a corpse is added to the client, sender is the corpse Item
        /// </summary>
        internal static event EventHandler<EventArgs> OnCorpseCreated;
        internal static void InvokeOnCorpseCreated(object sender) => OnCorpseCreated?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when the player is connected to a server
        /// </summary>
        internal static event EventHandler<EventArgs> OnConnected;
        internal static void InvokeOnConnected(object sender) => OnConnected?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when the player is connected to a server
        /// </summary>
        internal static event EventHandler<EventArgs> OnDisconnected;
        internal static void InvokeOnDisconnected(object sender) => OnDisconnected?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when any message is received from the server after client processing
        /// </summary>
        internal static event EventHandler<MessageEventArgs> MessageReceived;
        internal static void InvokeMessageReceived(object sender, MessageEventArgs e) => MessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Invoked when any message is received from the server *before* client processing
        /// </summary>
        internal static event EventHandler<MessageEventArgs> RawMessageReceived;
        internal static void InvokeRawMessageReceived(object sender, MessageEventArgs e) => RawMessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Not currently used. May be removed later or put into use, not sure right now
        /// </summary>
        internal static event EventHandler<MessageEventArgs> ClilocMessageReceived;
        internal static void InvokeClilocMessageReceived(object sender, MessageEventArgs e) => ClilocMessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Invoked anytime a message is added to the journal
        /// </summary>
        internal static event EventHandler<JournalEntry> JournalEntryAdded;
        internal static void InvokeJournalEntryAdded(object sender, JournalEntry e) => JournalEntryAdded?.Invoke(sender, e);

        /// <summary>
        /// Invoked anytime we receive object property list data (Tooltip text for items)
        /// </summary>
        internal static event EventHandler<OPLEventArgs> OPLOnReceive;
        internal static void InvokeOPLOnReceive(object sender, OPLEventArgs e) => OPLOnReceive?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a buff is "added" to a player
        /// </summary>
        internal static event EventHandler<BuffEventArgs> OnBuffAdded;
        internal static event EventHandler<LegionScripting.PyClasses.Buff> PyOnBuffAdded;
        internal static void InvokeOnBuffAdded(object sender, BuffEventArgs e)
        {
            OnBuffAdded?.Invoke(sender, e);
            PyOnBuffAdded?.Invoke(sender, new LegionScripting.PyClasses.Buff(e.Buff));
        }

        /// <summary>
        /// Invoked when a buff is "removed" to a player (Called before removal)
        /// </summary>
        internal static event EventHandler<BuffEventArgs> OnBuffRemoved;
        internal static event EventHandler<LegionScripting.PyClasses.Buff> PyOnBuffRemoved;
        internal static void InvokeOnBuffRemoved(object sender, BuffEventArgs e)
        {
            OnBuffRemoved?.Invoke(sender, e);
            PyOnBuffRemoved?.Invoke(sender, new LegionScripting.PyClasses.Buff(e.Buff));
        }

        /// <summary>
        /// Invoked when the players position is changed
        /// </summary>
        internal static event EventHandler<PositionChangedArgs> OnPositionChanged;
        internal static void InvokeOnPositionChanged(object sender, PositionChangedArgs e) => OnPositionChanged?.Invoke(sender, e);

        /// <summary>
        /// Invoked when any entity in game receives damage, not necessarily the player.
        /// </summary>
        internal static event EventHandler<int> OnEntityDamage;
        internal static void InvokeOnEntityDamage(object sender, int e) => OnEntityDamage?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a container is opened. Sender is the Item, serial is the item serial.
        /// </summary>
        internal static event EventHandler<uint> OnOpenContainer;
        internal static void InvokeOnOpenContainer(Item sender, uint serial) => OnOpenContainer?.Invoke(sender, serial);

        /// <summary>
        /// Invoked when the player receives a death packet from the server
        /// </summary>
        internal static event EventHandler<uint> OnPlayerDeath;
        internal static void InvokeOnPlayerDeath(object sender, uint serial) => OnPlayerDeath?.Invoke(sender, serial);

        /// <summary>
        /// Invoked when the player or server tells the client to path find
        /// Vector is X, Y, Z and Distance
        /// </summary>
        internal static event EventHandler<Vector4> OnPathFinding;
        internal static void InvokeOnPathFinding(object sender, Vector4 e) => OnPathFinding?.Invoke(sender, e);

        /// <summary>
        /// Invoked when the server asks the client to generate some weather
        /// </summary>
        internal static event EventHandler<WeatherEventArgs> OnSetWeather;
        internal static void InvokeOnSetWeather(object sender, WeatherEventArgs e) => OnSetWeather?.Invoke(sender, e);

        /// <summary>
        /// Invoked when the players hits changed.
        /// </summary>
        internal static event EventHandler<int> OnPlayerHitsChanged;
        internal static void InvokeOnPlayerStatChange(object sender, int newValue) => OnPlayerHitsChanged?.Invoke(sender, newValue);

        /// <summary>
        /// Called when the visual spell manager detects a spell being cast.
        /// The event argument is the spell ID.
        /// </summary>
        public static event EventHandler<int> SpellCastBegin;
        /// <summary>Invokes <see cref="SpellCastBegin"/>.</summary>
        public static void InvokeSpellCastBegin(int spell) => SpellCastBegin?.Invoke(null, spell);
        /// <summary>
        /// Called when the visual spell manager detects a spell done being cast.
        /// The event argument is the spell ID.
        /// </summary>
        public static event EventHandler SpellCastEnd;
        /// <summary>Invokes <see cref="SpellCastEnd"/>.</summary>
        public static void InvokeSpellCastEnd() => SpellCastEnd?.Invoke(null, null);
        /// <summary>
        /// Called when the visual spell manager detects a spell recovery phase.
        /// The event argument is the spell ID.
        /// </summary>
        public static event EventHandler<int> SpellRecoveryBegin;
        /// <summary>Invokes <see cref="SpellRecoveryBegin"/>.</summary>
        public static void InvokeSpellRecoveryBegin(int spell) => SpellRecoveryBegin?.Invoke(null, spell);
        /// <summary>
        /// Called when the visual spell manager detects a spell recovery phase end.
        /// The event argument is the spell ID.
        /// </summary>
        public static event EventHandler SpellRecoveryEnd;
        /// <summary>Invokes <see cref="SpellRecoveryEnd"/>.</summary>
        public static void InvokeSpellRecoveryEnd() => SpellRecoveryEnd?.Invoke(null, null);

    }

    public class OPLEventArgs : EventArgs
    {
        public readonly uint Serial;
        public readonly string Name;
        public readonly string Data;

        public OPLEventArgs(uint serial, string name, string data)
        {
            Serial = serial;
            Name = name;
            Data = data;
        }
    }

    public class BuffEventArgs : EventArgs
    {
        public BuffEventArgs(BuffIcon buff)
        {
            Buff = buff;
        }

        public BuffIcon Buff { get; }
    }

    public class PositionChangedArgs : EventArgs
    {
        public PositionChangedArgs(Vector3 newlocation)
        {
            Newlocation = newlocation;
        }

        public Vector3 Newlocation { get; }
    }

    public class WeatherEventArgs : EventArgs
    {
        public WeatherEventArgs(WeatherType type, byte count, byte temp)
        {
            Type = type;
            Count = count;
            Temp = temp;
        }

        public WeatherType Type { get; }
        public byte Count { get; }
        public byte Temp { get; }
    }
}
