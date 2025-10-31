using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Game.Managers.SpellVisualRange;
using Timer = System.Timers.Timer;

namespace ClassicUO.Game.Managers
{
    using System.Text.Json.Serialization;
    using Utility.Logging;

    public class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance => instance ??= new SpellVisualRangeManager();

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;
        public DateTime LastSpellTime { get; private set; } = DateTime.Now;
        public Dictionary<int, SpellRangeInfo> SpellRangeCache => spellRangeCache;

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath ?? "", "Data", "Profiles", "SpellVisualRange.json");
        private string overridePath = Path.Combine(ProfileManager.ProfilePath ?? "", "SpellVisualRange.json");

        private Dictionary<int, SpellRangeInfo> spellRangeCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<int, SpellRangeInfo> spellRangeOverrideCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<string, SpellRangeInfo> spellRangePowerWordCache = new Dictionary<string, SpellRangeInfo>();

        private bool loaded = false;
        private static SpellVisualRangeManager instance;

        private readonly object _castingLock = new object();
        private bool isCasting { get; set; } = false;
        private SpellRangeInfo currentSpell { get; set; }
        private bool frozenBySpell = false;
        private System.Threading.CancellationTokenSource _castCts;
        private System.Threading.CancellationTokenSource _recoveryCts;

        //Taken from Dust client
        private static readonly int[] stopAtClilocs = new int[]
        {
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
        };

        private World World;

        private SpellVisualRangeManager()
        {
            this.World = Client.Game.UO.World;
            Load();
        }

        /// <summary>
        /// Reindexes the PowerWords mapping for the provided spell in the runtime cache.
        /// </summary>
        public void ReindexSpellPowerWords(SpellRangeInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.PowerWords))
                return;

            spellRangePowerWordCache[info.PowerWords] = info;
        }

        private void OnRawMessageReceived(object sender, MessageEventArgs e) => Task.Run(() =>
                                                                                         {
                                                                                             if (loaded && e.Parent != null && ReferenceEquals(e.Parent, World.Player))
                                                                                             {
                                                                                                 if (spellRangePowerWordCache.TryGetValue(e.Text.Trim(), out SpellRangeInfo spell))
                                                                                                 {
                                                                                                     SetCasting(spell);
                                                                                                 }
                                                                                             }
                                                                                         });

        public void OnClilocReceived(int cliloc) => Task.Factory.StartNew(() =>
                                                             {
                                                                 if (isCasting && stopAtClilocs.Contains(cliloc))
                                                                 {
                                                                     ClearCasting();
                                                                 }
                                                             });


        private void SetCasting(SpellRangeInfo spell)
        {
            lock (_castingLock)
            {
                LastSpellTime = DateTime.Now;
                currentSpell = spell;
                isCasting = true;
            }

            if (currentSpell != null && currentSpell.FreezeCharacterWhileCasting)
            {
                frozenBySpell = true;
                World.Player.Flags |= Flags.Frozen;
            }

            if (ProfileManager.CurrentProfile?.EnableSpellIndicators == true)
            {
                CastTimerProgressBar bar = UIManager.GetGump<CastTimerProgressBar>() ?? new CastTimerProgressBar(World);
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

                         if (isCasting && currentSpell == spell)
                         {
                             if (spell.ExpectTargetCursor && World.TargetManager.IsTargeting)
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
                if (frozenBySpell)
                    World.Player.Flags &= ~Flags.Frozen;
                frozenBySpell = false;
                spellSnapshot = currentSpell;
            }

            if (spellSnapshot == null)
            {
                isCasting = false;
                World.Player.Flags &= ~Flags.Frozen;
                return;
            }


            if (spellSnapshot.RecoveryTime > 0)
            {
                _ = StartRecovery(spellSnapshot);
            }
            else
            {
                EndRecovery(spellSnapshot);
            }
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
                CastTimerProgressBar bar = UIManager.GetGump<CastTimerProgressBar>() ?? new CastTimerProgressBar(World);
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
                endedSpellId = currentSpell?.ID ?? spell?.ID ?? 0;
                currentSpell = null;
                isCasting = false;
                if (frozenBySpell)
                {
                    World.Player.Flags &= ~Flags.Frozen;
                    frozenBySpell = false;
                }
                EventSink.InvokeSpellRecoveryEnd();
            }
        }

        public SpellRangeInfo GetCurrentSpell() => currentSpell;

        #region Load and unload
        public void OnSceneLoad() => EventSink.RawMessageReceived += OnRawMessageReceived;

        public void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;
            _castCts?.Cancel();
            _castCts?.Dispose();
            _recoveryCts?.Cancel();
            _recoveryCts?.Dispose();
            instance = null;
        }
        #endregion

        public bool IsTargetingAfterCasting()
        {
            if (!loaded || currentSpell == null || !isCasting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (World.TargetManager.IsTargeting || (currentSpell.ShowCastRangeDuringCasting && IsCastingWithoutTarget()))
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCastingWithoutTarget()
        {
            if (!loaded || currentSpell == null || !isCasting || currentSpell.CastTime <= 0 || World.TargetManager.IsTargeting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.CastTime) > DateTime.Now)
                {
                    return true;
                }
                else if (currentSpell.FreezeCharacterWhileCasting)
                {
                    World.Player.Flags &= ~Flags.Frozen;
                }
            }
            else if (currentSpell.FreezeCharacterWhileCasting)
            {
                World.Player.Flags &= ~Flags.Frozen;
            }

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!loaded || currentSpell == null) { return hue; }

            if (currentSpell.CastRange > 0 && o.Distance <= currentSpell.CastRange)
            {
                hue = currentSpell.Hue;
            }

            int cDistance = o.DistanceFrom(LastCursorTileLoc);

            if (currentSpell.CursorSize > 0 && cDistance < currentSpell.CursorSize)
            {
                if (currentSpell.IsLinear)
                {
                    if (GetDirection(new Vector2(World.Player.X, World.Player.Y), LastCursorTileLoc) == SpellDirection.EastWest)
                    { //X
                        if (o.Y == LastCursorTileLoc.Y)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                    else
                    { //Y
                        if (o.X == LastCursorTileLoc.X)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                }
                else
                {
                    hue = currentSpell.CursorHue;
                }
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
            {
                return SpellDirection.SouthNorth;
            }
            else if (rx >= 0)
            {
                return SpellDirection.EastWest;
            }
            else if (ry >= 0)
            {
                return SpellDirection.EastWest;
            }
            else
            {
                return SpellDirection.SouthNorth;
            }
        }

        #region Save and load
        private Timer saveTimer;
        private readonly object saveLock = new object();
        private volatile bool hasPendingChanges = false;
        private void Load()
        {
            spellRangeCache.Clear();
            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(savePath))
                {
                    //CreateAndLoadDataFile();
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
                    loaded = true;
                    Save();
                }
                else
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                        foreach (SpellRangeInfo entry in fileData)
                        {
                            spellRangeCache.Add(entry.ID, entry);
                        }
                        AfterLoad();
                        loaded = true;
                    }
                    catch
                    {
                        CreateAndLoadDataFile();
                        AfterLoad();
                        loaded = true;
                    }

                }
            });
        }

        private void LoadOverrides()
        {
            spellRangeOverrideCache.Clear();

            if (File.Exists(overridePath))
            {
                try
                {
                    string data = File.ReadAllText(overridePath);
                    SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                    foreach (SpellRangeInfo entry in fileData)
                    {
                        spellRangeOverrideCache.Add(entry.ID, entry);
                    }

                    foreach (SpellRangeInfo entry in spellRangeOverrideCache.Values)
                    {
                        if (string.IsNullOrEmpty(entry.PowerWords))
                        {
                            var spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                            if (spellD == SpellDefinition.EmptySpell)
                            {
                                SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                            }

                            if (spellD != SpellDefinition.EmptySpell)
                            {
                                entry.PowerWords = spellD.PowerWords;
                            }
                        }
                        if (!string.IsNullOrEmpty(entry.PowerWords))
                        {
                            if (spellRangePowerWordCache.ContainsKey(entry.PowerWords))
                            {
                                spellRangePowerWordCache[entry.PowerWords] = entry;
                            }
                            else
                            {
                                spellRangePowerWordCache.Add(entry.PowerWords, entry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public bool LoadFromString(string json)
        {
            try
            {
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize(json, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                loaded = false;
                spellRangeCache.Clear();

                foreach (SpellRangeInfo entry in fileData)
                {
                    spellRangeCache.Add(entry.ID, entry);
                }
                AfterLoad();
                LoadOverrides();
                loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                loaded = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private void AfterLoad()
        {
            spellRangePowerWordCache.Clear();
            foreach (SpellRangeInfo entry in spellRangeCache.Values)
            {
                if (string.IsNullOrEmpty(entry.PowerWords))
                {
                    var spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                    if (spellD == SpellDefinition.EmptySpell)
                    {
                        SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                    }

                    if (spellD != SpellDefinition.EmptySpell)
                    {
                        entry.PowerWords = spellD.PowerWords;
                    }
                }
                if (!string.IsNullOrEmpty(entry.PowerWords))
                {
                    spellRangePowerWordCache.Add(entry.PowerWords, entry);
                }
            }
            LoadOverrides();
        }

        private void CreateAndLoadDataFile()
        {
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMagery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNecromancy.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsChivalry.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsBushido.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNinjitsu.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsSpellweaving.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMysticism.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMastery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }

            Task.Factory.StartNew(() =>
            {
                Save();
            });
        }

        public void DelayedSave()
        {
            lock (saveLock)
            {
                hasPendingChanges = true;

                // Cancel existing timer if it's running
                saveTimer?.Dispose();

                saveTimer = new Timer();
                saveTimer.Interval = 500;
                saveTimer.Elapsed += (_, _) => { PerformSave(); };
                saveTimer.Start();
            }
        }

        private void PerformSave()
        {
            lock (saveLock)
            {
                if (!hasPendingChanges)
                    return;

                hasPendingChanges = false;
            }

            string tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                string fileData = JsonSerializer.Serialize(spellRangeCache.Values.ToArray(), SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);
                File.WriteAllText(tempPath, fileData);

                if (File.Exists(savePath))
                    File.Delete(savePath);
                File.Move(tempPath, savePath);
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
            lock (saveLock)
            {
                saveTimer?.Dispose();
                if (hasPendingChanges)
                {
                    PerformSave();
                }
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
