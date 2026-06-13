using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;
using ClassicUO.Game;
using ClassicUO;

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
    private AutoStatLockState _state = new();
    private bool _isLoaded;
    private uint _lastLoadedSerial;

    public static AutoStatLockManager Instance { get; } = new();

    private AutoStatLockManager() { }

    public AutoStatLockState State
    {
        get
        {
            EnsureLoaded();
            return _state;
        }
    }

    private void EnsureLoaded()
    {
        uint playerSerial = World.Instance?.Player?.Serial ?? 0;
        if (playerSerial == 0) return;

        if (!_isLoaded || _lastLoadedSerial != playerSerial)
        {
            _lastLoadedSerial = playerSerial;
            Load();
        }
    }

    private void Load()
    {
        if (Client.Settings == null)
        {
            Log.Warn("SQLSettings not available for AutoStatLockManager");
            _isLoaded = true;
            return;
        }

        try
        {
            string json = Client.Settings.Get(SettingsScope.Char, Constants.SqlSettings.AUTO_STAT_LOCK, "");
            _state = !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.Deserialize(json, AutoStatLockStateContext.Default.AutoStatLockState) ?? new()
                : new();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load auto stat lock state: {ex.Message}");
            _state = new();
        }

        _isLoaded = true;
    }

    public void Save()
    {
        if (Client.Settings == null) return;

        try
        {
            string json = JsonSerializer.Serialize(_state, AutoStatLockStateContext.Default.AutoStatLockState);
            _ = Client.Settings.SetAsync(SettingsScope.Char, Constants.SqlSettings.AUTO_STAT_LOCK, json);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save auto stat lock state: {ex.Message}");
        }
    }

    public void OnStatsUpdated(World world)
    {
        EnsureLoaded();

        if (!_state.Enabled || world.Player == null) return;

        CheckAndApplyStatLock(0, world.Player.Strength, _state.DesiredStr, _state.StrEnabled, ref world.Player.StrLock);
        CheckAndApplyStatLock(1, world.Player.Dexterity, _state.DesiredDex, _state.DexEnabled, ref world.Player.DexLock);
        CheckAndApplyStatLock(2, world.Player.Intelligence, _state.DesiredInt, _state.IntEnabled, ref world.Player.IntLock);
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
