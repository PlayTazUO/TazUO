using System;
using ClassicUO.Game.Data;

public static class MessageTypeFilter
{
    // Maps each enum value to a unique bit position (0 to 31)
    private static int GetBitPosition(MessageType type) => type switch
    {
        MessageType.Regular => 0,
        MessageType.System => 1,
        MessageType.Emote => 2,
        MessageType.Limit3Spell => 3,
        MessageType.Label => 4,
        MessageType.Focus => 5,
        MessageType.Whisper => 6,
        MessageType.Yell => 7,
        MessageType.Spell => 8,
        MessageType.Guild => 9,
        MessageType.Alliance => 10,
        MessageType.Command => 11,
        MessageType.Encoded => 12,
        MessageType.ChatSystem => 13,
        MessageType.Damage => 14,
        MessageType.Discord => 15,
        MessageType.Party => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown MessageType: {type}")
    };

    /// <summary>
    /// Checks if a specific MessageType is enabled within the current settings mask.
    /// </summary>
    public static bool IsEnabled(uint currentSettings, MessageType type)
    {
        int bitPosition = GetBitPosition(type);
        return (currentSettings & (1U << bitPosition)) != 0;
    }

    /// <summary>
    /// Enables or disables a specific MessageType and returns the updated uint value.
    /// </summary>
    public static uint SetEnabled(uint currentSettings, MessageType type, bool enable)
    {
        int bitPosition = GetBitPosition(type);
        
        if (enable)
        {
            // Bitwise OR to set the bit to 1
            return currentSettings | (1U << bitPosition);
        }
        else
        {
            // Bitwise AND with an inverted mask to set the bit to 0
            return currentSettings & ~(1U << bitPosition);
        }
    }
}