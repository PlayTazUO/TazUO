using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Game.Managers.SpellVisualRange;
using Lock = System.Threading.Lock;
using Timer = System.Timers.Timer;

namespace ClassicUO.Game.Managers
{
    using Utility.Logging;

    public class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance => _instance ??= new SpellVisualRangeManager();

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;
        public DateTime LastSpellTime { get; private set; } = DateTime.Now;
        public Dictionary<int, SpellRangeInfo> SpellRangeCache => _spellRangeCache;

        private readonly string _savePath = Path.Combine(CUOEnviroment.ExecutablePath ?? "", "Data", "Profiles", "SpellVisualRange.json");
        private readonly string _overridePath = Path.Combine(ProfileManager.ProfilePath ?? "", "SpellVisualRange.json");

        private readonly Dictionary<int, SpellRangeInfo> _spellRangeCache = new();
        private readonly Dictionary<int, SpellRangeInfo> _spellRangeOverrideCache = new();
        private readonly Dictionary<string, SpellRangeInfo> _spellRangePowerWordCache = new();

        private bool _loaded = false;
        private static SpellVisualRangeManager _instance;

        private readonly object _castingLock = new();
        private bool IsCasting { get; set; }
        private SpellRangeInfo CurrentSpell { get; set; }
        private bool _frozenBySpell;
        private System.Threading.CancellationTokenSource _castCts;
        private System.Threading.CancellationTokenSource _recoveryCts;

        //Taken from Dust client
        private static readonly int[] _stopAtClilocs =
        [
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060 // You cannot cast a spell while calmed.
        ];

        private readonly World _world;

        private SpellVisualRangeManager()
        {
            _world = Client.Game.UO.World;
            Load();
        }

        /// <summary>
        /// Reindexes the PowerWords mapping for the provided spell in the runtime cache.
        /// </summary>
        public void ReindexSpellPowerWords(SpellRangeInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.PowerWords))
                return;

            _spellRangePowerWordCache[info.PowerWords] = info;
        }

        private void OnRawMessageReceived(object sender, MessageEventArgs e) => Task.Run(() =>
                                                                                         {
                                                                                             if (_loaded && e.Parent != null && ReferenceEquals(e.Parent, _world.Player))
                                                                                                 if (_spellRangePowerWordCache.TryGetValue(e.Text.Trim(), out SpellRangeInfo spell))
                                                                                                     SetCasting(spell);
                                                                                         });
        public void OnClilocReceived(int cliloc) => Task.Factory.StartNew(() =>
                                                             {
                                                                 if (IsCasting && _stopAtClilocs.Contains(cliloc)) ClearCasting();
                                                             });

        private void SetCasting(SpellRangeInfo spell)
        {
            lock (_castingLock)
            {
                LastSpellTime = DateTime.Now;
                CurrentSpell = spell;
                IsCasting = true;
            }

            if (CurrentSpell != null && CurrentSpell.FreezeCharacterWhileCasting)
            {
                _frozenBySpell = true;
                _world.Player.Flags |= Flags.Frozen;
            }

            if (ProfileManager.CurrentProfile?.EnableSpellIndicators == true)
            {
                CastTimerProgressBar bar = UIManager.GetGump<CastTimerProgressBar>() ?? new CastTimerProgressBar(_world);
                if (bar.Parent == null)
                    UIManager.Add(bar);
                bar.OnSpellCastBegin();
            }

            EventSink.InvokeSpellCastBegin(spell.ID);

            double castTime = spell.GetEffectiveCastTime();
            _castCts?.Cancel();
            _castCts?.Dispose();
            _castCts = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken ct = _castCts.Token;
            _ = Task.Run(async () =>
                     {
                         try
                         {
                             await Task.Delay(TimeSpan.FromSeconds(castTime), ct);
                         }
                         catch (TaskCanceledException) { return; }

                         if (IsCasting && CurrentSpell == spell)
                         {
                             if (spell.ExpectTargetCursor && _world.TargetManager.IsTargeting)
                                 return;

                             ClearCasting();
                         }
                     }, ct);
        }

        public void ClearCasting()
        {
            SpellRangeInfo spellSnapshot;
            lock (_castingLock)
            {
                if (_frozenBySpell)
                    _world.Player.Flags &= ~Flags.Frozen;
                _frozenBySpell = false;
                spellSnapshot = CurrentSpell;
            }

            if (spellSnapshot == null)
            {
                IsCasting = false;
                _world.Player.Flags &= ~Flags.Frozen;
                return;
            }


            if (spellSnapshot.RecoveryTime > 0)
                _ = StartRecovery(spellSnapshot);
            else
                EndRecovery(spellSnapshot);
        }

        private async Task StartRecovery(SpellRangeInfo spell)
        {
            if (spell == null)
                return;

            EventSink.InvokeSpellCastEnd();
            EventSink.InvokeSpellRecoveryBegin(spell.ID);

            _recoveryCts?.Cancel();
            _recoveryCts?.Dispose();
            _recoveryCts = new System.Threading.CancellationTokenSource();

            if (ProfileManager.CurrentProfile?.EnableSpellIndicators == true)
            {
                CastTimerProgressBar bar = UIManager.GetGump<CastTimerProgressBar>() ?? new CastTimerProgressBar(_world);
                if (bar.Parent == null)
                    UIManager.Add(bar);
                bar.OnRecoveryBegin();
            }

            double recTime = spell.GetEffectiveRecoveryTime();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(recTime), _recoveryCts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            EndRecovery(spell);
        }

        private void EndRecovery(SpellRangeInfo spell)
        {
            int endedSpellId;

            lock (_castingLock)
            {
                endedSpellId = CurrentSpell?.ID ?? spell?.ID ?? 0;
                CurrentSpell = null;
                IsCasting = false;
                if (_frozenBySpell)
                {
                    _world.Player.Flags &= ~Flags.Frozen;
                    _frozenBySpell = false;
                }
                EventSink.InvokeSpellRecoveryEnd();
            }
        }

        public SpellRangeInfo GetCurrentSpell() => CurrentSpell;

        #region Load and unload
        public void OnSceneLoad() => EventSink.RawMessageReceived += OnRawMessageReceived;

        public void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;
            _castCts?.Cancel();
            _castCts?.Dispose();
            _recoveryCts?.Cancel();
            _recoveryCts?.Dispose();
            _instance = null;
        }
        #endregion

        public bool IsTargetingAfterCasting()
        {
            if (!_loaded || CurrentSpell == null || !IsCasting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators) return false;

            if (_world?.TargetManager?.IsTargeting ?? (CurrentSpell.ShowCastRangeDuringCasting && IsCastingWithoutTarget()))
                if (LastSpellTime + TimeSpan.FromSeconds(CurrentSpell.MaxDuration) > DateTime.Now)
                    return true;

            return false;
        }

        public bool IsCastingWithoutTarget()
        {
            if (!_loaded || CurrentSpell == null || !IsCasting || CurrentSpell.CastTime <= 0 || _world.TargetManager.IsTargeting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators) return false;

            if (LastSpellTime + TimeSpan.FromSeconds(CurrentSpell.MaxDuration) > DateTime.Now)
            {
                if (LastSpellTime + TimeSpan.FromSeconds(CurrentSpell.CastTime) > DateTime.Now)
                    return true;
                else if (CurrentSpell.FreezeCharacterWhileCasting) _world.Player.Flags &= ~Flags.Frozen;
            }
            else if (CurrentSpell.FreezeCharacterWhileCasting) _world.Player.Flags &= ~Flags.Frozen;

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!_loaded || CurrentSpell == null) return hue;

            if (CurrentSpell.CastRange > 0 && o.Distance <= CurrentSpell.CastRange) hue = CurrentSpell.Hue;

            int cDistance = o.DistanceFrom(LastCursorTileLoc);

            if (CurrentSpell.CursorSize > 0 && cDistance < CurrentSpell.CursorSize)
            {
                if (CurrentSpell.IsLinear)
                {
                    if (GetDirection(new Vector2(_world.Player.X, _world.Player.Y), LastCursorTileLoc) == SpellDirection.EastWest)
                    { //X
                        if (o.Y == LastCursorTileLoc.Y) hue = CurrentSpell.CursorHue;
                    }
                    else
                    { //Y
                        if (o.X == LastCursorTileLoc.X) hue = CurrentSpell.CursorHue;
                    }
                }
                else
                    hue = CurrentSpell.CursorHue;
            }

            return hue;
        }

        private static SpellDirection GetDirection(Vector2 from, Vector2 to)
        {
            int dx = (int)(from.X - to.X);
            int dy = (int)(from.Y - to.Y);
            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            if (rx >= 0 && ry >= 0)
                return SpellDirection.SouthNorth;
            if (rx >= 0)
                return SpellDirection.EastWest;
            if (ry >= 0)
                return SpellDirection.EastWest;

            return SpellDirection.SouthNorth;
        }

        #region Save and load
        private Timer _saveTimer;
        private readonly Lock _saveLock = new ();
        private volatile bool _hasPendingChanges;
        private void Load()
        {
            _spellRangeCache.Clear();

            if (!File.Exists(_savePath))
            {
                System.Reflection.Assembly assembly = GetType().Assembly;
                string resourceName = "ClassicUO.Game.Managers.DefaultSpellIndicatorConfig.json";

                try
                {
                    using Stream stream = assembly.GetManifestResourceStream(resourceName);
                    using var reader = new StreamReader(stream);
                    LoadFromString(reader.ReadToEnd());
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    CreateAndLoadDataFile();
                }

                AfterLoad();
                _loaded = true;
                Save();
            }
            else
                try
                {
                    string data = File.ReadAllText(_savePath);
                    SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                    foreach (SpellRangeInfo entry in fileData)
                        _spellRangeCache.Add(entry.ID, entry);

                    AfterLoad();
                    _loaded = true;
                }
                catch
                {
                    CreateAndLoadDataFile();
                    AfterLoad();
                    _loaded = true;
                }
        }

        private void LoadOverrides()
        {
            _spellRangeOverrideCache.Clear();

            if (!File.Exists(_overridePath)) return;

            try
            {
                string data = File.ReadAllText(_overridePath);
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                foreach (SpellRangeInfo entry in fileData)
                    _spellRangeOverrideCache.Add(entry.ID, entry);

                foreach (SpellRangeInfo entry in _spellRangeOverrideCache.Values)
                {
                    if (string.IsNullOrEmpty(entry.PowerWords))
                    {
                        var spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                        if (spellD == SpellDefinition.EmptySpell)
                            SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);

                        if (spellD != SpellDefinition.EmptySpell)
                            entry.PowerWords = spellD.PowerWords;
                    }
                    if (!string.IsNullOrEmpty(entry.PowerWords))
                        _spellRangePowerWordCache[entry.PowerWords] = entry;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public bool LoadFromString(string json)
        {
            try
            {
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize(json, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                _loaded = false;
                _spellRangeCache.Clear();

                foreach (SpellRangeInfo entry in fileData) _spellRangeCache.Add(entry.ID, entry);
                AfterLoad();
                LoadOverrides();
                _loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                _loaded = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private void AfterLoad()
        {
            _spellRangePowerWordCache.Clear();
            foreach (SpellRangeInfo entry in _spellRangeCache.Values)
            {
                if (string.IsNullOrEmpty(entry.PowerWords))
                {
                    var spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                    if (spellD == SpellDefinition.EmptySpell) SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);

                    if (spellD != SpellDefinition.EmptySpell) entry.PowerWords = spellD.PowerWords;
                }
                if (!string.IsNullOrEmpty(entry.PowerWords)) _spellRangePowerWordCache.Add(entry.PowerWords, entry);
            }
            LoadOverrides();
        }

        private void CreateAndLoadDataFile()
        {
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMagery.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNecromancy.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsChivalry.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsBushido.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNinjitsu.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsSpellweaving.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMysticism.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMastery.GetAllSpells) _spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));

            Save();
        }

        public void DelayedSave()
        {
            lock (_saveLock)
            {
                _hasPendingChanges = true;

                // Cancel existing timer if it's running
                _saveTimer?.Dispose();

                _saveTimer = new Timer();
                _saveTimer.Interval = 500;
                _saveTimer.Elapsed += (_, _) => { PerformSave(); };
                _saveTimer.Start();
            }
        }

        private void PerformSave()
        {
            lock (_saveLock)
            {
                if (!_hasPendingChanges)
                    return;

                _hasPendingChanges = false;
            }

            string tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                string fileData = JsonSerializer.Serialize(_spellRangeCache.Values.ToArray(), SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);
                File.WriteAllText(tempPath, fileData);

                if (File.Exists(_savePath))
                    File.Delete(_savePath);
                File.Move(tempPath, _savePath);
            }
            catch (Exception e)
            {
                Log.Error($"Save failed: {e}");
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        public void Save()
        {
            lock (_saveLock)
            {
                _saveTimer?.Dispose();
                if (_hasPendingChanges)
                    PerformSave();
            }
        }
        #endregion

        private enum SpellDirection
        {
            EastWest,
            SouthNorth
        }
    }
}
