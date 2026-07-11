using System;
using System.IO;
using System.Text.Json;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    using System.Text.Json.Serialization;

    [JsonSerializable(typeof(Language))]
    public partial class LanguageJsonContext : JsonSerializerContext
    {
    }

    [Obsolete("This system is being replaced with TazLang and language.ini, please use that instead.")]
    public class Language
    {
        public ModernOptionsGumpLanguage GetModernOptionsGumpLanguage { get; set; } = new();

        public string TazuoVersionHistory { get; set; } = "TazUO Version History";
        public string CurrentVersion { get; set; } = "Current Version: ";
        public string TazUOWiki { get; set; } = "TazUO Wiki";
        public string TazUODiscord { get; set; } = "TazUO Discord";
        public string CommandGump { get; set; } = "Available Client Commands";

        private static string LanguageFilePath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Language.json");

        [JsonIgnore]
        public static Language Instance { get; private set; } = new();

        public static void Load() => Load(false);

        private static void Load(bool isRegenerating)
        {
            if (!File.Exists(LanguageFilePath))
            {
                CreateNewLanguageFile();
                return;
            }

            try
            {
                Language f = JsonSerializer.Deserialize(File.ReadAllText(LanguageFilePath), LanguageJsonContext.Default.Language);
                Instance = f;
                Save(); //To update language file with new additions as needed
            }
            catch (Exception e)
            {
                if (isRegenerating)
                {
                    Log.Error($"Failed to load a regenerated language file {LanguageFilePath}. This is a fatal error. Exception message: {e.Message}");
                    throw;
                }

                Log.Error($"Error loading language file: {e.Message}. Will attempt to re-generate and reload");
                RegenerateLanguageFile();
            }
        }

        public static void RegenerateLanguageFile()
        {
            if (File.Exists(LanguageFilePath))
                File.Delete(LanguageFilePath);

            CreateNewLanguageFile();
            Load(true);
        }

        private static void CreateNewLanguageFile()
        {
            Directory.CreateDirectory(Path.Combine(CUOEnviroment.ExecutablePath, "Data"));
            string defaultLanguage = JsonSerializer.Serialize(Instance, LanguageJsonContext.Default.Language);
            File.WriteAllText(LanguageFilePath, defaultLanguage);
        }

        private static void Save()
        {
            string language = JsonSerializer.Serialize(Instance, LanguageJsonContext.Default.Language);
            File.WriteAllText(LanguageFilePath, language);
        }
    }

    [Obsolete("This system is being replaced with TazLang and language.ini, please use that instead.")]
    public class ModernOptionsGumpLanguage
    {
        public string OptionsTitle { get; set; } = "Options";
        public string Search { get; set; } = "Search";
        public string SearchEllipses { get; set; } = "Search...";
        public string Apply { get; set; } = "Apply";
        public string ButtonGeneral { get; set; } = "General";
        public string ButtonSound { get; set; } = "Sound";
        public string ButtonVideo { get; set; } = "Video";
        public string ButtonMacros { get; set; } = "Macros";
        public string ButtonTooltips { get; set; } = "Tooltips";
        public string ButtonSpeech { get; set; } = "Speech";
        public string ButtonCombatSpells { get; set; } = "Combat & Spells";
        public string ButtonCounters { get; set; } = "Counters";
        public string ButtonInfoBar { get; set; } = "Info Bar";
        public string ButtonContainers { get; set; } = "Containers";
        public string ButtonExperimental { get; set; } = "Experimental";
        public string ButtonIgnoreList { get; set; } = "Ignore List";
        public string ButtonNameplates { get; set; } = "Nameplates";
        public string ButtonProfiles { get; set; } = "Profiles";
        public string ButtonCooldowns { get; set; } = "Cooldown bars";
        public string ButtonTazUO { get; set; } = "TazUO Specific";
        public string ButtonMobiles { get; set; } = "Mobiles";
        public string ButtonGumpContext { get; set; } = "Gumps & Context";
        public string ButtonMisc { get; set; } = "Misc";
        public string ButtonTerrainStatics { get; set; } = "Terrain & Statics";
        public string ButtonHealthBars { get; set; } = "Health Bars";
        public string ButtonGumps { get; set; } = "Gumps";
        public string ButtonGameWindow { get; set; } = "Game window";
        public string ButtonGameplay { get; set; } = "Gameplay";
        public string ButtonPaperdoll { get; set; } = "Paperdoll";
        public string ButtonZoom { get; set; } = "Zoom";
        public string ButtonLighting { get; set; } = "Lighting";
        public string ButtonShadows { get; set; } = "Shadows";
        public string ButtonScaling { get; set; } = "Scaling";

        public string LabelVideo { get; set; } = "Video";
        public string LabelViewport { get; set; } = "Viewport";
        public string LabelSpells { get; set; } = "Spells";
        public string LabelSpeech { get; set; } = "Speech";
        public string LabelChatAndText { get; set; } = "Chat & Text";
        public string LabelHue { get; set; } = "Hue";
        public string LabelTooltips { get; set; } = "Tooltips";
        public string LabelCounters { get; set; } = "Counters";
        public string LabelContainers { get; set; } = "Containers";

        public General GetGeneral { get; set; } = new();
        public Video GetVideo { get; set; } = new();
        public Sound GetSound { get; set; } = new();
        public Macros GetMacros { get; set; } = new();
        public ToolTips GetToolTips { get; set; } = new();
        public Speech GetSpeech { get; set; } = new();
        public CombatSpells GetCombatSpells { get; set; } = new();
        public Counters GetCounters { get; set; } = new();
        public InfoBars GetInfoBars { get; set; } = new();
        public Containers GetContainers { get; set; } = new();
        public Experimental GetExperimental { get; set; } = new();
        public NamePlates GetNamePlates { get; set; } = new();
        public Cooldowns GetCooldowns { get; set; } = new();
        public TazUO GetTazUO { get; set; } = new();

        public MobilesTabLang MobilesTab { get; set; } = new();

        public MovementTabLang MovementTab { get; set; } = new();

        public MiscTabLang MiscTab { get; set; } = new();

        public VideoTabLang VideoTab { get; set; } = new();

        public SoundTabLang SoundTab { get; set; } = new();

        public ChatTabLang ChatTab { get; set; } = new();

        public CooldownsTabLang CooldownsTab { get; set; } = new();

        public GumpsTabLang GumpsTab { get; set; } = new();

        public LayerHidingTabLang LayerHidingTab { get; set; } = new();

        public SpellsTabLang SpellsTab { get; set; } = new();

        public CombatTabLang CombatTab { get; set; } = new();

        public GameplayTabLang GameplayTab { get; set; } = new();

        public KeywordsLang Kw { get; set; } = new();

        public class GameplayTabLang
        {
            public string GameplayLabel { get; set; } = "Gameplay";

            public TerrainSection Terrain { get; set; } = new();

            public class TerrainSection
            {
                public string Label { get; set; } = "Terrain & Statics";

                public string HideRoof { get; set; } = "Hide roof";
                public string TreesToStump { get; set; } = "Vegetation to stumps";
                public string HideVegetation { get; set; } = "Hide vegetation";
                public string MagicFieldType { get; set; } = "Magic field type";
                public string ApplyBorderCaveTiles { get; set; } = "Apply border to cave tiles";
            }
        }

        public class KeywordsLang
        {
            public string Abbreviate { get; set; } = "Abbreviate";
            public string Advanced { get; set; } = "Advanced";
            public string Align { get; set; } = "Align";
            public string Ally { get; set; } = "Ally";
            public string Alpha { get; set; } = "Alpha";
            public string Alt { get; set; } = "Alt";
            public string AltScroll { get; set; } = "Alt Scroll";
            public string Amount { get; set; } = "Amount";
            public string Anchor { get; set; } = "Anchor";
            public string Anim { get; set; } = "Anim";
            public string Animation { get; set; } = "Animation";
            public string Appearance { get; set; } = "Appearance";
            public string Arrow { get; set; } = "Arrow";
            public string Assign { get; set; } = "Assign";
            public string Attack { get; set; } = "Attack";
            public string Audio { get; set; } = "Audio";
            public string Aura { get; set; } = "Aura";
            public string Auto { get; set; } = "Auto";
            public string AutoMove { get; set; } = "AutoMove";
            public string Avoid { get; set; } = "Avoid";
            public string Bw { get; set; } = "BW";
            public string Background { get; set; } = "Background";
            public string Bar { get; set; } = "Bar";
            public string Battle { get; set; } = "Battle";
            public string Below { get; set; } = "Below";
            public string Beneficial { get; set; } = "Beneficial";
            public string Beta { get; set; } = "Beta";
            public string Black { get; set; } = "Black";
            public string Blue { get; set; } = "Blue";
            public string Boat { get; set; } = "Boat";
            public string Border { get; set; } = "Border";
            public string Borderless { get; set; } = "Borderless";
            public string Buff { get; set; } = "Buff";
            public string BuffBar { get; set; } = "Buff Bar";
            public string BuffGump { get; set; } = "Buff Gump";
            public string Cot { get; set; } = "COT";
            public string Cast { get; set; } = "Cast";
            public string Cave { get; set; } = "Cave";
            public string Center { get; set; } = "Center";
            public string Changed { get; set; } = "Changed";
            public string Character { get; set; } = "Character";
            public string Chat { get; set; } = "Chat";
            public string Circle { get; set; } = "Circle";
            public string Clear { get; set; } = "Clear";
            public string Click { get; set; } = "Click";
            public string Close { get; set; } = "Close";
            public string Clothing { get; set; } = "Clothing";
            public string Color { get; set; } = "Color";
            public string Colour { get; set; } = "Colour";
            public string Column { get; set; } = "Column";
            public string Combat { get; set; } = "Combat";
            public string Condition { get; set; } = "Condition";
            public string Config { get; set; } = "Config";
            public string Container { get; set; } = "Container";
            public string Content { get; set; } = "Content";
            public string Context { get; set; } = "Context";
            public string Contrast { get; set; } = "Contrast";
            public string Controller { get; set; } = "Controller";
            public string Cooldown { get; set; } = "Cooldown";
            public string Corner { get; set; } = "Corner";
            public string Corpse { get; set; } = "Corpse";
            public string Counter { get; set; } = "Counter";
            public string Criminal { get; set; } = "Criminal";
            public string Ctrl { get; set; } = "Ctrl";
            public string Cursor { get; set; } = "Cursor";
            public string Custom { get; set; } = "Custom";
            public string Dps { get; set; } = "DPS";
            public string Damage { get; set; } = "Damage";
            public string Dark { get; set; } = "Dark";
            public string Darkness { get; set; } = "Darkness";
            public string Dead { get; set; } = "Dead";
            public string Death { get; set; } = "Death";
            public string Delay { get; set; } = "Delay";
            public string Disable { get; set; } = "Disable";
            public string Display { get; set; } = "Display";
            public string Dismount { get; set; } = "Dismount";
            public string Distance { get; set; } = "Distance";
            public string Door { get; set; } = "Door";
            public string Double { get; set; } = "Double";
            public string Download { get; set; } = "Download";
            public string Drag { get; set; } = "Drag";
            public string Drop { get; set; } = "Drop";
            public string Durability { get; set; } = "Durability";
            public string Duration { get; set; } = "Duration";
            public string Empty { get; set; } = "Empty";
            public string Enable { get; set; } = "Enable";
            public string Enemy { get; set; } = "Enemy";
            public string Enhanced { get; set; } = "Enhanced";
            public string Entity { get; set; } = "Entity";
            public string Equipment { get; set; } = "Equipment";
            public string Exceed { get; set; } = "Exceed";
            public string Experimental { get; set; } = "Experimental";
            public string Fps { get; set; } = "FPS";
            public string Fade { get; set; } = "Fade";
            public string Field { get; set; } = "Field";
            public string Fixed { get; set; } = "Fixed";
            public string Flying { get; set; } = "Flying";
            public string Font { get; set; } = "Font";
            public string Footstep { get; set; } = "Footstep";
            public string Follow { get; set; } = "Follow";
            public string Force { get; set; } = "Force";
            public string Format { get; set; } = "Format";
            public string Friend { get; set; } = "Friend";
            public string Full { get; set; } = "Full";
            public string Fullscreen { get; set; } = "Fullscreen";
            public string Game { get; set; } = "Game";
            public string Gamepad { get; set; } = "Gamepad";
            public string Gameplay { get; set; } = "Gameplay";
            public string Gargoyle { get; set; } = "Gargoyle";
            public string General { get; set; } = "General";
            public string Global { get; set; } = "Global";
            public string Grab { get; set; } = "Grab";
            public string Graphic { get; set; } = "Graphic";
            public string Green { get; set; } = "Green";
            public string Grid { get; set; } = "Grid";
            public string Ground { get; set; } = "Ground";
            public string Group { get; set; } = "Group";
            public string Gump { get; set; } = "Gump";
            public string HP { get; set; } = "HP";
            public string Hang { get; set; } = "Hang";
            public string Harmful { get; set; } = "Harmful";
            public string Health { get; set; } = "Health";
            public string HealthBar { get; set; } = "Health Bar";
            public string Height { get; set; } = "Height";
            public string Help { get; set; } = "Help";
            public string Hidden { get; set; } = "Hidden";
            public string Hide { get; set; } = "Hide";
            public string Highlight { get; set; } = "Highlight";
            public string History { get; set; } = "History";
            public string Hotkey { get; set; } = "Hotkey";
            public string House { get; set; } = "House";
            public string Hover { get; set; } = "Hover";
            public string Hue { get; set; } = "Hue";
            public string Humanoid { get; set; } = "Humanoid";
            public string Icon { get; set; } = "Icon";
            public string Ignore { get; set; } = "Ignore";
            public string Import { get; set; } = "Import";
            public string Improved { get; set; } = "Improved";
            public string Incoming { get; set; } = "Incoming";
            public string Indicator { get; set; } = "Indicator";
            public string InfoBar { get; set; } = "InfoBar";
            public string InfoBarSpaced { get; set; } = "Info Bar";
            public string Innocent { get; set; } = "Innocent";
            public string Interface { get; set; } = "Interface";
            public string Invite { get; set; } = "Invite";
            public string Invulnerable { get; set; } = "Invulnerable";
            public string Item { get; set; } = "Item";
            public string Journal { get; set; } = "Journal";
            public string Joystick { get; set; } = "Joystick";
            public string Keyboard { get; set; } = "Keyboard";
            public string Last { get; set; } = "Last";
            public string Layer { get; set; } = "Layer";
            public string Left { get; set; } = "Left";
            public string Level { get; set; } = "Level";
            public string Light { get; set; } = "Light";
            public string Location { get; set; } = "Location";
            public string Lock { get; set; } = "Lock";
            public string Log { get; set; } = "Log";
            public string Login { get; set; } = "Login";
            public string Loot { get; set; } = "Loot";
            public string Low { get; set; } = "Low";
            public string Magic { get; set; } = "Magic";
            public string Main { get; set; } = "Main";
            public string Managed { get; set; } = "Managed";
            public string Mechanics { get; set; } = "Mechanics";
            public string Message { get; set; } = "Message";
            public string Misc { get; set; } = "Misc";
            public string Miscellaneous { get; set; } = "Miscellaneous";
            public string Mobile { get; set; } = "Mobile";
            public string Mode { get; set; } = "Mode";
            public string Model { get; set; } = "Model";
            public string Modern { get; set; } = "Modern";
            public string Modifier { get; set; } = "Modifier";
            public string Monster { get; set; } = "Monster";
            public string Mouse { get; set; } = "Mouse";
            public string Move { get; set; } = "Move";
            public string Moved { get; set; } = "Moved";
            public string Movement { get; set; } = "Movement";
            public string Murderer { get; set; } = "Murderer";
            public string Music { get; set; } = "Music";
            public string Name { get; set; } = "Name";
            public string Nameplate { get; set; } = "Nameplate";
            public string Neutral { get; set; } = "Neutral";
            public string Night { get; set; } = "Night";
            public string Notoriety { get; set; } = "Notoriety";
            public string Object { get; set; } = "Object";
            public string Obstacle { get; set; } = "Obstacle";
            public string Old { get; set; } = "Old";
            public string Opacity { get; set; } = "Opacity";
            public string Options { get; set; } = "Options";
            public string Original { get; set; } = "Original";
            public string Other { get; set; } = "Other";
            public string Over { get; set; } = "Over";
            public string Overhead { get; set; } = "Overhead";
            public string Overlap { get; set; } = "Overlap";
            public string Override { get; set; } = "Override";
            public string Paperdoll { get; set; } = "Paperdoll";
            public string Paralyze { get; set; } = "Paralyze";
            public string Party { get; set; } = "Party";
            public string Pathfinding { get; set; } = "Pathfinding";
            public string Pet { get; set; } = "Pet";
            public string Perspective { get; set; } = "Perspective";
            public string Player { get; set; } = "Player";
            public string Poison { get; set; } = "Poison";
            public string Position { get; set; } = "Position";
            public string Post { get; set; } = "Post";
            public string PostProcessing { get; set; } = "Post Processing";
            public string Preset { get; set; } = "Preset";
            public string Preview { get; set; } = "Preview";
            public string Process { get; set; } = "Process";
            public string Profile { get; set; } = "Profile";
            public string Progress { get; set; } = "Progress";
            public string Property { get; set; } = "Property";
            public string Query { get; set; } = "Query";
            public string Radius { get; set; } = "Radius";
            public string Rain { get; set; } = "Rain";
            public string Range { get; set; } = "Range";
            public string Reagent { get; set; } = "Reagent";
            public string Recolor { get; set; } = "Recolor";
            public string Red { get; set; } = "Red";
            public string Refresh { get; set; } = "Refresh";
            public string Relative { get; set; } = "Relative";
            public string Resolution { get; set; } = "Resolution";
            public string Restore { get; set; } = "Restore";
            public string Resync { get; set; } = "Resync";
            public string Right { get; set; } = "Right";
            public string RightClick { get; set; } = "Right Click";
            public string Ripple { get; set; } = "Ripple";
            public string Rock { get; set; } = "Rock";
            public string Roof { get; set; } = "Roof";
            public string Row { get; set; } = "Row";
            public string Rule { get; set; } = "Rule";
            public string Run { get; set; } = "Run";
            public string SOS { get; set; } = "SOS";
            public string Sallos { get; set; } = "Sallos";
            public string Save { get; set; } = "Save";
            public string Scale { get; set; } = "Scale";
            public string Scaling { get; set; } = "Scaling";
            public string Screenshot { get; set; } = "Screenshot";
            public string Scroll { get; set; } = "Scroll";
            public string Search { get; set; } = "Search";
            public string Select { get; set; } = "Select";
            public string Self { get; set; } = "Self";
            public string Sensitivity { get; set; } = "Sensitivity";
            public string Setting { get; set; } = "Setting";
            public string Shadow { get; set; } = "Shadow";
            public string Shift { get; set; } = "Shift";
            public string Shop { get; set; } = "Shop";
            public string Show { get; set; } = "Show";
            public string Size { get; set; } = "Size";
            public string Skill { get; set; } = "Skill";
            public string Skills { get; set; } = "Skills";
            public string Smooth { get; set; } = "Smooth";
            public string Snow { get; set; } = "Snow";
            public string Sound { get; set; } = "Sound";
            public string Speech { get; set; } = "Speech";
            public string Spell { get; set; } = "Spell";
            public string Splash { get; set; } = "Splash";
            public string Split { get; set; } = "Split";
            public string Stamina { get; set; } = "Stamina";
            public string Standard { get; set; } = "Standard";
            public string Stat { get; set; } = "Stat";
            public string Static { get; set; } = "Static";
            public string Stats { get; set; } = "Stats";
            public string Status { get; set; } = "Status";
            public string Stump { get; set; } = "Stump";
            public string Style { get; set; } = "Style";
            public string Tab { get; set; } = "Tab";
            public string Talk { get; set; } = "Talk";
            public string Target { get; set; } = "Target";
            public string Targeting { get; set; } = "Targeting";
            public string Terrain { get; set; } = "Terrain";
            public string Test { get; set; } = "Test";
            public string Text { get; set; } = "Text";
            public string Thread { get; set; } = "Thread";
            public string Time { get; set; } = "Time";
            public string Timer { get; set; } = "Timer";
            public string Tooltip { get; set; } = "Tooltip";
            public string Transparency { get; set; } = "Transparency";
            public string Tree { get; set; } = "Tree";
            public string Turn { get; set; } = "Turn";
            public string Type { get; set; } = "Type";
            public string Use { get; set; } = "Use";
            public string VSync { get; set; } = "VSync";
            public string Vegetation { get; set; } = "Vegetation";
            public string Vendor { get; set; } = "Vendor";
            public string Video { get; set; } = "Video";
            public string View { get; set; } = "View";
            public string Viewport { get; set; } = "Viewport";
            public string Voice { get; set; } = "Voice";
            public string Volume { get; set; } = "Volume";
            public string WASD { get; set; } = "WASD";
            public string War { get; set; } = "War";
            public string Warmode { get; set; } = "Warmode";
            public string Water { get; set; } = "Water";
            public string Weather { get; set; } = "Weather";
            public string Wheel { get; set; } = "Wheel";
            public string Width { get; set; } = "Width";
            public string Wiki { get; set; } = "Wiki";
            public string Window { get; set; } = "Window";
            public string X { get; set; } = "X";
            public string Y { get; set; } = "Y";
            public string Zlib { get; set; } = "Zlib";
            public string Zoom { get; set; } = "Zoom";
        }

        public class MovementTabLang
        {
            public string Label { get; set; } = "Movement";

            public PathfindingSection Pathfinding { get; set; } = new();
            public RunningSection Running { get; set; } = new();
            public DoorsSection Doors { get; set; } = new();
            public ControllerSection Controller { get; set; } = new();

            public string AutoAvoidObstacles { get; set; } = "Auto Avoid Obstacles";
            public string AutoFollow { get; set; } = "Auto follow";
            public string UseWasdMovement { get; set; } = "Use WASD movement instead of arrow keys";

            public class PathfindingSection
            {
                public string EnablePathfinding { get; set; } = "Enable pathfinding";
                public string ShiftPathfinding { get; set; } = "Use shift for pathfinding";
                public string SingleClickPathfind { get; set; } = "Single click for pathfinding";
            }

            public class RunningSection
            {
                public string AlwaysRun { get; set; } = "Always run";
                public string RunUnlessHidden { get; set; } = "Unless hidden";
            }

            public class DoorsSection
            {
                public string AutoOpenDoors { get; set; } = "Automatically open doors";
                public string AutoOpenPathfinding { get; set; } = "While pathfinding";
                public string AutoOpenHidden { get; set; } = "While hidden";
            }

            public class ControllerSection
            {
                public string Label { get; set; } = "Controller support";
                public string EnableController { get; set; } = "Enable controller input";
                public string MouseSensitivity { get; set; } = "Mouse Sensitivity";
            }
        }

        public class MobilesTabLang
        {
            public string Label { get; set; } = "Mobiles";

            public HighlightingSection Highlighting { get; set; } = new();
            public HuesSection Hues { get; set; } = new();

            public class HighlightingSection
            {
                public string Label { get; set; } = "Highlighting";
                public string ShowMobileHP { get; set; } = "Show mobile's HP";
                public string MobileHPType { get; set; } = "Type";
                public string HPShowWhen { get; set; } = "Show when";
                public string HighlightPoisoned { get; set; } = "Highlight poisoned mobiles";
                public string HighlightPara { get; set; } = "Highlight paralyzed mobiles";
                public string HighlightInvul { get; set; } = "Highlight invulnerable mobiles";
                public string IncomingMobiles { get; set; } = "Show incoming mobile names";
                public string IncomingCorpses { get; set; } = "Show incoming corpse names";
                public string AuraUnderFeet { get; set; } = "Show aura under feet";
                public string AuraForParty { get; set; } = "Use a custom color for party members";
            }

            public class HuesSection
            {
                public string Label { get; set; } = "Hues";
                public string HueMobileByNotoriety { get; set; } = "Hue mobiles by notoriety";
                public string DamageHuesTooltip { get; set; } = "Select hues for different damage types";
                public string PlayerVisibility { get; set; } = "Player Visibility";
            }
        }

        public class MiscTabLang
        {
            public string Label { get; set; } = "Misc";

            public string GeneralLabel { get; set; } = "General";
            public string InteractionLabel { get; set; } = "Interaction";
            public string AdvancedLabel { get; set; } = "Advanced";
            public string LabelScreenshot { get; set; } = "Screenshot";

            public class ExperimentalSection
            {
                public string Label { get; set; } = "Experimental";
                public string DisableDefaultUoHotkeys { get; set; } = "Disable default UO hotkeys";
                public string DisableArrowsNumlockArrowsPlayerMovement { get; set; } = "Disable arrows & numlock arrows(player movement)";
                public string DisableTabToggleWarmode { get; set; } = "Disable tab (toggle warmode)";
                public string DisableCtrlQWMessageHistory { get; set; } = "Disable Ctrl+Q/W message history";
                public string DisableRightLeftClickAutoMove { get; set; } = "Disable Right+Left click auto-move";
            }

            public ExperimentalSection Experimental { get; set; } = new();

            public string ManageIgnoreListButtonLabel { get; set; } = "Manage Entity Ignore List";
            public string SosGumpId { get; set; } = "SOS Gump ID";
            public string SosGumpIdLabelTooltip { get; set; } = "Responsible for determining whether a gump is an S.O.S and displaying it correctly";
            public string EnableAutoResyncOnHangDetection { get; set; } = "Enable Auto-Resync hang detection";
            public string EnableAutoResyncOnHangDetectionTooltip { get; set; } = "Automatically send a RESYNC packet if server did not send a PING in the last 5 seconds";
            public string EnableASyncMapLoading { get; set; } = "Enable asynchronous map loading";
            public string UseManagedZlib { get; set; } = "Use a managed zLib";
            public string UseManagedZlibTooltip { get; set; } = "Improve UNIX/Linux compatibility at the expense of slightly lesser performance";
            public string HousingTransparency { get; set; } = "House Transparency";
            public string EnableHouseTransparency { get; set; } = "Enable house transparency";
            public string DisplayProgressBarOnSkillChanges { get; set; } = "Display progress bar on skill changes";
            public string SkillProgressBarFormatTooltip { get; set; } = "The text format with which to display the progress bar.\nUse {0} for skill name, {1} for the current value and {2} for the cap";
        }

        public class VideoTabLang
        {
            public string Label { get; set; } = "Video";

            public GameWindowSection GameWindow { get; set; } = new();
            public ZoomSection Zoom { get; set; } = new();
            public LightingSection Lighting { get; set; } = new();
            public ShadowsSection Shadows { get; set; } = new();
            public MiscSection Misc { get; set; } = new();

            public class GameWindowSection
            {
                public string Label { get; set; } = "Game window";

                public string RendererLabel { get; set; } = "Renderer";
                public string FPSCap { get; set; } = "FPS Cap";
                public string BackgroundFPS { get; set; } = "Reduce FPS when inactive";
                public string EnableVSync { get; set; } = "Enable VSync";

                public string ViewportLabel { get; set; } = "Viewport";
                public string FullsizeViewport { get; set; } = "Fullsize viewport";
                public string FullScreen { get; set; } = "Fullscreen (borderless)";
                public string LockViewport { get; set; } = "Lock viewport";
                public string ViewportX { get; set; } = "Viewport X";
                public string ViewportY { get; set; } = "Viewport Y";
                public string ViewportW { get; set; } = "Viewport Width";
                public string ViewportH { get; set; } = "Viewport Height";
            }

            public class ZoomSection
            {
                public string Label { get; set; } = "Zoom & Scaling";

                public string ZoomLabel { get; set; } = "Zoom";
                public string DefaultZoom { get; set; } = "Default zoom";
                public string ZoomWheel { get; set; } = "Enable mousewheel zoom";
                public string ReturnDefaultZoom { get; set; } = "Return to default zoom after unpressing Ctrl";

                public string ScalingLabel { get; set; } = "Scaling";
                public string PaperdollScaling { get; set; } = "Paperdoll scaling";
                public string GlobalScaling { get; set; } = "Global scaling";
            }

            public class LightingSection
            {
                public string Label { get; set; } = "Lighting";

                public string AltLights { get; set; } = "Alternative lights";
                public string CustomLLevel { get; set; } = "Custom light level";
                public string Level { get; set; } = "Level";
                public string LightType { get; set; } = "Light type";
                public string LightType_Absolute { get; set; } = "Absolute";
                public string LightType_Minimum { get; set; } = "Minimum";
                public string DarkNight { get; set; } = "Dark nights";
                public string ColoredLight { get; set; } = "Colored lights";
            }

            public class ShadowsSection
            {
                public string Label { get; set; } = "Shadows";

                public string EnableShadows { get; set; } = "Enable shadows";
                public string RockTreeShadows { get; set; } = "Rock and tree shadows";
                public string TerrainShadowLevel { get; set; } = "Terrain shadow level";
            }

            public class MiscSection
            {
                public string Label { get; set; } = "Misc";

                public string EnableDeathScreen { get; set; } = "Enable death screen";
                public string BWDead { get; set; } = "Black and white when dead";
                public string MouseThread { get; set; } = "Run mouse in a separate thread";
                public string TargetAura { get; set; } = "Target aura on mouse";
                public string AnimWater { get; set; } = "Animated water";
                public string EnablePostProcessing { get; set; } = "Enable post-processing effects";
                public string PostProcessingEffectType { get; set; } = "Effect type";
                public string Perspective { get; set; } = "Perspective";
                public string PlayerPositionOffsetX { get; set; } = "Player position offset X";
                public string PlayerPositionOffsetY { get; set; } = "Player position offset Y";
            }
        }

        public class SoundTabLang
        {
            public string Label { get; set; } = "Sound";
            public string VoiceToText { get; set; } = "Voice to text";
            public string CreateVoiceButton { get; set; } = "Create voice toggle button";
        }

        public class ChatTabLang
        {
            public string ChatAndText { get; set; } = "Chat & Text";

            public SpeechSection Speech { get; set; } = new();
            public JournalSection Journal { get; set; } = new();
            public FontTabLang FontTab { get; set; } = new();

            public class SpeechSection
            {
                public string Label { get; set; } = "Speech";

                public string ChatGradient { get; set; } = "Hide chat gradient";
                public string HideGuildChat { get; set; } = "Hide guild chat";
                public string HideAllianceChat { get; set; } = "Hide alliance chat";
                public string DisableSystemChat { get; set; } = "Disable system chat";

                public string DelaySection { get; set; } = "Delay";
                public string ScaleSpeechDelay { get; set; } = "Scale speech delay";
                public string SpeechDelay { get; set; } = "Speech delay";

                public string ActivationSection { get; set; } = "Activation";
                public string ChatEnterActivation { get; set; } = "Activate chat by pressing Enter";
                public string ChatEnterSpecial { get; set; } = "Also activate with common keys";
                public string ShiftEnterChat { get; set; } = "Use Shift + Enter to send message without closing chat";

                public string ColorsSection { get; set; } = "Colors";
                public string SpeechColor { get; set; } = "Speech color";
                public string YellColor { get; set; } = "Yell color";
                public string PartyColor { get; set; } = "Party color";
                public string AllianceColor { get; set; } = "Alliance color";
                public string EmoteColor { get; set; } = "Emote color";
                public string WhisperColor { get; set; } = "Whisper color";
                public string GuildColor { get; set; } = "Guild color";
                public string CharColor { get; set; } = "Chat color";

                public string OverheadText { get; set; } = "Overhead Text";
                public string DisableOverheadMessages { get; set; } = "Disable Overhead Messages";
                public string MessageTypeRegular { get; set; } = "Regular";
                public string MessageTypeSystem { get; set; } = "System";
                public string MessageTypeEmote { get; set; } = "Emote";
                public string MessageTypeLimit3Spell { get; set; } = "Limit-3-Spell";
                public string MessageTypeLabel { get; set; } = "Label";
                public string MessageTypeFocus { get; set; } = "Focus";
                public string MessageTypeWhisper { get; set; } = "Whisper";
                public string MessageTypeYell { get; set; } = "Yell";
                public string MessageTypeSpell { get; set; } = "Spell";
                public string MessageTypeGuild { get; set; } = "Guild";
                public string MessageTypeAlliance { get; set; } = "Alliance";
                public string MessageTypeCommand { get; set; } = "Command";
                public string MessageTypeEncoded { get; set; } = "Encoded";
                public string MessageTypeChatSystem { get; set; } = "Chat System";
                public string MessageTypeDamage { get; set; } = "Damage";
                public string MessageTypeDiscord { get; set; } = "Discord";
                public string MessageTypeParty { get; set; } = "Party";
            }


            public class JournalSection
            {
                public string Label { get; set; } = "Journal";

                public string MaxJournalEntries { get; set; } = "Max journal entries";
                public string JournalOpacity { get; set; } = "Journal opacity";
                public string JournalStyle { get; set; } = "Journal style";
                public string JournalBackgroundColor { get; set; } = "Journal background color";
                public string JournalHideBorders { get; set; } = "Journal hide borders";
                public string HideTimestamp { get; set; } = "Hide timestamp";
                public string JournalHideSystemPrefix { get; set; } = "Journal hide system prefix";
                public string MakeAnchorable { get; set; } = "Make anchorable";
                public string SaveJournalToFile { get; set; } = "Save journal to file";
            }
        }

        public class FontTabLang
        {
            public string FontsLabel { get; set; } = "Fonts";
            public string FontLabel { get; set; } = "Font";
            public string FontsWikiLabel { get; set; } = "TTF Fonts Wiki";
            public string FontSettings { get; set; } = "Font settings";
            public string TtfFontBorder { get; set; } = "TTF Font border";
            public string InfoBarFont { get; set; } = "Info-Bar";
            public string Size { get; set; } = "Size";
            public string SystemChatFont { get; set; } = "System chat";
            public string TooltipFont { get; set; } = "Tooltip";
            public string OverheadFont { get; set; } = "Overhead";
            public string JournalFont { get; set; } = "Journal";
            public string NameplateFont { get; set; } = "Nameplate";
            public string OptionsFont { get; set; } = "Options menu";
        }

        public class CooldownsTabLang
        {
            public string CooldownBarsLabel { get; set; } = "Cooldown Bars";
            public string CustomCooldownBars { get; set; } = "Custom Cooldown Bars";
            public string PositionX { get; set; } = "Position X";
            public string PositionY { get; set; } = "Position Y";
            public string UseLastMovedBarPosition { get; set; } = "Use last moved bar position";
            public string Conditions { get; set; } = "Conditions";
            public string AddCondition { get; set; } = "+ Add condition";
            public string Name { get; set; } = "Name";
            public string Hue { get; set; } = "Hue";
            public string Cooldown { get; set; } = "Cooldown";
            public string TriggerMessageType { get; set; } = "Trigger Message Type";
            public string TriggerMessage { get; set; } = "Trigger Message";
            public string ReplaceExisting { get; set; } = "Replace Existing";
            public string ReplaceExistingTooltip { get; set; } = "Replace any existing cooldown bar with the same name when triggered";
            public string NameTooltip { get; set; } = "Display name shown on the cooldown bar";
            public string HueTooltip { get; set; } = "Color of the cooldown bar";
            public string CooldownTooltip { get; set; } = "Duration of the cooldown in seconds";
            public string TriggerMessageTypeTooltip { get; set; } = "The type of message that triggers this cooldown bar";
            public string TriggerMessageTooltip { get; set; } = "Text to match in the incoming message to trigger this cooldown bar";
            public string PreviewTooltip { get; set; } = "Show a preview of this cooldown bar";
        }

        public class GumpsTabLang
        {
            public string GumpsLabel { get; set; } = "Gumps";
            public string EnableImprovedBuffGump { get; set; } = "Enable improved buff gump";
            public string BuffGumpHue { get; set; } = "Buff gump hue";
            public string EnableAdvancedShopGump { get; set; } = "Enable advanced shop gump";
            public string EnableGumpOpacityAdjustViaAltScroll { get; set; } = "Enable gump opacity adjust via Alt + Scroll";
            public string AltForAnchorsGumps { get; set; } = "Require alt to close anchored gumps";
            public string AltToMoveGumps { get; set; } = "Require alt to move gumps";
            public string CloseEntireAnchorWithRClick { get; set; } = "Close entire group of anchored gumps with right click";
            public string OriginalSkillsGump { get; set; } = "Use original skills gump";
            public string OldStatusGump { get; set; } = "Use old status gump";
            public string PartyInviteGump { get; set; } = "Show party invite gump";
        }

        public class LayerHidingTabLang
        {
            public string Label { get; set; } = "Layer Hiding";

            public string EnableLayerHiding { get; set; } = "Enable Layer Hiding";
            public string OnlyForYourself { get; set; } = "Only for yourself";
            public string OnlyForYourselfTooltip { get; set; } = "Hide layers only on your own character";
            public string HideFollowingLayers { get; set; } = "Hide the following layers on in-game mobiles:";
        }

        public class CombatTabLang
        {
            public CombatSection Combat { get; set; } = new();
            public SpellsTabLang Spells { get; set; } = new();

            public class CombatSection
            {
                public string Label { get; set; } = "Combat";

                public string HoldTabForCombat { get; set; } = "Hold tab for combat";
                public string QueryBeforeAttack { get; set; } = "Query before attack";
                public string QueryBeforeBeneficial { get; set; } = "Query before beneficial acts on murderers/criminals/gray";
                public string ShowBuffDurationOnOldStyleBuffBar { get; set; } = "Show buff duration on old style buff bar";
                public string EnableDPSCounter { get; set; } = "Enable damage-taken DPS counter with damage numbers";
            }
        }

        public class SpellsTabLang
        {
            public string SpellLabel { get; set; } = "Spells";
            public string SpellIndicators { get; set; } = "Spell Indicators";
            public string EnableSpellIndicators { get; set; } = "Enable Spell Indicators";
            public string ImportIndicatorsFromUrl { get; set; } = "Import indicators from URL";
            public string SpellIndicatorsDownloadPrompt { get; set; } = "Enter the URL for the spell config. \n/c[red]This will override your current config.";
            public string EnableOverheadSpellFormat { get; set; } = "Enable overhead spell format";
            public string EnableOverheadSpellHue { get; set; } = "Enable overhead spell hue";
            public string SingleClickForSpellIcons { get; set; } = "Single click for spell icons";
            public string EnableFastSpellHotkeyAssigning { get; set; } = "Enable fast spell hotkey assigning";
            public string SpellOverheadFormat { get; set; } = "Spell overhead format";
            public string DisplayMatchingHotkeysOnSpellIcons { get; set; } = "Display matching hotkeys on spell icons";
            public string SpellIconScale { get; set; } = "Spell icon scale";
            public string HotkeyTextHue { get; set; } = "Hotkey text hue";

            public string InnocentColor { get; set; } = "Innocent color";
            public string BeneficialSpell { get; set; } = "Beneficial spell";
            public string FriendColor { get; set; } = "Friend color";
            public string HarmfulSpell { get; set; } = "Harmful spell";
            public string Criminal { get; set; } = "Criminal";
            public string NeutralSpell { get; set; } = "Neutral spell";
            public string CanBeAttackedHue { get; set; } = "Can be attacked hue";
            public string Murderer { get; set; } = "Murderer";
            public string Enemy { get; set; } = "Enemy";
        }

        public class General
        {
            public string SharedNone { get; set; } = "None";
            public string SharedShift { get; set; } = "Shift";
            public string SharedCtrl { get; set; } = "Ctrl";
            public string SharedAlt { get; set; } = "Alt";
            public string DraggingSectionLabel { get; set; } = "Dragging";

            #region General->General
            public string HighlightObjects { get; set; } = "Highlight objects under cursor";
            public string Pathfinding { get; set; } = "Enable pathfinding";
            public string ShiftPathfinding { get; set; } = "Use shift for pathfinding";
            public string SingleClickPathfind { get; set; } = "Single click for pathfinding";
            public string AlwaysRun { get; set; } = "Always run";
            public string RunUnlessHidden { get; set; } = "Unless hidden";
            public string AutoOpenDoors { get; set; } = "Automatically open doors";
            public string AutoOpenPathfinding { get; set; } = "Open doors while pathfinding";
            public string AutoOpenCorpse { get; set; } = "Automatically open corpses";
            public string CorpseOpenDistance { get; set; } = "Corpse open distance";
            public string CorpseSkipEmpty { get; set; } = "Skip empty corpses";
            public string CorpseSkipEmptyTooltip { get; set; } = "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers.";
            public string CorpseOpenOptions { get; set; } = "Corpse open options";
            public string CorpseOptNone { get; set; } = "None";
            public string CorpseOptNotTarg { get; set; } = "Not targeting";
            public string CorpseOptNotHiding { get; set; } = "Not hiding";
            public string CorpseOptBoth { get; set; } = "Both";
            public string OutRangeColor { get; set; } = "No color for out of range objects";
            public string SallosEasyGrab { get; set; } = "Enable sallos easy grab";
            public string SallosTooltip { get; set; } = "Sallos easy grab is not recommended with grid containers enabled.";
            public string ShowHouseContent { get; set; } = "Show house content";
            public string SmoothBoat { get; set; } = "Smooth boat movements";
            public string ClientVersionLimitedTooltip { get; set; } = "Not all client versions support this feature";
            #endregion

            #region General->Mobiles
            public string ShowMobileHP { get; set; } = "Show mobile's HP";
            public string ShowTargetIndicator { get; set; } = "Show Target Indicator";
            public string MobileHPType { get; set; } = "Type";
            public string HPTypePerc { get; set; } = "Percentage";
            public string HPTypeBar { get; set; } = "Bar";
            public string HPTypeNBoth { get; set; } = "Both";
            public string HPShowWhen { get; set; } = "Show when";
            public string HPShowWhen_Always { get; set; } = "Always";
            public string HPShowWhen_Less100 { get; set; } = "Less than 100%";
            public string HPShowWhen_Smart { get; set; } = "Smart";
            public string HighlightPoisoned { get; set; } = "Highlight poisoned mobiles";
            public string PoisonHighlightColor { get; set; } = "Highlight color";
            public string HighlightPara { get; set; } = "Highlight paralyzed mobiles";
            public string ParaHighlightColor { get; set; } = "Highlight color";
            public string HighlightInvul { get; set; } = "Highlight invulnerable mobiles";
            public string InvulHighlightColor { get; set; } = "Highlight color";
            public string IncomingMobiles { get; set; } = "Show incoming mobile names";
            public string IncomingCorpses { get; set; } = "Show incoming corpse names";
            public string AuraUnderFeet { get; set; } = "Show aura under feet";
            public string AuraOptDisabled { get; set; } = "Disabled";
            public string AuroOptWarmode { get; set; } = "Warmode";
            public string AuraOptCtrlShift { get; set; } = "Ctrl + Shift";
            public string AuraOptAlways { get; set; } = "Always";
            public string AuraForParty { get; set; } = "Use a custom color for party members";
            public string AuraPartyColor { get; set; } = "Party aura color";
            public string IgnoreStaminaCheck { get; set; } = "Disable stamina check for movement";
            public string DisableGrayEnemies { get; set; } = "Don't make last target/enemies gray";
            public string DisableDismountWarmode { get; set; } = "Prevent dismounting in combat";
            #endregion

            #region General->Gumps
            public string DisableTopMenu { get; set; } = "Disable top menu bar";
            public string DisableTopMenuTooltip { get; set; } = "The top menu is pretty vital in TazUO, we recommend leaving this unchecked.";
            public string AltForAnchorsGumps { get; set; } = "Require alt to close anchored gumps";
            public string AltToMoveGumps { get; set; } = "Require alt to move gumps";
            public string CloseEntireAnchorWithRClick { get; set; } = "Close entire group of anchored gumps with right click";
            public string OriginalSkillsGump { get; set; } = "Use original skills gump";
            public string OldStatusGump { get; set; } = "Use old status gump";
            public string PartyInviteGump { get; set; } = "Show party invite gump";
            public string ModernHealthBars { get; set; } = "Use modern health bar gumps";
            public string ModernHPBlackBG { get; set; } = "Use black background";
            public string SaveHPBars { get; set; } = "Save health bars on logout";
            public string CloseHPGumpsWhen { get; set; } = "Close health bars when";
            public string CloseHPOptDisable { get; set; } = "Disabled";
            public string CloseHPOptOOR { get; set; } = "Out of range";
            public string CloseHPOptDead { get; set; } = "Dead";
            public string CloseHPOptBoth { get; set; } = "Both";
            public string GridLoot { get; set; } = "Grid Loot";
            public string GridLootOptDisable { get; set; } = "Disabled";
            public string GridLootOptOnly { get; set; } = "Grid loot only";
            public string GridLootOptOnlyTooltip { get; set; } = "This is not the same as grid containers.";
            public string GridLootOptBoth { get; set; } = "Grid loot and normal container";
            public string GridLootTooltip { get; set; } = "This is not the same as Grid Containers, this is a simple grid gump used for looting corpses.";
            public string ShiftContext { get; set; } = "Require shift to open context menus";
            public string ShiftSplit { get; set; } = "Require shift to split stacks of items";

            #endregion

            #region General->Misc
            public string EnableCOT { get; set; } = "Enable circle of transparency";
            public string COTDistance { get; set; } = "Distance";
            public string COTType { get; set; } = "Type";
            public string COTTypeOptFull { get; set; } = "Full";
            public string COTTypeOptGrad { get; set; } = "Gradient";
            public string COTTypeOptModern { get; set; } = "Modern";
            public string HideScreenshotMessage { get; set; } = "Hide 'screenshot stored in' message";
            public string ObjFade { get; set; } = "Enable object fading";
            public string TextFade { get; set; } = "Enable text fading";
            public string CursorRange { get; set; } = "Show target range indicator";

            public string AutoAvoidObstacules { get; set; } = "Auto Avoid Obstacles";
            public string DragSelectHP { get; set; } = "Enable drag select for health bars";
            public string DragKeyMod { get; set; } = "Key modifier";
            public string DragPlayersOnly { get; set; } = "Players only";
            public string DragMobsOnly { get; set; } = "Monsters only";
            public string DragNameplatesOnly { get; set; } = "Visible nameplates only";
            public string DragX { get; set; } = "X Position of healthbars";
            public string DragY { get; set; } = "Y Position of healthbars";
            public string DragAnchored { get; set; } = "Anchor opened health bars together";
            public string ShowStatsChangedMsg { get; set; } = "Show stats changed messages";
            public string ShowSkillsChangedMsg { get; set; } = "Show skills changed messages";
            public string ChangeVolume { get; set; } = "Every tenth (0.1)";
            #endregion

            #region General->TerrainStatics
            public string HideRoof { get; set; } = "Hide roof tiles";
            public string TreesToStump { get; set; } = "Change trees to stumps";
            public string HideVegetation { get; set; } = "Hide vegetation";
            public string MagicFieldType { get; set; } = "Field types";
            public string MagicFieldOpt_Normal { get; set; } = "Normal";
            public string MagicFieldOpt_Static { get; set; } = "Static";
            public string MagicFieldOpt_Tile { get; set; } = "Tile";
            #endregion
        }

        public class Sound
        {
            public string SharedVolume { get; set; } = "Volume";

            public string EnableSound { get; set; } = "Enable sound";
            public string EnableMusic { get; set; } = "Enable music";
            public string LoginMusic { get; set; } = "Enable login page music";
            public string PlayFootsteps { get; set; } = "Play footsteps";
            public string CombatMusic { get; set; } = "Combat music";
            public string BackgroundMusic { get; set; } = "Play sound when UO is not in focus";
        }

        public class Video
        {
            #region GameWindow

            public string EnablePostProcessing { get; set; } = "Enable post processing effects";
            public string PostProcessingEffectType { get; set; } = "Processing type";
            public string LabelRenderer { get; set; } = "Renderer";
            public string FPSCap { get; set; } = "FPS Cap";
            public string BackgroundFPS { get; set; } = "Reduce FPS when game is not in focus";
            public string EnableVSync { get; set; } = "Enable VSync";
            public string FullsizeViewport { get; set; } = "Always use fullsize game world viewport";
            public string FullScreen { get; set; } = "Fullscreen window";
            public string LockViewport { get; set; } = "Lock game world viewport position/size";
            public string ViewportX { get; set; } = "Viewport position X";
            public string ViewportY { get; set; } = "Viewport position Y";
            public string ViewportW { get; set; } = "Viewport width";
            public string ViewportH { get; set; } = "Viewport height";

            #endregion

            #region Zoom & Scaling

            public string ZoomAndScaling { get; set; } = "Zoom & Scaling";
            public string Zoom { get; set; } = "Zoom";
            public string DefaultZoom { get; set; } = "Default zoom";
            public string ZoomWheel { get; set; } = "Enable zooming with ctrl + mousewheel";
            public string ReturnDefaultZoom { get; set; } = "Return to default zoom after ctrl is released";
            public string PaperdollScaling { get; set; } = "Paperdoll scaling";
            public string GlobalScaling { get; set; } = "Global scaling";

            #endregion

            #region Lighting

            public string AltLights { get; set; } = "Alternative lights";
            public string CustomLLevel { get; set; } = "Custom light level";
            public string Level { get; set; } = "Light level";
            public string LabelLighting { get; set; } = "Lighting";
            public string LightType { get; set; } = "Light level type";
            public string LightType_Absolute { get; set; } = "Absolute";
            public string LightType_Minimum { get; set; } = "Minimum";
            public string DarkNight { get; set; } = "Dark nights";
            public string ColoredLight { get; set; } = "Colored lighting";

            #endregion

            #region Misc
            public string EnableDeathScreen { get; set; } = "Enable death screen";
            public string BWDead { get; set; } = "Black and white mode while dead";
            public string MouseThread { get; set; } = "Run mouse in seperate thread";
            public string TargetAura { get; set; } = "Aura on mouse target";
            public string AnimWater { get; set; } = "Animated water effect";
            public string Perspective { get; set; } = "Perspective";
            public string PlayerPositionOffsetX { get; set; } = "Player Position Offset X";
            public string PlayerPositionOffsetY { get; set; } = "Player Position Offset Y";

            #endregion

            #region Shadows
            public string EnableShadows { get; set; } = "Enable shadows";
            public string RockTreeShadows { get; set; } = "Rock and tree shadows";
            public string TerrainShadowLevel { get; set; } = "Terrain shadow level";
            #endregion
        }

        public class Macros
        {
            public string NewMacro { get; set; } = "New Macro";
            public string DelMacro { get; set; } = "Delete Macro";
        }

        public class ToolTips
        {
            public string LabelTooltipOverrides { get; set; } = "Tooltip Overrides";
            public string LabelOpenOverridesConfig { get; set; } = "Open Overrides Config";
            public string EnableToolTips { get; set; } = "Enable tooltips";
            public string ToolTipDelay { get; set; } = "Tooltip delay";
            public string ToolTipBG { get; set; } = "Tooltip background opacity";
            public string ToolTipFont { get; set; } = "Default tooltip font color";
        }

        public class Speech
        {
            public string ScaleSpeechDelay { get; set; } = "Scale speech delay";
            public string SpeechDelay { get; set; } = "Delay";
            public string SaveJournalE { get; set; } = "Save journal entries to file";
            public string ChatEnterActivation { get; set; } = "Activate chat by pressing Enter";
            public string ChatEnterSpecial { get; set; } = "Also activate with common keys( ! ; : / \\ \\ , . [ | ~ )";
            public string ShiftEnterChat { get; set; } = "Use Shift + Enter to send message without closing chat";
            public string ChatGradient { get; set; } = "Hide chat gradient";
            public string HideGuildChat { get; set; } = "Hide guild chat";
            public string HideAllianceChat { get; set; } = "Hide alliance chat";
            public string SpeechColor { get; set; } = "Speech color";
            public string YellColor { get; set; } = "Yell color";
            public string PartyColor { get; set; } = "Party color";
            public string AllianceColor { get; set; } = "Alliance color";
            public string EmoteColor { get; set; } = "Emote color";
            public string WhisperColor { get; set; } = "Whisper color";
            public string GuildColor { get; set; } = "Guild color";
            public string CharColor { get; set; } = "Chat color";
        }

        public class CombatSpells
        {
            public string Combat { get; set; } = "Combat";
            public string Spells { get; set; } = "Spells";
            public string HoldTabForCombat { get; set; } = "Hold tab for combat";
            public string QueryBeforeAttack { get; set; } = "Query before attack";
            public string QueryBeforeBeneficial { get; set; } = "Query before beneficial acts on murderers/criminals/gray";
            public string EnableOverheadSpellFormat { get; set; } = "Enable overhead spell format";
            public string EnableOverheadSpellHue { get; set; } = "Enable overhead spell hue";
            public string SingleClickForSpellIcons { get; set; } = "Single click for spell icons";
            public string ShowBuffDurationOnOldStyleBuffBar { get; set; } = "Show buff duration on old style buff bar";
            public string EnableFastSpellHotkeyAssigning { get; set; } = "Enable fast spell hotkey assigning";
            public string EnableDPSCounter { get; set; } = "Enable damage-taken DPS counter with damage numbers";
            public string TooltipFastSpellAssign { get; set; } = "Ctrl + Alt + Click a spell icon the open a gump to set a hotkey";
            public string InnocentColor { get; set; } = "Innocent color";
            public string BeneficialSpell { get; set; } = "Beneficial spell";
            public string FriendColor { get; set; } = "Friend color";
            public string HarmfulSpell { get; set; } = "Harmful spell";
            public string Criminal { get; set; } = "Criminal";
            public string NeutralSpell { get; set; } = "Neutral spell";
            public string CanBeAttackedHue { get; set; } = "Can be attacked hue";
            public string Murderer { get; set; } = "Murderer";
            public string Enemy { get; set; } = "Enemy";
            public string SpellOverheadFormat { get; set; } = "Spell overhead format";
            public string TooltipSpellFormat { get; set; } = "{power} for powerword, {spell} for spell name";
        }

        public class Counters
        {
            public string EnableCounters { get; set; } = "Enable counters";
            public string HighlightItemsOnUse { get; set; } = "Highlight items on use";
            public string AbbreviatedValues { get; set; } = "Use abbreviated values";
            public string AbbreviateIfAmountExceeds { get; set; } = "When amount exceeds";
            public string SectionHighlightingLabel { get; set; } = "Counter Highlighting";
            public string HighlightRedWhenAmountIsLow { get; set; } = "Highlight red when amount is low";
            public string HighlightRedIfAmountIsBelow { get; set; } = "Highlight red if amount is below";
            public string CounterLayout { get; set; } = "Counter layout";
            public string GridSize { get; set; } = "Grid size";
            public string Rows { get; set; } = "Rows";
            public string Columns { get; set; } = "Columns";
        }

        public class InfoBars
        {
            public string InfoBar { get; set; } = "Info bar";
            public string InfoBarFont { get; set; } = "Info bar font";
            public string ShowInfoBar { get; set; } = "Show info bar";
            public string HighlightType { get; set; } = "Highlight type";
            public string HighLightOpt_TextColor { get; set; } = "Text color";
            public string HighLightOpt_ColoredBars { get; set; } = "Colored bars";
            public string AddItem { get; set; } = "+ Add item";
            public string Hp { get; set; } = "HP";
            public string Label { get; set; } = "Label";
            public string Color { get; set; } = "Color";
            public string Data { get; set; } = "Data";
            public string DeleteButtonLabel { get; set; } = "X";
            public string HueTooltipFormat { get; set; } = "Hue: 0x{0:X}";
        }

        public class Containers
        {
            public string LabelGridContainerStyling { get; set; } = "Grid Container Styling";
            public string LabelGridContainerHighlighting { get; set; } = "Grid Container Highlighting";
            public string LabelOriginalContainers { get; set; } = "Original";
            public string LabelGridContainers { get; set; } = "Grid";
            public string LabelGridContainersWiki { get; set; } = "Grid Containers";
            public string Description { get; set; } = "These settings are for original container gumps, for grid container settings visit the TazUO section";
            public string CharacterBackpackStyle { get; set; } = "Character backpack style";
            public string BackpackOpt_Default { get; set; } = "Default";
            public string BackpackOpt_Suede { get; set; } = "Suede";
            public string BackpackOpt_PolarBear { get; set; } = "Polar bear";
            public string BackpackOpt_GhoulSkin { get; set; } = "Ghoul skin";
            public string ContainerScale { get; set; } = "Container scale";
            public string AlsoScaleItems { get; set; } = "Also scale items";
            public string UseLargeContainerGumps { get; set; } = "Use large container gumps";
            public string DoubleClickToLootItemsInsideContainers { get; set; } = "Double click to loot items inside containers";
            public string RelativeDragAndDropItemsInContainers { get; set; } = "Relative drag and drop items in containers";
            public string HighlightContainerOnGroundWhenMouseIsOverAContainerGump { get; set; } = "Highlight container on ground when mouse is over a container gump";
            public string RecolorContainerGumpByWithContainerHue { get; set; } = "Recolor container gump with container hue";
            public string OverrideContainerGumpLocations { get; set; } = "Override container gump locations";
            public string OverridePosition { get; set; } = "Override position";
            public string PositionOpt_NearContainer { get; set; } = "Near container";
            public string PositionOpt_TopRight { get; set; } = "Top right";
            public string PositionOpt_LastDraggedPosition { get; set; } = "Last dragged position";
            public string RememberEachContainer { get; set; } = "Remember each container";
            public string RebuildContainersTxt { get; set; } = "Rebuild containers.txt";
        }

        public class Experimental
        {
            public string DisableDefaultUoHotkeys { get; set; } = "Disable default UO hotkeys";
            public string DisableArrowsNumlockArrowsPlayerMovement { get; set; } = "Disable arrows & numlock arrows(player movement)";
            public string DisableTabToggleWarmode { get; set; } = "Disable tab (toggle warmode)";
            public string DisableCtrlQWMessageHistory { get; set; } = "Disable Ctrl + Q/W (message history)";
            public string DisableRightLeftClickAutoMove { get; set; } = "Disable right + left click auto move";
        }

        public class NamePlates
        {
            public string NewEntry { get; set; } = "New entry";
            public string NameOverheadEntryName { get; set; } = "Name overhead entry name";
            public string DeleteEntry { get; set; } = "Delete entry";
            public NamePlatesOptionsTab OptionsTab { get; set; } = new();
        }

        public class NamePlatesOptionsTab
        {
            public string CheckAll { get; set; } = "Check All";
            public string UncheckAll { get; set; } = "Uncheck All";
            public string Items { get; set; } = "Items";
            public string Containers { get; set; } = "Containers";
            public string Stackable { get; set; } = "Stackable";
            public string Moveable { get; set; } = "Moveable";
            public string OtherItems { get; set; } = "Other items";
            public string Gold { get; set; } = "Gold";
            public string LockedDown { get; set; } = "Locked down";
            public string Immovable { get; set; } = "Immovable";
            public string Corpses { get; set; } = "Corpses";
            public string Monster { get; set; } = "Monster";
            public string Humanoid { get; set; } = "Humanoid";
            public string MobilesByType { get; set; } = "Mobiles by type";
            public string YourFollowers { get; set; } = "Your followers";
            public string ExcludeYourself { get; set; } = "Exclude yourself";
            public string Yourself { get; set; } = "Yourself";
            public string MobilesByNotoriety { get; set; } = "Mobiles by notoriety";
            public string Innocent { get; set; } = "Innocent";
            public string Attackable { get; set; } = "Attackable";
            public string Enemy { get; set; } = "Enemy";
            public string Invulnerable { get; set; } = "Invulnerable";
            public string Allied { get; set; } = "Allied";
            public string Criminal { get; set; } = "Criminal";
            public string Murderer { get; set; } = "Murderer";

            public string HotkeyInputTooltip { get; set; } = "Nameplate hotkeys do not support mouse bindings";
        }

        public class Cooldowns
        {
            public string CustomCooldownBars { get; set; } = "Custom cooldown bars";
            public string PositionX { get; set; } = "Position X";
            public string PositionY { get; set; } = "Position Y";
            public string UseLastMovedBarPosition { get; set; } = "Use last moved bar position";
            public string Conditions { get; set; } = "Conditions";
            public string AddCondition { get; set; } = "+ Add condition";
        }

        public class TazUO
        {
            #region General
            public string GridContainers { get; set; } = "Grid containers";
            public string EnableGridContainers { get; set; } = "Enable grid containers";
            public string GridContainersDefaultToOldStyleView { get; set; } = "Open new containers in the original view";
            public string GridContainerScale { get; set; } = "Grid container scale";
            public string AlsoScaleItems { get; set; } = "Also scale items";
            public string HighlightLowContrastItems { get; set; } = "Highlight low contrast items";
            public string LowContrastHighlightStyle { get; set; } = "Low contrast highlight style";
            public string GridItemBorderOpacity { get; set; } = "Grid item border opacity";
            public string BorderColor { get; set; } = "Border color";
            public string ContainerOpacity { get; set; } = "Container opacity";
            public string BackgroundColor { get; set; } = "Background color";
            public string UseContainersHue { get; set; } = "Use container's hue";
            public string SearchStyle { get; set; } = "Search style";
            public string OnlyShow { get; set; } = "Only show";
            public string Highlight { get; set; } = "Highlight";
            public string EnableContainerPreview { get; set; } = "Enable container preview";
            public string TooltipPreview { get; set; } = "This only works on containers that you have opened, otherwise the client does not have that information yet.";
            public string MakeAnchorable { get; set; } = "Make anchorable";
            public string TooltipGridAnchor { get; set; } = "This will allow grid containers to be anchored to other containers/world map/journal";
            public string ContainerStyle { get; set; } = "Container style";
            public string HideBorders { get; set; } = "Hide borders";
            public string DefaultGridRows { get; set; } = "Default grid rows";
            public string DefaultGridColumns { get; set; } = "Default grid columns";
            public string GridHighlightSettings { get; set; } = "Grid highlight settings";
            public string GridHighlightSize { get; set; } = "Grid highlight size";
            public string GridHighlightProperties { get; set; } = "Show highlighted item properties in tooltip";
            public string GridHighlightShowRuleName { get; set; } = "Show matched rule name in tooltip";
            public string GridDisableTargeting { get; set; } = "Disable Targeting Grid Containers";
            #endregion

            #region Journal
            public string Journal { get; set; } = "Journal";
            public string MaxJournalEntries { get; set; } = "Max journal entries";
            public string JournalOpacity { get; set; } = "Journal opacity";
            public string JournalBackgroundColor { get; set; } = "Background color";
            public string JournalStyle { get; set; } = "Journal style";
            public string JournalHideBorders { get; set; } = "Hide borders";
            public string JournalHideSystemPrefix { get; set; } = "Hide \"System:\" prefix";
            public string HideTimestamp { get; set; } = "Hide timestamp";
            public string JournalAnchor { get; set; } = "Make anchorable";
            #endregion

            #region ModernPaperdoll
            public string ModernPaperdoll { get; set; } = "Modern paperdoll";
            public string EnableModernPaperdoll { get; set; } = "Enable modern paperdoll";
            public string PaperdollHue { get; set; } = "Paperdoll hue";
            public string DurabilityBarHue { get; set; } = "Durability bar hue";
            public string ShowDurabilityBarBelow { get; set; } = "Show durability bar below %";
            public string PaperdollAnchor { get; set; } = "Make anchorable";
            #endregion

            #region Nameplates
            public string Nameplates { get; set; } = "Nameplates";
            public string NameplatesAlsoActAsHealthBars { get; set; } = "Nameplates also act as health bars";
            public string HpOpacity { get; set; } = "HP opacity";
            public string HideNameplatesIfFullHealth { get; set; } = "Hide nameplates if full health";
            public string OnlyInWarmode { get; set; } = "Only in warmode";
            public string BorderOpacity { get; set; } = "Border opacity";
            public string BackgroundOpacity { get; set; } = "Background opacity";
            public string AvoidOverlap { get; set; } = "Avoid overlap";
            public string NameWidth { get; set; } = "Name width";
            public string NameplateHeight { get; set; } = "Height";
            public string CornerRadius { get; set; } = "Corner radius";
            public string SeparateHealthBarWidth { get; set; } = "Separate health bar width";
            public string HealthBarWidth { get; set; } = "Health bar width";
            public string SplitHealthBar { get; set; } = "Split health bar";
            public string FixedWidth { get; set; } = "Fixed width";
            public string ShowWordOfDeathIcon { get; set; } = "Show Word of Death icon at 30% health";
            public string Preset { get; set; } = "Preset";
            #endregion

            #region Mobile
            public string Mobiles { get; set; } = "Mobiles";
            public string DamageToSelf { get; set; } = "Damage to self";
            public string DamageToOthers { get; set; } = "Damage to others";
            public string DamageToPets { get; set; } = "Damage to pets";
            public string DamageToAllies { get; set; } = "Damage to allies";
            public string DamageToLastAttack { get; set; } = "Damage to last attack";
            public string DisplayPartyChatOverPlayerHeads { get; set; } = "Display party chat over player heads";
            public string TooltipPartyChat { get; set; } = "If a party member uses party chat their text will also show above their head to you";
            public string OverheadTextWidth { get; set; } = "Overhead text width";
            public string TooltipOverheadText { get; set; } = "This adjusts the maximum width for text over players, setting to 0 will allow it to use any width needed to stay one line";
            public string BelowMobileHealthBarScale { get; set; } = "Below mobile health bar scale";
            public string AutomaticallyOpenHealthBarsForLastAttack { get; set; } = "Automatically open health bars for last attack";
            public string UpdateOneBarAsLastAttack { get; set; } = "Update one bar as last attack";
            public string HiddenPlayerOpacity { get; set; } = "Hidden player opacity";
            public string HiddenPlayerHue { get; set; } = "Hidden player hue";
            public string RegularPlayerOpacity { get; set; } = "Regular player opacity";
            public string AutoFollowDistance { get; set; } = "Auto follow distance";
            public string DisableAutoFollow { get; set; } = "Disable alt click to auto follow";
            public string DisableMouseInteractionsForOverheadText { get; set; } = "Disable mouse interactions for overhead text";
            public string OverridePartyMemberHues { get; set; } = "Override party member body hues with friendly hue";
            public string TurnDelay { get; set; } = "Adjust turn delay";
            #endregion

            #region Misc
            public string Misc { get; set; } = "Misc";
            public string DisableSystemChat { get; set; } = "Disable system chat";
            public string EnableImprovedBuffGump { get; set; } = "Enable improved buff gump";
            public string BuffGumpHue { get; set; } = "Buff gump hue";
            public string MainGameWindowBackground { get; set; } = "Main game window background";
            public string HealthBarIndicator { get; set; } = "Health bar indicator";
            public string OnlyShowBelowHp { get; set; } = "Only show below hp %";
            public string Size { get; set; } = "Size";
            public string SpellIconScale { get; set; } = "Spell icon scale";
            public string DisplayMatchingHotkeysOnSpellIcons { get; set; } = "Display matching hotkeys on spell icons";
            public string HotkeyTextHue { get; set; } = "Hotkey text hue";
            public string EnableGumpOpacityAdjustViaAltScroll { get; set; } = "Enable gump opacity adjust via Alt + Scroll";
            public string EnableAdvancedShopGump { get; set; } = "Enable advanced shop gump";
            public string DisplaySkillProgressBarOnSkillChanges { get; set; } = "Display skill progress bar on skill changes";
            public string TextFormat { get; set; } = "Text format";
            public string EnableSpellIndicatorSystem { get; set; } = "Enable spell indicator system";
            public string ImportFromUrl { get; set; } = "Import from url";
            public string InputRequestUrl { get; set; } = "Enter the url for the spell config. \n/c[red]This will override your current config.";
            public string Download { get; set; } = "Download";
            public string Cancel { get; set; } = "Cancel";
            public string AttemptingToDownloadSpellConfig { get; set; } = "Attempting to download spell config..";
            public string SuccesfullyDownloadedNewSpellConfig { get; set; } = "Succesfully downloaded new spell config.";
            public string FailedToDownloadTheSpellConfigExMessage { get; set; } = "Failed to download the spell config. ({0})";
            public string FailedToLoadSpellConfigMessage { get; set; } = "The configuration was successfully downloaded but could not be loaded";
            public string AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars { get; set; } = "Also close anchored healthbars when auto closing healthbars";
            public string EnableAutoResyncOnHangDetection { get; set; } = "Enable auto resync on hang detection";
            public string PlayerOffsetX { get; set; } = "Player Offset X";
            public string PlayerOffsetY { get; set; } = "Player Offset Y";
            public string UseLandTexturesWhereAvailable { get; set; } = "Use land textures where available(Experimental)";
            public string SOSGumpID { get; set; } = "SOS Gump ID";
            public string UseWASDMovement { get; set; } = "Use WASD movement instead of arrow keys";
            public string ApplyBorderCaveTiles { get; set; } = "Apply a border to cave tile art";
            public string ForcedHouseTransparencyLevel { get; set; } = "Forced house transparency";
            public string EnableHouseTransparency { get; set; } = "Enable forced house transparency";
            public string HouseTransparencyTileHue { get; set; } = "House transparency tile hue";
            public string EnableASyncMapLoading { get; set; } = "Enable ASync map loading";
            public string ForceManagedZlib { get; set; } = "Force using a managed zlib";
            #endregion

            #region Tooltips
            public string Tooltips { get; set; } = "Tooltips";
            public string AlignTooltipsToTheLeftSide { get; set; } = "Align tooltips to the left side";
            public string AlignMobileTooltipsToCenter { get; set; } = "Align mobile tooltips to center";
            public string BackgroundHue { get; set; } = "Background hue";
            public string HeaderFormatItemName { get; set; } = "Header format(Item name)";
            public string TooltipOverrideSettings { get; set; } = "Tooltip override settings";
            public string ForcedTooltips { get; set; } = "Force tooltips on pre-tooltip servers";
            #endregion

            #region Fontsettings
            public string FontSettings { get; set; } = "Font settings";
            public string TtfFontBorder { get; set; } = "TTF Font border";
            public string InfoBarFont { get; set; } = "Infobar font";
            public string SharedSize { get; set; } = "Size";
            public string SystemChatFont { get; set; } = "System chat font";
            public string TooltipFont { get; set; } = "Tooltip font";
            public string OverheadFont { get; set; } = "Overhead font";
            public string JournalFont { get; set; } = "Journal font";
            public string NameplateFont { get; set; } = "Nameplate font";
            public string OptionsFont { get; set; } = "Options menu font";
            #endregion

            #region Controller
            public string Controller { get; set; } = "Controller";
            public string MouseSesitivity { get; set; } = "Mouse Sensitivity";
            public string EnableController { get; set; } = "Enable controller input";
            #endregion

            #region SettingsTransfer
            public string SettingsTransfers { get; set; } = "Settings transfers";
            public string SettingsWarning { get; set; } = "/es/c[red]! Warning !/cd\n" +
                "This will override other character's profile options!\n" +
                "This is not reversable!\n" +
                "You have {0} other profiles that will may overridden with the settings in this profile.\n\n" +
                "This will not override: Macros, skill groups, info bar, grid container data, or gump saved positions.";
            public string OverrideAll { get; set; } = "Override {0} other profiles with this one.";
            public string OverrideAllMacros { get; set; } = "Override {0} other profile's macros with this one.";
            public string OverrideSuccess { get; set; } = "{0} profiles overriden.";
            public string OverrideSame { get; set; } = "Override {0} other profiles on this same server with this one.";
            public string SetAsDefault { get; set; } = "Set this profile as the default for new characters.";
            public string SetMacrosAsDefault { get; set; } = "Set this profile's macros as the default for new characters.";
            public string SetAsDefaultSuccess { get; set; } = "This profile is now the default for new characters.";
            public string SetMacrosAsDefaultSuccess { get; set; } = "This profile's macros are now the default for new characters.";

            #endregion

            #region GumpScaling
            public string GumpScaling { get; set; } = "Gump scaling";
            public string ScalingInfo { get; set; } = "Some of these settings may only take effect after closing and reopening. Visual bugs may occur until the gump is closed and reopened.";
            public string PaperdollGump { get; set; } = "Paperdoll Gump";
            public string GlobalScaling { get; set; } = "Global scale";
            public string GlobalScale { get; set; } = "Scale";
            #endregion

            public string AutoLoot { get; set; } = "Autoloot";
            public string AutoLootEnable { get; set; } = "Enable auto loot";
            public string ScavengerEnable { get; set; } = "Enable scavenger";
            public string AutoLootProgessBarEnable { get; set; } = "Show progress bar while looting";
            public string AutoLootHumanCorpses { get; set; } = "Loot human corpses? (Potentially player corpses)";

            public string AutoSellMenu { get; set; } = "Auto Sell";
            public string AutoSellEnable { get; set; } = "Enable auto sell feature";
            public string AutoSellMaxUniques { get; set; } = "Maximum unique items per transaction";
            public string AutoSellMaxUniquesTooltip { get; set; } = "This is the maximum number of unique items that will be sold at once. A value of 0 means unlimited. A stack of items counts as one towards this limit. Some servers block transactions that sell too many unique items.";
            public string AutoSellMaxItems { get; set; } = "Maximum total items per transaction";
            public string AutoSellMaxItemsTooltip { get; set; } = "This is the maximum number of items that will be sold at once. A value of 0 means unlimited. Some servers block transactions that sell too many items.";

            public string AutoBuyMenu { get; set; } = "Auto Buy";
            public string AutoBuyEnable { get; set; } = "Enable auto buy feature";
            public string GraphicChangeFilter { get; set; } = "Graphic Filter";
            public string Hotkeys { get; set; } = "Hotkeys";


            #region VoiceRecognition
            public string VoiceRecognition { get; set; } = "Voice Recognition";
            public string VoiceRecognitionEnable { get; set; } = "Enable voice recognition";
            public string VoiceToggle { get; set; } = "Toggle Voice";
            public string VoiceModelPath { get; set; } = "Vosk model path";
            public string VoiceModelPathTooltip { get; set; } = "Path to a Vosk speech model directory or .zip file. Download models from alphacephei.com/vosk/models - zip files will be auto-extracted to the vosk/ folder.";
            public string VoiceRecognitionStatus { get; set; } = "Status: {0}";
            public string VoiceStatusReady { get; set; } = "Ready";
            public string VoiceStatusNotInitialized { get; set; } = "Not initialized - set model path first";
            public string VoiceStatusListening { get; set; } = "Listening...";
            public string VoiceApplyModel { get; set; } = "Apply model path";
            public string VoiceCreateMacro { get; set; } = "Create macro button";
            #endregion

            #region VisibileLayers
            public string VisibleLayers { get; set; } = "Visible Layers";
            public string VisLayersInfo { get; set; } = "These settings are to hide layers on in-game mobiles. Check the box to hide that layer.";
            public string OnlyForYourself { get; set; } = "Only for yourself";
            public string HiddenLayersEnabled { get; set; } = "Enable visible layer system";
            #endregion
        }
    }
}
