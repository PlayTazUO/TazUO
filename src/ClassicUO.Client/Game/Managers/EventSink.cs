using System;
using ClassicUO.Common;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.LegionScripting.ApiClasses;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers;

public class EventSink
{
    /// <summary>
    /// Invoked when the player is created
    /// </summary>
    public static event EventHandler<EventArgs> OnPlayerCreated;

    public static void InvokeOnPlayerCreated() => OnPlayerCreated?.Invoke(null, EventArgs.Empty);

    /// <summary>
    /// Invoked when an item is added to the client. The event's 'sender' is the Item
    /// </summary>
    internal static event EventHandler<EventArgs> OnItemCreatedInternal;

    /// <summary>
    /// Invoked when an item is added to the client.
    /// The event's argument is the ApiItem.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<ApiItem> OnItemCreated;

    internal static void InvokeOnItemCreated(Item sender)
    {
        OnItemCreatedInternal?.Invoke(sender, EventArgs.Empty);
        OnItemCreated?.Invoke(sender, new ApiItem(sender));
    }

    /// <summary>
    /// Invoked when an item is already in the client but has been updated.
    /// The event's 'sender' is the Item and event arguments are empty
    /// </summary>
    internal static event EventHandler<EventArgs> OnItemUpdatedInternal;

    /// <summary>
    /// Invoked when an item is already in the client but has been updated.
    /// The event's argument is the ApiItem.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<ApiItem> OnItemUpdated;

    internal static void InvokeOnItemUpdated(Item sender)
    {
        OnItemUpdatedInternal?.Invoke(sender, EventArgs.Empty);
        OnItemUpdated?.Invoke(sender, new ApiItem(sender));
    }

    /// <summary>
    /// Invoked when a corpse is added to the client. The event's 'sender' is the corpse Item
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<EventArgs> OnCorpseCreated;

    internal static void InvokeOnCorpseCreated(object sender) => OnCorpseCreated?.Invoke(sender, EventArgs.Empty);

    /// <summary>
    /// Invoked when the player is connected to a server
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<EventArgs> OnConnected;

    internal static void InvokeOnConnected(object sender) => OnConnected?.Invoke(sender, EventArgs.Empty);

    /// <summary>
    /// Invoked when the player is disconnected from the server
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<EventArgs> OnDisconnected;

    internal static void InvokeOnDisconnected(object sender) => OnDisconnected?.Invoke(sender, EventArgs.Empty);

    /// <summary>
    /// Invoked when any message is received from the server after client processing
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<MessageEventArgs> MessageReceived;

    internal static void InvokeMessageReceived(object sender, MessageEventArgs e) => MessageReceived?.Invoke(sender, e);

    /// <summary>
    /// Invoked when any message is received from the server *before* client processing
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<MessageEventArgs> RawMessageReceived;

    internal static void InvokeRawMessageReceived(object sender, MessageEventArgs e) => RawMessageReceived?.Invoke(sender, e);

    /// <summary>
    ///  Not currently used. May be removed later or put into use, not sure right now
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<MessageEventArgs> ClilocMessageReceived;

    internal static void InvokeClilocMessageReceived(object sender, MessageEventArgs e) => ClilocMessageReceived?.Invoke(sender, e);

    /// <summary>
    ///  Invoked when a message is added to the journal
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<JournalEntry> JournalEntryAdded;

    internal static void InvokeJournalEntryAdded(object sender, JournalEntry e) => JournalEntryAdded?.Invoke(sender, e);

    /// <summary>
    /// Invoked when the server requests that a sound be played
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<SoundEventArgs> SoundPlayed;
    internal static void InvokeSoundPlayed(SoundEventArgs e) => SoundPlayed?.Invoke(null, e);

    /// <summary>
    /// Invoked when an object's property list data (Tooltip text for items) is received
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<OPLEventArgs> OPLOnReceive;

    internal static void InvokeOPLOnReceive(object sender, OPLEventArgs e) => OPLOnReceive?.Invoke(sender, e);

    /// <summary>
    /// Invoked when a buff is "added" to a player
    /// </summary>
    internal static event EventHandler<BuffEventArgs> OnBuffAddedInternal;

    /// <summary>
    /// Invoked when a buff is "added" to a player.
    /// The event's argument is the ApiBuff.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<ApiBuff> OnBuffAdded;

    internal static void InvokeOnBuffAdded(object sender, BuffEventArgs e)
    {
        OnBuffAddedInternal?.Invoke(sender, e);
        OnBuffAdded?.Invoke(sender, new ApiBuff(e.Buff));
    }

    /// <summary>
    /// Invoked when a buff is "removed" to a player (Called before removal)
    /// </summary>
    internal static event EventHandler<BuffEventArgs> OnBuffRemovedInternal;

    /// <summary>
    /// Invoked when a buff is "removed" to a player (Called before removal)
    /// The event's argument is the ApiBuff.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<ApiBuff> OnBuffRemoved;

    internal static void InvokeOnBuffRemoved(object sender, BuffEventArgs e)
    {
        OnBuffRemovedInternal?.Invoke(sender, e);
        OnBuffRemoved?.Invoke(sender, new ApiBuff(e.Buff));
    }

    /// <summary>
    /// Invoked when the player's position is changed
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<PositionChangedArgs> OnPositionChanged;

    internal static void InvokeOnPositionChanged(object sender, PositionChangedArgs e) => OnPositionChanged?.Invoke(sender, e);

    /// <summary>
    /// Invoked when any entity in the game receives damage, not necessarily the player.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<int> OnEntityDamage;

    internal static void InvokeOnEntityDamage(object sender, int e) => OnEntityDamage?.Invoke(sender, e);

    /// <summary>
    /// Invoked when a container is opened.
    /// The event's 'sender' is the Item, the event's argument is the item's serial
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<uint> OnOpenContainer;

    internal static void InvokeOnOpenContainer(Item sender, uint serial) => OnOpenContainer?.Invoke(sender, serial);

    /// <summary>
    /// Invoked when the player receives a death packet from the server
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<uint> OnPlayerDeath;

    internal static void InvokeOnPlayerDeath(object sender, uint serial) => OnPlayerDeath?.Invoke(sender, serial);

    /// <summary>
    ///  Invoked when the player or server tells the client to path find
    ///  Vector is X, Y, Z, and Distance
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<Vector4> OnPathFinding;

    internal static void InvokeOnPathFinding(object sender, Vector4 e) => OnPathFinding?.Invoke(sender, e);

    /// <summary>
    /// Invoked when the server asks the client to generate some weather
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<WeatherEventArgs> OnSetWeather;

    internal static void InvokeOnSetWeather(object sender, WeatherEventArgs e) => OnSetWeather?.Invoke(sender, e);

    /// <summary>
    /// Invoked after the player's hit points have changed.
    /// </summary>
    [ApiEvent]
    internal static event EventHandler<int> OnPlayerHitsChanged;

    internal static void InvokeOnPlayerStatChange(object sender, int newValue) => OnPlayerHitsChanged?.Invoke(sender, newValue);

    /// <summary>
    /// Called when the visual spell manager detects a spell being cast.
    /// The event argument is the spell ID.
    /// </summary>
    public static event EventHandler<int> SpellCastBegin;

    /// <summary>Invokes <see cref="SpellCastBegin" />.</summary>
    public static void InvokeSpellCastBegin(int spell) => SpellCastBegin?.Invoke(null, spell);

    /// <summary>
    /// Called when the visual spell manager detects a spell done being cast.
    /// This event has no sender or arguments.
    /// </summary>
    public static event EventHandler SpellCastEnd;

    /// <summary>Invokes <see cref="SpellCastEnd" />.</summary>
    public static void InvokeSpellCastEnd() => SpellCastEnd?.Invoke(null, EventArgs.Empty);

    /// <summary>
    ///  Invoked when a mobile's notoriety has changed
    /// </summary>
    // [ApiEvent] - Note - Cannot currently be automatically exposed as an API event due to implementation limitation (non 'object' sender)
    public static event EventHandler<uint, NotorietyFlag> NotorietyFlagChanged;

    public static void InvokeNotorietyChange(uint serial, NotorietyFlag flag) => NotorietyFlagChanged?.Invoke(serial, flag);


    /// <summary>
    /// Invoked when a mobile is created
    /// </summary>
    public static event EventHandler<Mobile> MobileCreated;

    /// <summary>
    /// Invoked when a mobile is created.
    /// The event's sender is null and the argument is an ApiMobile.
    /// </summary>
    [ApiEvent]
    public static event EventHandler<ApiMobile> ApiMobileCreated;

    public static void InvokeMobileCreated(Mobile m)
    {
        MobileCreated?.Invoke(m, m);
        ApiMobileCreated?.Invoke(null, new ApiMobile(m));
    }

    public static event EventHandler<SkillChangeArgs> SkillValueChangedEvent;
    public static event EventHandler<SkillChangeArgs> SkillBaseChangedEvent;
    public static event EventHandler<SkillChangeArgs> SkillCapChangedEvent;
    public static void InvokeSkillValueChanged(int index) => SkillValueChangedEvent?.Invoke(null, new SkillChangeArgs(index));
    public static void InvokeSkillBaseChanged(int index) => SkillBaseChangedEvent?.Invoke(null, new SkillChangeArgs(index));
    public static void InvokeSkillCapChanged(int index) => SkillCapChangedEvent?.Invoke(null, new SkillChangeArgs(index));
}

public class SkillChangeArgs : EventArgs
{
    public int Index;
    public SkillChangeArgs(int index)
    {
        Index = index;
    }
}

/// <summary>
/// Describes an object's property list data (Tooltip text for items)'
/// </summary>
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

/// <summary>
/// A sound play event
/// </summary>
/// <param name="index">The sound's index, that is, its exact type</param>
/// <param name="x">The sound's X origin</param>
/// <param name="y">The sound's Y origin</param>
public class SoundEventArgs(int index, int x, int y) : EventArgs
{
    public readonly int Index = index;
    public readonly int X = x;
    public readonly int Y = y;

    public DateTime Time = DateTime.Now;
}
