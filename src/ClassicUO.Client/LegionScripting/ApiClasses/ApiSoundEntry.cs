using System;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>Represents a sound entry exposed to Legion scripting.</summary>
public class ApiSoundEntry(SoundEventArgs entry)
{
    /// <summary>Sound ID/index.</summary>
    public int ID = entry.Index;
    /// <summary>World X coordinate.</summary>
    public int X = entry.X;
    /// <summary>World Y coordinate.</summary>
    public int Y = entry.Y;
    /// <summary>Timestamp when the sound was observed.</summary>
    public DateTime Time = entry.Time;
}
