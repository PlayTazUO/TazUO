using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace ClassicUO.Game.Managers.SpellVisualRange
{
    using Utility.Logging;

    public class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance => instance ??= new SpellVisualRangeManager();

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;
        public DateTime LastSpellTime { get; private set; } = DateTime.Now;
        public Dictionary<int, SpellRangeInfo> SpellRangeCache => spellRangeCache;

        private SpellVisualRangeConfig config;
        private string overridePath = Path.Combine(ProfileManager.ProfilePath ?? "", "SpellVisualRange.json");

        private Dictionary<int, SpellRangeInfo> spellRangeCache = new();
        private Dictionary<int, SpellRangeInfo> spellRangeOverrideCache = new();
        private Dictionary<string, SpellRangeInfo> spellRangePowerWordCache = new();

        private bool loaded = false;
        private static SpellVisualRangeManager instance;

        private bool isCasting { get; set; } = false;
        private SpellRangeInfo currentSpell { get; set; }

        /// <summary>
        /// Monotonic tick of the last time the server reported a cast failure (a <see cref="stopAtClilocs"/>
        /// message: concentration disturbed, insufficient mana/reagents, frozen, etc.). Consumers can compare
        /// this against when they issued a cast to know it genuinely failed — vs. a benign casting-flag drop —
        /// and react immediately instead of waiting out a timeout.
        /// </summary>
        public long LastCastFailedTick { get; private set; }

        /// <summary>
        /// An instance of a cast timer bar. May be null, depending on profile settings
        /// </summary>
        private static CastTimerProgressBar _castTimerBar;

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
            World = Client.Game.UO.World;
            Load();
        }

        private void OnRawMessageReceived(object sender, MessageEventArgs e) =>
            Task.Run(() =>
            {
                if (loaded && e.Parent != null && ReferenceEquals(e.Parent, World.Player))
                    if (spellRangePowerWordCache.TryGetValue(e.Text.Trim(), out SpellRangeInfo spell))
                        SetCasting(spell);
            });

        public void OnClilocReceived(int cliloc) =>
            Task.Factory.StartNew(() =>
            {
                if (stopAtClilocs.Contains(cliloc))
                {
                    // Record the failure regardless of our isCasting flag: a damage packet may have
                    // already cleared isCasting before this disrupt cliloc arrives (packet ordering),
                    // and consumers still need to know the cast just failed.
                    LastCastFailedTick = ClassicUO.Time.Ticks;
                    if (isCasting) ClearCasting();
                }
            });

        private void SetCasting(SpellRangeInfo spell)
        {
            if (World?.Player == null) return;

            LastSpellTime = DateTime.Now;
            currentSpell = spell;
            isCasting = true;

            if (currentSpell != null && currentSpell.FreezeCharacterWhileCasting) World.Player.Flags |= Flags.Frozen;

            World.Player.IsCasting = true;
            EventSink.InvokeSpellCastBegin(spell.ID);
        }

        public void ClearCasting()
        {
            isCasting = false;
            currentSpell = null;
            LastSpellTime = DateTime.MinValue;

            if (World?.Player != null)
            {
                World.Player.Flags &= ~Flags.Frozen;
                World.Player.IsCasting = false;
            }

            EventSink.InvokeSpellCastEnd();
        }

        public SpellRangeInfo GetCurrentSpell() => currentSpell;

        #region Load and unload

        public void OnSceneLoad()
        {
            EventSink.RawMessageReceived += OnRawMessageReceived;

            // Register a settings listener so we can dynamically enable/disable the spell progress bar
            ProfileManager.CurrentProfilePropertyChanged += UpdateSpellProgressBarPresence;
            UpdateSpellProgressBarPresence(null, new PropertyChangedEventArgs(nameof(Profile.EnableSpellIndicators)));
        }

        public void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;

            // Remove settings listener. Leave gump destruction to UI Mgr. disposal.
            ProfileManager.CurrentProfilePropertyChanged -= UpdateSpellProgressBarPresence;

            instance = null;

        }
        #endregion

        /// <summary>
        /// Adds/removes the spell progress bar, depending on current configuration
        /// </summary>
        /// <param name="sender">The event's source; Effectivly always null</param>
        /// <param name="e">The event args. Used to ignore irrelevant updates</param>
        private void UpdateSpellProgressBarPresence(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Profile.EnableSpellIndicators))
                return;

            if (ProfileManager.CurrentProfile?.EnableSpellIndicators != true)
            {
                _castTimerBar?.Dispose();
                _castTimerBar = null;
                return;
            }

            if (_castTimerBar is { IsDisposed: false })
                return;

            _castTimerBar = new CastTimerProgressBar(World);
            UIManager.Add(_castTimerBar, false);
        }

        public bool IsTargetingAfterCasting()
        {
            if (!loaded || currentSpell == null || !isCasting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators) return false;

            if (World.TargetManager.IsTargeting || (currentSpell.ShowCastRangeDuringCasting && IsCastingWithoutTarget()))
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
                    return true;

            return false;
        }

        public bool IsCastingWithoutTarget()
        {
            if (!loaded || currentSpell == null || !isCasting || currentSpell.CastTime <= 0 || World.TargetManager.IsTargeting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators) return false;

            if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.CastTime) > DateTime.Now)
                    return true;
                else if (currentSpell.FreezeCharacterWhileCasting) World.Player.Flags &= ~Flags.Frozen;
            }
            else if (currentSpell.FreezeCharacterWhileCasting) World.Player.Flags &= ~Flags.Frozen;

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!loaded || currentSpell == null) return hue;

            if (currentSpell.CastRange > 0 && o.Distance <= currentSpell.CastRange) hue = currentSpell.Hue;

            int cDistance = o.DistanceFrom(LastCursorTileLoc);

            if (currentSpell.CursorSize > 0 && cDistance < currentSpell.CursorSize)
            {
                if (currentSpell.IsLinear)
                {
                    if (GetDirection(new Vector2(World.Player.X, World.Player.Y), LastCursorTileLoc) == SpellDirection.EastWest)
                    { //X
                        if (o.Y == LastCursorTileLoc.Y) hue = currentSpell.CursorHue;
                    }
                    else
                    { //Y
                        if (o.X == LastCursorTileLoc.X) hue = currentSpell.CursorHue;
                    }
                }
                else
                    hue = currentSpell.CursorHue;
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
            else if (rx >= 0)
                return SpellDirection.EastWest;
            else if (ry >= 0)
                return SpellDirection.EastWest;
            else
                return SpellDirection.SouthNorth;
        }

        #region Save and load
        private Timer saveTimer;
        private readonly object saveLock = new();
        private volatile bool hasPendingChanges = false;
        private void Load()
        {
            spellRangeCache.Clear();
            Task.Factory.StartNew(() =>
            {
                config = SpellVisualRangeConfig.Load();

                if (config.Spells.Count > 0)
                {
                    foreach (SpellRangeInfo entry in config.Spells) spellRangeCache[entry.ID] = entry;
                    AfterLoad();
                    loaded = true;
                    return;
                }

                // No saved config yet - seed from the legacy array file, then the embedded defaults, and persist.
                if (!TryLoadLegacyFile() && !TryLoadEmbeddedDefaults())
                    CreateAndLoadDataFile();

                AfterLoad();
                loaded = true;
                PersistCache();
            });
        }

        /// <summary>Migrates the old bare-array save file (Data/Profiles/SpellVisualRange.json) into the cache.</summary>
        private bool TryLoadLegacyFile()
        {
            string legacyPath = Path.Combine(CUOEnviroment.ExecutablePath ?? "", "Data", "Profiles", "SpellVisualRange.json");

            if (!File.Exists(legacyPath))
                return false;

            try
            {
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize(File.ReadAllText(legacyPath), SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                if (fileData == null || fileData.Length == 0)
                    return false;

                foreach (SpellRangeInfo entry in fileData) spellRangeCache[entry.ID] = entry;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }
        }

        /// <summary>Seeds the cache from the embedded default indicator config.</summary>
        private bool TryLoadEmbeddedDefaults()
        {
            try
            {
                Assembly assembly = GetType().Assembly;
                using Stream stream = assembly.GetManifestResourceStream("ClassicUO.Game.Managers.DefaultSpellIndicatorConfig.json");
                using var reader = new StreamReader(stream);

                SpellRangeInfo[] fileData = JsonSerializer.Deserialize(reader.ReadToEnd(), SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                if (fileData == null || fileData.Length == 0)
                    return false;

                foreach (SpellRangeInfo entry in fileData) spellRangeCache[entry.ID] = entry;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }
        }

        private void LoadOverrides()
        {
            spellRangeOverrideCache.Clear();

            if (File.Exists(overridePath))
                try
                {
                    string data = File.ReadAllText(overridePath);
                    SpellRangeInfo[] fileData = JsonSerializer.Deserialize(data, SpellRangeInfoJsonContext.Default.SpellRangeInfoArray);

                    foreach (SpellRangeInfo entry in fileData) spellRangeOverrideCache.Add(entry.ID, entry);

                    foreach (SpellRangeInfo entry in spellRangeOverrideCache.Values)
                    {
                        if (string.IsNullOrEmpty(entry.PowerWords))
                        {
                            var spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                            if (spellD == SpellDefinition.EmptySpell) SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);

                            if (spellD != SpellDefinition.EmptySpell) entry.PowerWords = spellD.PowerWords;
                        }
                        if (!string.IsNullOrEmpty(entry.PowerWords))
                        {
                            if (spellRangePowerWordCache.ContainsKey(entry.PowerWords))
                                spellRangePowerWordCache[entry.PowerWords] = entry;
                            else
                                spellRangePowerWordCache.Add(entry.PowerWords, entry);
                        }
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

                loaded = false;
                spellRangeCache.Clear();

                foreach (SpellRangeInfo entry in fileData) spellRangeCache.Add(entry.ID, entry);
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

                    if (spellD is null) continue;

                    if (spellD == SpellDefinition.EmptySpell) SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);

                    if (spellD != null &&  spellD != SpellDefinition.EmptySpell) entry.PowerWords = spellD.PowerWords;
                }
                if (!string.IsNullOrEmpty(entry.PowerWords)) spellRangePowerWordCache.Add(entry.PowerWords, entry);
            }
            LoadOverrides();
        }

        private void CreateAndLoadDataFile()
        {
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMagery.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNecromancy.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsChivalry.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsBushido.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsNinjitsu.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsSpellweaving.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMysticism.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            foreach (KeyValuePair<int, SpellDefinition> entry in SpellsMastery.GetAllSpells) spellRangeCache.TryAdd(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
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
                saveTimer.Elapsed += (_,_) => { PerformSave(); };
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

            PersistCache();
        }

        /// <summary>Writes the current cache to disk via the JsonSave base class (atomic + rotating backups).</summary>
        private void PersistCache()
        {
            config ??= new SpellVisualRangeConfig();
            config.Spells = spellRangeCache.Values.ToList();
            config.Save();
        }

        public void Save()
        {
            lock (saveLock)
            {
                saveTimer?.Dispose();
                if (hasPendingChanges) PerformSave();
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
