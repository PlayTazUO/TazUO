using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

internal class VersionHistory : NineSliceGump
{
    private static readonly string[] _updateTexts =
    [
        """
        /c[white][4.17.0]/cd
        ## Misc:
        - Implemented a new priority queue
        - Less iteration over gumps for better performance and more stable FPS
        - Add create macro button to new macro editor window
        - Add queue all item moves
        - Add queue all item uses(double clicks)
        - Merged existing use item queue into new priority queue system
        - Add an experimental object delay detector
        - Add Take Item button the Item Detail Window in item database interface - Sarumon
        - Renamable containers
        - Add health bar collector & ability buttons to hide hud feature
        - Auto-Unequip for Potions (And complete rewrite for spell unequip also!) - Vem
        - Add option to set quick heal/cure spells in assistant
        - Changed item inspector gump to copy to clipboard instead of dump
        - Add corpse hue option for autoloot
        ## Legion
        - `API.CastSpell` now tries for an exact spell first, if that fails it will try for a partial match
        - Add a simple search box to script manager window
        - Add `API.GetItemData()` to PyItem's
        - Add API.SetOutlineColor() to objects

        ## Bugs
        - Fix for healthbar collector race condition crash
        - Fix for grid sorting via name - Vem
        - Disable ctrl alt lock for spell button gump
        - Fix targeting backpack in paperdoll
        - Fix for context menus appearing in the wrong spot
        - Add more robust bandage healing checks to bandage agent
        - Fix nameplates when mobile is scaled
        - Fix for joystick still moving character when game is not in focus
        - Truncate container name's and add tooltip to see full name
        - House customization lag fix
        - Removed style 8 from journal and grid container
        - Fix web map not rendering markers set as color None

        ## Other
        - Add Hit Magic Arrow to DefaultProperties in GridHighLightRules - Vem
        - Split packet handlers for better organization internally - Vem
        - `PixelPicker` cache optimization - Vem
        - Added some back-end stuff to support tagging items with a custom name in the future
        """,

        """
        /c[white][4.16.0]/cd
        ## Misc
        - Improved graphic filter UI
        - Added export/import to sound filter UI
        - Allow XBR when not zoomed in/out
        - Add season filter
        - Added spell id's to spell indicator editor
        - Added delete button to spell indicator editor
        - Grid container visual rework to allow minimizing(Double click title bar)
        - Add heal/cure buttons to pet health bars in addition to party members
        - Updated graphics driver hints for SDL.
        - Script editor window now has a minimum size instead of being ultra small
        - **Added macro editor in assistant window**
        - Season filter will now update live
        - Add clipboard import/export to organizer, auto buy/sell/loot, graphic filter, journal filter and macros.
        - Updated auto loot export to use clipboard
        - Add mobs/npcs to web map
        - Add healthbar collector gump
        - Leaving the go-to location empty will clear the current goto location on submit - <@493756177893818369>
        - Sort and add spaces to main macro actions in assistant macro editor
        - Move mastery spell macros to its own action

        ## Legion
        - Added -stopall command to stop all running legion scripts
        - Fix API.SetSkillLock
        - API.CloseGump now returns a bool if a gump was closed or not
        - Added `API.CurrentAbilityNames()` and `API.KnownAbilityNames()` - <@493756177893818369>
        - Added `API.GetPartyMemberSerials()` - <@493756177893818369>
        - Update `API.ReplyGump` to also take option switches

        ## Bugs
        - Reworked a lot of graphic replacement system
        - Fix title not showing up in paperdolls when closing/reopening
        - Viewport clamping fix
        - Fix paperdoll lag issue
        - Potential crash fix for rendered text.
        - Added null and enabled checks for controller input handling.
        - Updated plugin creation to handle empty paths better.
        - Fix auto sell error when switching characters.
        - Fix division by zero crash in anchor manager.
        - Fix KR dress packet
        - Fix game scaling not scaling the login scene properly
        - Fix `API.ConfigNextGump` crashing client - <@493756177893818369>
        """,

        """
        /c[white][4.15.0]/cd
        # Misc
        - Change grid highlight to use min/max weight instead of a set 50 stone limit
        - Add support for <color> tags in UOX3 emu
        - Add ability to create new spell configs in spell indicator window
        - Add optional per-item loot container to grid highlight settings for autoloot
        - Hold Ctrl while moving a gump to position it with more fine-tuned precision
        - Better game scaling
        - Add discord message type to separate them in the journal
        - Viewport can no longer be moved/resized outside the game window
        - Auto unequip/reequip for casting
        - Add sound filter
        - Add disable weather option

        # Legion Py
        - Improve script recorder: current menu replies and Script Manager refresh - @echokk11
        - Add missing LayerOrder on py gump wrapper
        - Added API DropDown.OnDropDownOptionSelected() callback option
        - Add API.CloseContextMenus()
        - Add Undress to LegionScripting - @dirkackerman
        - Add API.UnIgnoreObject()
        - Add python API endpoint to get items in the current menu gump - @dirkackerman

        # Bugs
        - When using API.GetSkill a rare crash where the skill was null
        - A crash when saving settings.json but access is denied
        - Fix elf animation while dead
        - Fix spell.icon dragging after accidentally breaking it in the last update
        - No longer has extra spaces when using ctrl q/w
        - Ctrl q/w restarts from "0" after pressing enter
        - Crash fix for spell indicator system
        - DM's now properly appear in the top section of the gump(Discord)
        - The `-reply` command now works properly to reply to the last person to DM you
        - Crash fix when calling API.RemoveMarkedTile and world or map is null
        - Fix auto avoid obstacle
        - Fix items not re-rendering bug in paperdoll
        """,

        """
        /c[white][4.14.0]/cd
        # Misc
        - Testing out long-distance pathfinding
        - **Removed `.lscript` support**
        - Added UseType macro
        - Added max buy price for auto-buy agent
        - Added new web-based world map
        - Added quick spell search to counter bar, added spell menu select to spell bar
        - Add optional camera smoothing effect

        # Legion Py
        - All gump related controls are now py wrappers
        - All gump methods have been moved to API.Gumps.xxx, please begin changing your scripts over.
        - If you use API.PyProfile, change to API.Profile
        - Improved API.py generation
        - Add .IsContainer and .GetContainerGump() to API items
        - API.Mount and Dismount no long attack your pet in war mode
        - Added API.CreateTiledGumpPic() to API.Gumps
        - Add NameAndProps method to PyItem and PyMobile (#314)
        - Add Notoriety to PyMobile
        - Fix for API.HasGump ignoring specified gump id
        - Fix API.RequestTargetAny()
        - Add API.IsDestroyed to Py game objects

        # Bugs
        - Dress agent crash fix when an item or config are null
        - Crash fix when updating spell icons while collection was modified
        - Crash fix for race condition in bandage manager
        - Fix .CancelPathfinding() in API
        - Fix Py item/mobile .Destroy()
        - Checks for vet buff in bandage agent now
        - Reduce some stuttering when loading animations
        - Fixed a stutter related to animations
        - Fixed a stutter in packet processing
        - Crash fix in grid containers
        - Caps lock no longer affects spell bar hotkeys
        - Crash fix for null item name
        - Fix Async map loading
        """,

        """
        /c[white][4.13.0]/cd
        # Misc
        - Add tooltips to auto loot config explaining hue and graphic ( -1 = any )
        - Better poisoned healing handling in bandage agent
        - Add dex formula option to bandage agent
        - Add disable self heal to bandage agent
        - Bandages should also be found in waist layer too
        - Change script info gump to use hex values
        - Add configurable alpha setting to assistant windows
        - Added FC and FCR calculations to spell indicator system( @nesci )
        - Allow multi move gump to place on statics
        - Add character name to journal log filenames
        - Add auto open own corpse option
        - Improve performance when auto open corpses
        - Added `-reply` command for responding to discord DM's in-game
        - Autoloot will re-open corpses if you got too far away and did not finish looting

        # Legion Py
        - Remove ScriptManagerGump in favor of ScriptManagerWindow
        - Fix for API.HasGump potentially returning a serial when the gump is disposed
        - Add API.ConfigNextGump
        - Replaced the old script browser with an improved, easier to use one
        - Converted API.Player to use a python wrapper
        - Add running scripts window(Menu in script manager)
        - Add .Clear() to base control wrapper
        - Add .Mount to player wrapper
        - Added edit constants to script manager menu for scripts

        # Bugs
        - Performance improvement for excessive quantities of large gumps
        - Fix for riding manticore mounts
        - Fixed a few bugs with grid highlighting
        - Fix for a race condition when stopping scripts
        - Fix house customization bounds on upper levels
        - API Crashfix when profile is null
        - Rare crash when trying to select a server that doesn't exist
        - Rare crash when disposing controls
        - Rare crash when a texture is disposed after flush but before draw
        - Bug fix protection for disposing controls on key input
        - Fixed opening scripts externally with spaces in the file name
        - Added several null checks to py api
        - Fix a few typos
        - Fix bug with saving journal entries to file
        - Small bug fix when using API.CloseGump with id 0
        """,

        """
        /c[white][4.12.0]/cd
        # Misc
        - Minor performance improvements to mobiles, land, and statics.
        - Bandage agent will use last ping + 10ms wait before allowing a retry when bandage buff option is in use
        - Added item name tooltip to autoloot graphic in assistant
        - Change title bar progress bar to | characters
        - Add grid container context menu to autoloot this container
        - Save guild and party member names for world map cache
        - Display Graphics and Container values in hexadecimal format in various areas
        - Add player and mouse to hide hud options
        - Add ClearHands and EquipHands macros
        - Add move macro up/down buttons
        - Added force managed zlib option
        - Added per-item containers for auto loot agent
        - Add per item destinations to organizer agent

        # Legion Py
        - Move some py class stuff to the base game object class to broaden accessibility
        - Add Oragnizer() method
        - Add ClientCommand() method
        - RequestTarget now properly returns 0 if canceled
        - Added GetGumpContents to py api

        # Bugs
        - Fix modern shop gump prices
        - Fix hide hud functionality when all is selected
        - API Crash fix when checking buffs
        - Fix bandage manager queueing more heals then 1 at a time
        - Ignore destroyed items in grid containers
        - Some ImgUI display size checks
        - Fix drag select while interacting with imgui
        - Fix for rare server gump crashes
        - Fix a few bugs in crash reports and browser launching
        - Fix thread safety issue in TextFileParser causing gump crashes
        - Fix for paperdoll gump scaling war mode button
        - Add small delay after healing buff is removed to try bandaging
        - Add catch for rare error while changing seasons
        - Add a null guard for API.Pathfinding()
        - Bug fix for OPL crash
        - Rare crash with rendered text
        """,

        """
        /c[white][4.11.0]/cd
        # Misc
        - Autoloot now provides backups
        - Finish up info section in assistant window
        - Added ToggleMount to macros
        - Added Bandage Agent to global queue system with priority
        - Added hud options to imgui assistant
        - Added titlebar to imgui assistant
        - Added spell indicator to imgui assistant
        - Added journal filters to imgui assistant
        - Added friends list to imgui assistant
        - Added spell bar settings to imgui assistant
        - Allow healing friends with bandage agent
        - Added item database to keep track of all known items
        - Added dress agent to imgui assistant
        - Moved old assistant to new menu
        - Legion assistant and new style script manager reopen now
        - ImGui windows can be closed with right click now
        - Grid highlight selects best matching rule and show on item tooltips now(Can be toggled in options) ( <@397429006657519617> )

        # Legion Py
        - Better fake py api gen
        - Begin conversion to Pywrapper for controls
        - Added dropdown box to API
        - Added `.Destroy()` to Entity objects(Items, Mobiles)
        - Added new Modern UI gump option `API.CreateModernGump`
        - Added events to API.Events(See https://TazUO.org docs)
        - Fixed various Que spellings to Queue
        - Added `.GetAvailableDressOutfits()`
        - Added IsTree and Name to PyStatic ( <@188729541307531264> )
        - Added `GetAllGumps()` (Server side gumps)
        - Added Persistent Vars manager
        - Convert persistent vars to use SQL
        - `.GumpContains` should search better now
        - Added MatchingHighlightName and MatchesHighlight bool to PyItem

        # Bug fixes
        - Fix autoloot loading incorrect profile
        - Fixed a rare crash when pathfinding on the world map
        - Fixed a rare crash with nameplates
        - Fixed several potential rare crashes related to paperdolls
        - Fix close corpse macro to also close GridContainer corpses
        - Spiked whip now correctly shows abilities, thank you <@719588654913159190>
        - Audio switches when an audio device is removed/added
        - Grid container locked slots and autosort fixes
        - No more double input in new assistant window
        - Disable pooling for textboxes
        """,

        """
        /c[white][4.10.0]/cd
        # Misc
        - Add clear journal button
        - Add VSync toggle
        - Async map loading for better performance
        - Add auto buy to new assistant window
        - Add Async map loading toggle
        - Default profiles to no reduced fps while inactive
        - Default profiles to use vsync
        - Add graphic to autoloot assistant window
        - Moved graphic replacement settings to new assistant window

        # Legion Py
        - Fixed Legion Py script stopping bug
        - Added API.StopRequested to check if your script should stop
        - Added several friends methods
        - Limit API.Pause to 0-30 seconds
        - Check for stop requests after API.Pause internally
        - Added API.Dress()
        - API.Pause now uses a cancellation token to end early when hitting stop on a script mid pause

        # Bug fixes
        - Fixed crash when world is somehow null
        - Fixed a rare crash on control hit test
        - Fix for grid container borders not properly hued
        - Fix for lag in high traffic areas
        - Fix for viewport not showing border
        - Fix for grid containers sorting when they shouldn't be
        - Fix background color on comparison tooltips
        """,

        """
        /c[white][4.9.0]/cd
        ## Misc
        - Moved auto sell to new assistant window

        ## Legion Py
        - Changed how script stopping works slightly, it should indicate now if the script did not stop at least.

        ## Bugs
        - Fixed a couple crashes when re-logging quickly
        - Fixed a high cpu usage bug
        """,

        "/c[white][4.8.0]/cd\n" +
        """
        # Misc
        - Vastly improved grid container performance
        - Better profiler info for testing and debugging
        - Remove need for cuoapi.dll(Mostly affects mac arm users)
        - Grid containers for corpses will now only show the items in there instead of the full grid
        - Added Top menu -> More -> Tools -> Retrieve gumps to find lost gumps(Outside your screen)
        - Added Lichtblitz color picker to color picker gumps
        - Single click coords on world map to copy to clipboard
        - Performance improvements for tiles, land, mobiles
        - Added new low fps reminder and `-syncfps` command to sync your fps limit to your monitor

        # Legion Py
        - Fix for buffexists method to make sure a valid string was put in and null checks
        - Add API.SetWarMode()
        - Add optional notoriety list to GetAllMobiles()
        - Add API.GlobalMsg() to send global chat messages

        # Bug fixes
        - Check if cuoapi is found/loaded, if not don't try to load plugins and disable pluginhost
        - Check if sdl event is null before processing
        - Fix buff icon text
        - Fixed a rare infobar crash
        """+
        "\n",

        "/c[white][4.7.0]/cd\n" +
        """
        - Upgraded to latest FNA and SDL(Major changes on backend)
        - Added missing shortblade abilities
        - Autoloot is now in the new assistant menu

        ## Other
        - Better crash report information regarding OS and .Net frameworks
        - Better url handling for opening links in the browser
        - Added action delay config to autoloot menu
        - Added option to block dismount in warmode

        ## LegionPy
        - Added disable module cache option to menu
        - Minor back-end changes to python engine
        - Fix for legion py imports - nesci
        - Added optional graphic and distance to GetAllMobiles method
        - Changed Legion Py journals to use a py specific journal entry
        - Enforce max journal entries limitation on legion py journal entries(User-chosen max journal entries)

        - Fixed script recorder using API.Cast instead of CastSpell

        ## Bug fixes
        - Added some safety net for a rare crash while saving anchored gumps
        - Fix for ImGui display size occasional crash
        - Fix for a crash during python script execution
        - Fix for parrots on OSI - BCrowly
        - Potential high memory usage with a lot of UI controls open
        - Fix login gump still showing after logging in sometimes
        - Fix books not allowing you to type when you first open them
        """ +
        "\n",

        "/c[white][4.5.0]/cd\n" +
        """
        - Check for yellow hits before bandaging
        - Added some missing OSI mounts
        - Show other drives in file selector(When you navigate to root)
        - Grid highlights will re-check items when making changes to configs
        - Minor improvements to grid container ui interactions
        - Several new python API changes

        ## Bug fixes
        - Fix for in-game file selector without a parent folder
        - Improved nameplate jumping when moving and using avoid overlap
        - Fix chat command hue
        """ +
        "\n",

        "\n\n/c[white]For further history please visit our forums: forums.tazuo.org"
    ];

    private ScrollArea _scrollArea;
    private VBoxContainer _vBoxContainer;

    public VersionHistory(World world) : base(world, 0, 0, 400, 500, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BorderSize, true, 200, 200)
    {
        CanCloseWithRightClick = true;
        CanMove = true;

        Build();

        CenterXInViewPort();
        CenterYInViewPort();
    }

    private void Build()
    {
        Clear();

        Positioner pos = new(13, 13);

        Add(pos.Position(TextBox.GetOne(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Color.White, TextBox.RTLOptions.DefaultCentered(Width))));

        Add(pos.Position(TextBox.GetOne(Language.Instance.CurrentVersion + CUOEnviroment.Version, TrueTypeLoader.EMBEDDED_FONT, 20, Color.Orange, TextBox.RTLOptions.DefaultCentered(Width))));

        _scrollArea = new ScrollArea(0, 0, Width - 26, Height - (pos.LastY + pos.LastHeight) - 32, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };
        _vBoxContainer = new VBoxContainer(_scrollArea.Width - _scrollArea.ScrollBarWidth());
        _scrollArea.Add(_vBoxContainer);

        foreach (string s in _updateTexts)
        {
            _vBoxContainer.Add(TextBox.GetOne(s, TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(_scrollArea.Width - _scrollArea.ScrollBarWidth())));
        }

        Add(pos.Position(_scrollArea));

        Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUOWiki, "https://github.com/PlayTazUO/TazUO/wiki", Color.Orange, 15), 25, Height - 20));
        Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUODiscord, "https://discord.gg/QvqzkB95G4", Color.Orange, 15), Width - 110, Height - 20));
    }

    protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
        Build();
    }
}
