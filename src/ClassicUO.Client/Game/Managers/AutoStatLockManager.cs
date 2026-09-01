using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers;

public class AutoStatLockState
{
    public bool Enabled { get; set; } = false;
    public bool StrEnabled { get; set; } = false;
    public ushort DesiredStr { get; set; } = 0;
    public bool DexEnabled { get; set; } = false;
    public ushort DesiredDex { get; set; } = 0;
    public bool IntEnabled { get; set; } = false;
    public ushort DesiredInt { get; set; } = 0;
}

[JsonSerializable(typeof(AutoStatLockState))]
public partial class AutoStatLockStateContext : JsonSerializerContext { }

public class AutoStatLockManager
{
    private static readonly AutoStatLockState _emptyState = new();

    public static AutoStatLockManager Instance { get; } = new();

    private AutoStatLockManager() { }

    public AutoStatLockState State
    {
        get
        {
            Profile profile = ProfileManager.CurrentProfile;
            return profile?.AutoStatLockState ?? _emptyState;
        }
    }

    public void Save()
    {
        ProfileManager.CurrentProfile?.Save();
    }

    public void OnStatsUpdated(World world)
    {
        AutoStatLockState state = State;

        if (!state.Enabled || world.Player == null) return;

        CheckAndApplyStatLock(0, world.Player.Strength, state.DesiredStr, state.StrEnabled, ref world.Player.StrLock);
        CheckAndApplyStatLock(1, world.Player.Dexterity, state.DesiredDex, state.DexEnabled, ref world.Player.DexLock);
        CheckAndApplyStatLock(2, world.Player.Intelligence, state.DesiredInt, state.IntEnabled, ref world.Player.IntLock);
    }

    private static void CheckAndApplyStatLock(byte statIndex, ushort currentValue, ushort desiredValue, bool enabled, ref Lock lockState)
    {
        if (!enabled || desiredValue == 0 || currentValue == 0) return;

        Lock newLock;
        if (currentValue < desiredValue)
            newLock = Lock.Up;
        else if (currentValue > desiredValue)
            newLock = Lock.Down;
        else
            newLock = Lock.Locked;

        if (lockState == newLock) return;

        lockState = newLock;
        GameActions.ChangeStatLock(statIndex, newLock);
    }
}
