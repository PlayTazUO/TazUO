# Changelog
All notable changes to TazUO will be recorded here.

---
## In Development

### Features
* Moved the Health Bars options tab from the Interface category to Gameplay > Mobiles - [P.R 714](https://github.com/PlayTazUO/TazUO/pull/714) ([bittiez](https://github.com/bittiez))
* Added an "Import Map File" option to the world map context menu (under Map Marker Options) that copies a selected .map/.csv/.xml file into the current server's marker directory and reloads markers - [P.R 710](https://github.com/PlayTazUO/TazUO/pull/710) ([bittiez](https://github.com/bittiez))
* Added a "Keep Existing" option to cooldown bars that preserves the running countdown instead of adding a new bar when the same rule triggers again; mutually exclusive with "Replace Existing" - [P.R 709](https://github.com/PlayTazUO/TazUO/pull/709) ([bittiez](https://github.com/bittiez))
* Added a "Pathfind to location" option to the world map context menu (below "Go to location") that walks the player to entered map/sextant coordinates - [P.R 699](https://github.com/PlayTazUO/TazUO/pull/699) ([bittiez](https://github.com/bittiez))
* Overhauled the options window with a new, modern UI (use command `old-options-window` to open legacy window) - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Added reorder support to CoolDown Bars - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Pet support for the bandage agent - [P.R 485](https://github.com/PlayTazUO/TazUO/pull/485) ([yuval-po](https://github.com/yuval-po))
* Allow dropping of items onto minimized grid containers - [P.R 487](https://github.com/PlayTazUO/TazUO/pull/487) ([yuval-po](https://github.com/yuval-po))
* Auto focus and enter to submit support added to prompt input window - [P.R 486](https://github.com/PlayTazUO/TazUO/pull/486) ([bittiez](https://github.com/bittiez))
* Add overhead message filter option - [P.R 494](https://github.com/PlayTazUO/TazUO/pull/494) ([bittiez](https://github.com/bittiez))
* Color picker gump now dynamically calculates page count based on loaded hues. Updated shader slightly for supporting hues past 3k. - [P.R 496](https://github.com/PlayTazUO/TazUO/pull/496) ([bittiez](https://github.com/bittiez))
* Add option to skip server select & reorganized login gump - [P.R 498](https://github.com/PlayTazUO/TazUO/pull/498) ([bittiez](https://github.com/bittiez))
* Opening an already open grid container will now unminimize it if minimized and bring it to the front - [P.R 502](https://github.com/PlayTazUO/TazUO/pull/502) ([bittiez](https://github.com/bittiez))
* Script manager and assistant windows now re-center when reopened via toolbar button instead of closing/reopening - [P.R 503](https://github.com/PlayTazUO/TazUO/pull/503) ([bittiez](https://github.com/bittiez))
* Swapped a few hard coded texts for their cliloc equivelent - [P.R 505](https://github.com/PlayTazUO/TazUO/pull/505) ([bittiez](https://github.com/bittiez))
* Auto loot corpse retry delay is now configurable in the Auto Loot agent UI (range 1000–600000ms, default 5000ms) - [P.R 508](https://github.com/PlayTazUO/TazUO/pull/508) ([bittiez](https://github.com/bittiez))
* Added a quick settings.json editor on the login gump - [P.R 510](https://github.com/PlayTazUO/TazUO/pull/510) ([bittiez](https://github.com/bittiez))
* Added an alternative character select screen - [P.R 513](https://github.com/PlayTazUO/TazUO/pull/513) ([bittiez](https://github.com/bittiez))
* Added a macro to set an organizer's source container via target - [P.R 516](https://github.com/PlayTazUO/TazUO/pull/516) ([bittiez](https://github.com/bittiez))
* Added new language system for easier futute translations - [P.R 519](https://github.com/PlayTazUO/TazUO/pull/519) ([bittiez](https://github.com/bittiez))
* Added an auto stat lock agent - [P.R 524](https://github.com/PlayTazUO/TazUO/pull/524) ([bittiez](https://github.com/bittiez))
* UI language selection on the login screen now applies immediately instead of requiring a restart - [P.R 526](https://github.com/PlayTazUO/TazUO/pull/526) ([bittiez](https://github.com/bittiez))
* Added loops to in-game macros - [P.R 519](https://github.com/PlayTazUO/TazUO/pull/519) ([DavideRei](https://github.com/DavideRei))
* ZIP file support for Legion Scripting - script libraries with custom PNG artwork can now be distributed and loaded as a single .zip file; scripts in gumps can display zip textures via `API.Gumps.LegionTextureControl` - [P.R 533](https://github.com/PlayTazUO/TazUO/pull/533) ([bittiez](https://github.com/bittiez))
* Added tuoassets.zip support to override embedded TUO graphics assets; a zip in the client directory overrides embedded assets and a zip in the UO directory takes highest priority for server-specific overrides; supports named embedded asset overrides and gump/art ID overrides matching the existing PNG system - [P.R 534](https://github.com/PlayTazUO/TazUO/pull/534) ([bittiez](https://github.com/bittiez))
* Add option to disable door opening while player is hidden - [P.R 535](https://github.com/PlayTazUO/TazUO/pull/535) ([bittiez](https://github.com/bittiez))
* Add option to disable system chat while the Resizable Journal is open - [P.R 541](https://github.com/PlayTazUO/TazUO/pull/541) ([bittiez](https://github.com/bittiez))
* Expanded the spell bar so slots can hold macros and weapon abilities in addition to spells - [P.R 543](https://github.com/PlayTazUO/TazUO/pull/543) ([bittiez](https://github.com/bittiez))
* Bandage agent now supports journal message triggers — configure messages (separated by `;`) that immediately allow re-bandaging when matched - [P.R 550](https://github.com/PlayTazUO/TazUO/pull/550) ([bittiez](https://github.com/bittiez))
* Create Tinkerer window with cliloc viewer - [P.R 549](https://github.com/PlayTazUO/TazUO/pull/549) ([bittiez](https://github.com/bittiez))
* Made pathfinding limits user-configurable (max nodes, search timeout, and retry attempts) in the Pathfinding options tab - [P.R 551](https://github.com/PlayTazUO/TazUO/pull/551) ([bittiez](https://github.com/bittiez))
* Added customizable nameplates with presets, sizing, health bar, background, font, and overlap options - [P.R 558](https://github.com/PlayTazUO/TazUO/pull/558) ([Nesci28](https://github.com/Nesci28))
* Add marker search to web map to hide non-matching markers - [P.R 565](https://github.com/PlayTazUO/TazUO/pull/565) ([bittiez](https://github.com/bittiez))
* Add toggle buy and sell agent macros - [P.R 566](https://github.com/PlayTazUO/TazUO/pull/566) ([bittiez](https://github.com/bittiez))
* Add Self Heal hotkey (hold-to-heal, Magery + Chivalry) - [P.R 567](https://github.com/PlayTazUO/TazUO/pull/567) ([eddo87](https://github.com/eddo87))
* Add highlight low contrast grid items option - [P.R 563](https://github.com/PlayTazUO/TazUO/pull/563) ([Nesci28](https://github.com/Nesci28))
* Add option to select and copy text in journal - [P.R 575](https://github.com/PlayTazUO/TazUO/pull/575) ([Nesci28](https://github.com/Nesci28))
* Add Script slot type to the Spell Bar - [P.R 568](https://github.com/PlayTazUO/TazUO/pull/568) ([eddo87](https://github.com/eddo87))
* Converted the Add/Edit User Marker world map window to a Myra window - [P.R 588](https://github.com/PlayTazUO/TazUO/pull/588) ([bittiez](https://github.com/bittiez))
* Journal filters now match partial messages (case-insensitive contains) instead of requiring exact matches - [P.R 593](https://github.com/PlayTazUO/TazUO/pull/593) ([bittiez](https://github.com/bittiez))
* Added a centralized hotkey system with a Hotkeys tab in the Assistant window for viewing, rebinding, and conflict-checking hotkeys (including macros), plus a global hotkey shutoff - [P.R 591](https://github.com/PlayTazUO/TazUO/pull/591) ([bittiez](https://github.com/bittiez))
* Added a world map context menu option to choose the double click action between toggling the lock state and toggling fullscreen - [P.R 602](https://github.com/PlayTazUO/TazUO/pull/602) ([bittiez](https://github.com/bittiez))
* Added a "Loot Hovered Item" macro that grabs whatever item the mouse is hovering over — grid containers, regular containers, paperdoll, modern paperdoll, items on the ground, and item nameplates - [P.R 603](https://github.com/PlayTazUO/TazUO/pull/603) ([bittiez](https://github.com/bittiez))
* Added a warning when more than 100 gumps of the same type are open at once - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([bittiez](https://github.com/bittiez))
* Add Skill slot type to the Spell Bar - [P.R 613](https://github.com/PlayTazUO/TazUO/pull/613) ([bittiez](https://github.com/bittiez))
* Added individual scaling support for the Status Gump - [P.R 614](https://github.com/PlayTazUO/TazUO/pull/614) ([bittiez](https://github.com/bittiez))
* Added a single scaling option for all server-created gumps - [P.R 615](https://github.com/PlayTazUO/TazUO/pull/615) ([bittiez](https://github.com/bittiez))
* Add option to disable the party-style health bar for party members - [P.R 620](https://github.com/PlayTazUO/TazUO/pull/620) ([bittiez](https://github.com/bittiez))
* Added a clear (X) button to the nameplate manager search field - [P.R 621](https://github.com/PlayTazUO/TazUO/pull/621) ([bittiez](https://github.com/bittiez))
* Added a scaling option for context menus - [P.R 623](https://github.com/PlayTazUO/TazUO/pull/623) ([bittiez](https://github.com/bittiez))
* Added marker icons to the web map, served from their source file on disk instead of streaming rendered textures, with a toggle to switch between icons and circles - [P.R 637](https://github.com/PlayTazUO/TazUO/pull/637) ([bittiez](https://github.com/bittiez))
* World map pathfinding now supports multi-segment routes — hold the append modifier (Shift by default, rebindable) while Ctrl right-clicking to chain a new path segment onto the current route (A→B→C) instead of restarting - [P.R 638](https://github.com/PlayTazUO/TazUO/pull/638) ([bittiez](https://github.com/bittiez))
* Added scrollable item list views under the trade gump showing each item's graphic, name, and stack amount, with one list per trade side - [P.R 649](https://github.com/PlayTazUO/TazUO/pull/649) ([bittiez](https://github.com/bittiez))
* Trade gump item lists now have a semi-transparent backing for legibility and scale with the rest of the gump - [P.R 661](https://github.com/PlayTazUO/TazUO/pull/661) ([bittiez](https://github.com/bittiez))
* Added a better weather atmospheric effect - [P.R 592](https://github.com/PlayTazUO/TazUO/pull/592) ([birdinforest](https://github.com/birdinforest))
* Added list view to grid containers - [P.R 626](github.com/PlayTazUO/TazUO/pull/626) ([Nesci28](https://github.com/Nesci28))
* World map font style now applies to both names and markers and moved to the main context menu; added a TTF Fonts menu to render names/markers with TrueType fonts and adjustable size - [P.R 655](https://github.com/PlayTazUO/TazUO/pull/655) ([bittiez](https://github.com/bittiez))
* Added a journal option to make the journal transparent (hide border, tabs, scroll bar, and background) after 3 seconds when it is not hovered and not the active window - [P.R 670](https://github.com/PlayTazUO/TazUO/pull/670) ([bittiez](https://github.com/bittiez))
* Rebuilt the world map Markers Manager as a Myra window with real tabs (full-path tooltips), map/zoom columns, editable map and zoom fields, and — on editable .usr/.csv files — checkbox bulk delete and bulk move of markers between files - [P.R 679](https://github.com/PlayTazUO/TazUO/pull/679) ([bittiez](https://github.com/bittiez))
* Converted the world map "Go to location" window to a Myra window with a clear button and live decoding that shows the resolved map coordinates (from map or sextant input) as you type - [P.R 682](https://github.com/PlayTazUO/TazUO/pull/682) ([bittiez](https://github.com/bittiez))
* Added a "Radar Map" entry to the top bar More menu that opens the radar/mini map - [P.R 686](https://github.com/PlayTazUO/TazUO/pull/686) ([bittiez](https://github.com/bittiez))
* Added a "Button Editor" button to the new options window's Macros tab, giving access to the macro button editor (label, scale, color, graphic) - [P.R 685](https://github.com/PlayTazUO/TazUO/pull/685) ([bittiez](https://github.com/bittiez))
* Added an option to hide the "Target: name" overhead message shown when a macro sets a target - [P.R 687](https://github.com/PlayTazUO/TazUO/pull/687) ([bittiez](https://github.com/bittiez))
* Added a "Borderless window (no title bar)" video option that removes the window border while keeping a normal windowed size, separate from fullscreen-borderless - [P.R 680](https://github.com/PlayTazUO/TazUO/pull/680) ([bittiez](https://github.com/bittiez))
* The Open macro type's gump list in the Macros tab is now sorted alphabetically and spaced after capitals for legibility, matching the main macro list - [P.R 688](https://github.com/PlayTazUO/TazUO/pull/688) ([bittiez](https://github.com/bittiez))
* Added a negative search field to the nameplate manager that hides matching nameplates (the opposite of search); both search fields now accept multiple terms separated by `;` - [P.R 691](https://github.com/PlayTazUO/TazUO/pull/691) ([bittiez](https://github.com/bittiez))
* Added auto skinning support — when a corpse is opened, a configured knife/dagger is automatically used on it through the action queue; includes enable and human-corpse toggles, an editable knife graphic list, and a "Target Skinning Weapon" button in the Auto Loot tab - [P.R 694](https://github.com/PlayTazUO/TazUO/pull/694) ([bittiez](https://github.com/bittiez))
* Nameplate search and negative search now save per nameplate profile — switching profiles and logging out/in restore each profile's filters, and both fields are editable in the nameplate profile editor - [P.R 695](https://github.com/PlayTazUO/TazUO/pull/695) ([bittiez](https://github.com/bittiez))
* Added new tazuo polls window - ([bittiez](https://github.com/bittiez))
* Added a goto location input to the web map (accepts raw map or sextant coordinates) that sets the player's Go-To location - [P.R 708](https://github.com/PlayTazUO/TazUO/pull/708) ([bittiez](https://github.com/bittiez))

### Fixes
* Fixed the nameplate overhead manager gump not resizing to fit all buttons and profile names, and now refreshes its buttons when a profile is renamed in the options window - [P.R 698](https://github.com/PlayTazUO/TazUO/pull/698) ([bittiez](https://github.com/bittiez))
* Fixed client crash ("pointer being freed was not allocated") when deleting map markers in the marker manager, caused by leaked marker list controls whose graphics textures were freed off the render thread by the GC finalizer - [P.R 678](https://github.com/PlayTazUO/TazUO/pull/678) ([bittiez](https://github.com/bittiez))
* Auto open doors no longer closes a door that is already open - [P.R 674](https://github.com/PlayTazUO/TazUO/pull/674) ([bittiez](https://github.com/bittiez))
* Added a Video > Misc option to reduce mobile feet clipping through walls (character depth slice step, default now minimizes clipping) - [P.R 666](https://github.com/PlayTazUO/TazUO/pull/666) ([bittiez](https://github.com/bittiez))
* Fixed simultaneous drag and resize occurring on resizable windows - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Fixed reset/min/max size buttons in resizable windows not updating correctly or causing crashes - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Fixed mouse clicks passing through Myra windows in certain cases - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Render scale is now clamped to 0.1–1.75 to prevent crashes from out-of-range values - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([yuval-po](https://github.com/yuval-po))
* Fix rapid right-clicks interrupting character movement - [P.R 600](https://github.com/PlayTazUO/TazUO/pull/600) ([bittiez](https://github.com/bittiez))
* Fixed crash reading BWT-compressed UOP animations caused by returning a non-pooled buffer to the array pool - [P.R 532](https://github.com/PlayTazUO/TazUO/pull/532) ([bittiez](https://github.com/bittiez))
* Fixed WorldMap crash when a marker has a null/empty name - [P.R 530](https://github.com/PlayTazUO/TazUO/pull/530) ([bittiez](https://github.com/bittiez))
* Fixed reconnect getting stuck when the server is unavailable or restarting during a reconnect attempt - [P.R 517](https://github.com/PlayTazUO/TazUO/pull/517) ([bittiez](https://github.com/bittiez))
* Fix NullReferenceException in campfire character selection when a character has no appearance data - [P.R 515](https://github.com/PlayTazUO/TazUO/pull/515) ([bittiez](https://github.com/bittiez))
* Mouse wheel macros hijack scroll from shop gumps - [P.R 479](https://github.com/PlayTazUO/TazUO/pull/479) ([yuval-po](https://github.com/yuval-po))
* FindItems now properly returns the highest level container - [P.R 488](https://github.com/PlayTazUO/TazUO/pull/488) ([Jascen](https://github.com/Jascen))
* Grid container label missing updates - [P.R 487](https://github.com/PlayTazUO/TazUO/pull/487) ([yuval-po](https://github.com/yuval-po))
* Fixed server index from name - ([bittiez](https://github.com/bittiez))
* Fixed bulletin board crash - ([bittiez](https://github.com/bittiez))
* Added maximum depth recursion to legion py scripting to prevent stack overflow - ([bittiez](https://github.com/bittiez))
* Fixed tooltips going outside window bounds when scaled - ([bittiez](https://github.com/bittiez))
* Fixed logout gump not being centered when scaled - ([bittiez](https://github.com/bittiez))
* Back button now reaches the server select & username screens when 'Skip Server Select' is enabled, and added a `-skipserverselect` command-line arg - [P.R 512](https://github.com/PlayTazUO/TazUO/pull/512) ([bittiez](https://github.com/bittiez))
* Fixed an issue with Toggle Legion Script macro not reoping it - ([bittiez](https://github.com/bittiez))
* Fixed IndexOutOfRangeException when pressing the arrow button on the server selection screen - [P.R 520](https://github.com/PlayTazUO/TazUO/pull/520) ([bittiez](https://github.com/bittiez))
* Fixed server selection gump lingering behind the login screen when stepping back - [P.R 521](https://github.com/PlayTazUO/TazUO/pull/521) ([bittiez](https://github.com/bittiez))
* Setting reconnect time via launch args would not allow less than 1000(Reconnect time is in seconds, it should be 1) - ([bittiez](https://github.com/bittiez))
* Better long distance pathfinding - [P.R 454](https://github.com/PlayTazUO/TazUO/pull/454) [P.R 539](https://github.com/PlayTazUO/TazUO/pull/539) ([eddo87](https://github.com/eddo87))
* Fixed SDL GPU assertion ("Command buffer already submitted!") on macOS caused by unnecessary GPU texture readback in the web map server - [P.R 538](https://github.com/PlayTazUO/TazUO/pull/538) ([bittiez](https://github.com/bittiez))
* A few minor ui fixes where focus gained was not needed - ([bittiez](https://github.com/bittiez))
* Fix: spell bar hotkeys not firing when Scroll Lock is on - [P.R 548](https://github.com/PlayTazUO/TazUO/pull/549) ([eddo87](https://github.com/eddo87))
* Fixed UltimaLive block reloads leaving the reloaded map chunk untracked, so it could never be garbage collected and stayed loaded until relog - [P.R 556](https://github.com/PlayTazUO/TazUO/pull/556) ([bittiez](https://github.com/bittiez))
* Fix IsFlying flag reference and add missing CantWalkOrRun speed mode - [P.R 560](https://github.com/PlayTazUO/TazUO/pull/560) ([bittiez](https://github.com/bittiez))
* Fixed EndOfStreamException when loading world map marker icons (.cur/.ico) caused by reading the full pooled buffer instead of the actual stream length - [P.R 579](https://github.com/PlayTazUO/TazUO/pull/579) ([bittiez](https://github.com/bittiez))
* Fixed NullReferenceException in ArtLoader/MultiLoader when art or multi data files are missing, replacing the cryptic crash with a clear FileNotFoundException pointing at the UO data directory - [P.R 582](https://github.com/PlayTazUO/TazUO/pull/582) ([bittiez](https://github.com/bittiez))
* Missing required UO data files now show a clear error message naming the file and data directory instead of crashing with a crash report - [P.R 583](https://github.com/PlayTazUO/TazUO/pull/583) ([bittiez](https://github.com/bittiez))
* Show a clear, actionable message when graphics shaders fail to compile instead of crashing — points at outdated/unavailable OpenGL (Remote Desktop, a VM without 3D acceleration, or missing/outdated GPU drivers) and suggests updating drivers or switching renderer - [P.R 584](https://github.com/PlayTazUO/TazUO/pull/584) ([bittiez](https://github.com/bittiez))
* Add option disable gargoyle flying animation - ([Nesci28](https://github.com/Nesci28))
* Fixed NullReferenceException in ImprovedBuffGump when a buff icon's title cliloc is not found - [P.R 585](https://github.com/PlayTazUO/TazUO/pull/585) ([bittiez](https://github.com/bittiez))
* Fixed crash when creating a journal tab with a name that already exists - [P.R 589](https://github.com/PlayTazUO/TazUO/pull/589) ([bittiez](https://github.com/bittiez))
* Show a suggested fix for the "OpenGL 2.1 support is required!" graphics device error, advising users to update their drivers or try the `-force_driver 1`, `2`, or `3` launch args - [P.R 595](https://github.com/PlayTazUO/TazUO/pull/595) ([bittiez](https://github.com/bittiez))
* Pass the first click through to a Myra window so clicking a control on an unfocused window works in one click instead of requiring a focus click first - [P.R 604](https://github.com/PlayTazUO/TazUO/pull/604) ([bittiez](https://github.com/bittiez))
* Fixed percent-type "Show Mobiles HP" text drifting in a radius around the mobile based on zoom level instead of staying centered on top - [P.R 608](https://github.com/PlayTazUO/TazUO/pull/608) ([bittiez](https://github.com/bittiez))
* Fixed web map markers disappearing after changing facets - the live event stream is now kept alive across map changes so markers refresh correctly when recalling between facets - [P.R 619](https://github.com/PlayTazUO/TazUO/pull/619) ([bittiez](https://github.com/bittiez))
* Game cursor now uses a consistent style across all maps instead of changing on non-zero maps - [P.R 622](https://github.com/PlayTazUO/TazUO/pull/622) ([bittiez](https://github.com/bittiez))
* Fixed camera smoothly panning across the map on a teleport (dungeon entrance/exit, recall, gate) instead of snapping to the new location when Camera Smoothing is enabled - [P.R 633](https://github.com/PlayTazUO/TazUO/pull/633) ([bittiez](https://github.com/bittiez))
* Fixed the Show Target Indicator option not hiding the target bracket graphics while the new target system was enabled; the option now solely controls the brackets - [P.R #](https://github.com/PlayTazUO/TazUO/pull/#) ([bittiez](https://github.com/bittiez))
* Restored tooltips for mastery spell abilities that were dropped in the mastery spellbook rework - [P.R 635](https://github.com/PlayTazUO/TazUO/pull/635) ([bittiez](https://github.com/bittiez))
* Fixed context menus rendering outside the window bounds when global or context menu scaling was active, and fixed submenu arrows being clipped out of view at higher scales - [P.R 640](https://github.com/PlayTazUO/TazUO/pull/640) ([bittiez](https://github.com/bittiez))
* Retrieve Gumps now also brings Myra (iGui) windows such as the Script Manager back on screen, and accounts for game scale - [P.R 641](https://github.com/PlayTazUO/TazUO/pull/641) ([bittiez](https://github.com/bittiez))
* Fixed context menu submenus being unreachable when they open to the left (or upward) near the screen edge, where moving the mouse onto them closed the whole menu - [P.R 642](https://github.com/PlayTazUO/TazUO/pull/642) ([bittiez](https://github.com/bittiez))
* Fixed nested context menu submenus extending past the bottom of the window; the overflow guard now clamps against absolute screen coordinates and the live logical window height so it stays correct at any nesting depth and honors both context menu and game scaling - [P.R 646](https://github.com/PlayTazUO/TazUO/pull/646) ([bittiez](https://github.com/bittiez))
* Fixed OverflowException crash in MultiMapLoader when a DisplayMap packet supplies out-of-range or inverted map bounds; the pixel buffer allocation is now validated to guard against negative or overflowing dimensions - [P.R 658](https://github.com/PlayTazUO/TazUO/pull/658) ([bittiez](https://github.com/bittiez))
* Fixed client crash (IndexOutOfRangeException) from unpaired UTF-16 surrogates in journal/text; malformed text (e.g. a truncated emoji from the server) is now sanitized before measuring instead of crashing - [P.R 659](https://github.com/PlayTazUO/TazUO/pull/659) ([bittiez](https://github.com/bittiez))
* Show a suggested fix for the ArgumentOutOfRangeException from FetchDisplayAdapter, which happens when connected displays change at runtime (monitor unplugged/slept, dock or KVM switch, laptop lid) - [P.R 660](https://github.com/PlayTazUO/TazUO/pull/660) ([bittiez](https://github.com/bittiez))
* Fixed container overhead text (item names/speech) being pushed off screen instead of appearing above the item - grid containers now account for the scroll offset when scaled/scrolled, and legacy containers anchor the text over the item's actual slot instead of a stale click coordinate - [P.R 667](https://github.com/PlayTazUO/TazUO/pull/667) ([bittiez](https://github.com/bittiez))
* World map mobile dots are now colored by notoriety (matching the radar/minimap) instead of always being red - [P.R 684](https://github.com/PlayTazUO/TazUO/pull/684) ([bittiez](https://github.com/bittiez))
* Context menu submenus now stay open until the menu is closed, an item is clicked, or another submenu is opened, and remain fully clickable when they open above the parent menu instead of closing when the mouse moves toward them - [P.R 689](https://github.com/PlayTazUO/TazUO/pull/689) ([bittiez](https://github.com/bittiez))

### Legion
* Added ModernNineSliceGump.SetLegionTexture to go along with zip files and custom png's - Use your own png for a 9-slice texture - ([bittiez](https://github.com/bittiez))
* Fixed a legion bug where control/gump `.IsDisposed` was not reported correctly. - ([bittiez](https://github.com/bittiez))
* Added `API.GetClilocString(cliloc, englishOnly=False)` to retrieve cliloc strings from scripts - [P.R 546](https://github.com/PlayTazUO/TazUO/pull/546) ([bittiez](https://github.com/bittiez))
* Added `API.PlaySound(index)` to play a sound effect locally, `API.LastSpellIndex` to get the index of the last spell cast, and `API.LastSpellName` to get the name of the last spell cast - [P.R 561](https://github.com/PlayTazUO/TazUO/pull/561) ([bittiez](https://github.com/bittiez))
* Added the ability to bind a hotkey (keyboard, mouse, or controller) to a Legion script from the script manager to toggle it on/off - [P.R 609](https://github.com/PlayTazUO/TazUO/pull/609) ([bittiez](https://github.com/bittiez))
* Added an optional `API.OnStop(callback)` hook — when set, stopping a script is delayed until the callback is processed via `API.ProcessCallbacks` or a maximum of 5 seconds has elapsed - [P.R 652](https://github.com/PlayTazUO/TazUO/pull/652) ([bittiez](https://github.com/bittiez))

### Misc
* Remove tab completion and command history tracking - [P.R 489](https://github.com/PlayTazUO/TazUO/pull/489) ([Jascen](https://github.com/Jascen))
* Add option to toggle bandage agent from macros - [P.R 491](https://github.com/PlayTazUO/TazUO/pull/491) ([bittiez](https://github.com/bittiez))
* Refactored PromptPopupWindow into a reusable text prompt and replaced InputRequest with it - [P.R 509](https://github.com/PlayTazUO/TazUO/pull/509) ([bittiez](https://github.com/bittiez))
* Managed zlib is now a global setting, defaults to enabled on Linux and disabled on Windows/Mac, and the `-zlib` arg now persists the setting - [P.R 514](https://github.com/PlayTazUO/TazUO/pull/514) ([bittiez](https://github.com/bittiez))
* Add option to disable corpse retry in autoloot - [P.R 525](https://github.com/PlayTazUO/TazUO/pull/525) ([bittiez](https://github.com/bittiez))
* Corpse hueing from auto loot will now reapply when a corpse is removed and added back onto your screen - [P.R 557](https://github.com/PlayTazUO/TazUO/pull/557) ([bittiez](https://github.com/bittiez))
* Corpse hueing from auto loot will now reapply when a corpse is removed and added back onto your screen - [P.R 607](https://github.com/PlayTazUO/TazUO/pull/607) ([bittiez](https://github.com/bittiez))
* Moved cooldown bar rules to a dedicated `cooldownbars.json` in the profile folder (existing profiles are migrated automatically) and consolidated their configuration into the new options menu - [P.R 711](https://github.com/PlayTazUO/TazUO/pull/711) ([bittiez](https://github.com/bittiez))

---

## V5.2.0

### Features
* Automatic loading of system fonts - [P.R 444](https://github.com/PlayTazUO/TazUO/pull/444) ([yuval-po](https://github.com/yuval-po) & [bittiez](https://github.com/bittiez))
* Added Timer APIs to Legion - [P.R 457](https://github.com/PlayTazUO/TazUO/pull/457) ([yuval-po](https://github.com/yuval-po))

### Misc
* Added a few fixes to music filter system - ([bittiez](https://github.com/bittiez))
* Added option to set current macros as default for new characters - ([bittiez](https://github.com/bittiez))
* Added option to override all other character macros with current characters - ([bittiez](https://github.com/bittiez))
* Updated some default profile settings - ([bittiez](https://github.com/bittiez))
* * Lowered music volume defaults
* * Changed default auto follow distance to 1
* * Enabled ctrl scroll to zoom by default
* * Enabled spell format by default
* * Nameplates only show in warmode is now false
* * Increased overhead chat width to 400(Up from 200)
* * Disable dismount in warmode now on by default
* Updated TazUO User and Channel areas to not stretch the entire screen when full - ([bittiez](https://github.com/bittiez))
* Split stack gump now accepts spacebar in addition to enter to accept the amount - ([bittiez](https://github.com/bittiez))
* Removed anonymous metrics - ([bittiez](https://github.com/bittiez))
* Removed TazUO Chat - ([bittiez](https://github.com/bittiez))
* Running Scripts window can now effectivley make use of allocated space via wrapping -  [P.R 460](https://github.com/PlayTazUO/TazUO/pull/460) ([yuval-po](https://github.com/yuval-po))

### Fixes
* Fix for latest UO Publish causing a crash in animation loading - ([bittiez](https://github.com/bittiez))
* SOS Gump ID now supports entering id as both hex and int(0x0000, or 0000 directly) - ([bittiez](https://github.com/bittiez))
* Fixed a rare crash that could occur when receiving chat messages during login/logout - [P.R 455](https://github.com/PlayTazUO/TazUO/pull/455) ([yuval-po](https://github.com/yuval-po))
* Fixed a rare crash that could occur during login due to a concurrent gump modification - [P.R 456](https://github.com/PlayTazUO/TazUO/pull/456) ([yuval-po](https://github.com/yuval-po))
* Fixed a crash that occurred when clicking an empty `Combobox` - [P.R 451](https://github.com/PlayTazUO/TazUO/pull/451) ([yuval-po](https://github.com/yuval-po))
* Dramatically reduced memory footprint and load times for system fonts - [P.R 446](https://github.com/PlayTazUO/TazUO/pull/446) ([yuval-po](https://github.com/yuval-po))
* Eventine-specific paperdoll layer ordering - [P.R 458](https://github.com/PlayTazUO/TazUO/pull/458) ([yuval-po](https://github.com/yuval-po))
* Crash when using the Plugin API's UsePrimaryAbility/UseSecondaryAbility methods - [P.R 461](https://github.com/PlayTazUO/TazUO/pull/461) ([yuval-po](https://github.com/yuval-po))
* HTML control text dispalyed in GridLootGump name label in UO POL based servers - [P.R 462](https://github.com/PlayTazUO/TazUO/pull/462) ([yuval-po](https://github.com/yuval-po) & [bittiez](https://github.com/bittiez))
* Spell progress indicator never shows - [P.R 464](https://github.com/PlayTazUO/TazUO/pull/464) ([yuval-po](https://github.com/yuval-po))
* Allow deletion of individual pieces of house stairs - [P.R 466](https://github.com/PlayTazUO/TazUO/pull/466) ([yuval-po](https://github.com/yuval-po))
* Add missing Shirt and Kilt slot to paperdoll - [P.R 467](https://github.com/PlayTazUO/TazUO/pull/467) ([yuval-po](https://github.com/yuval-po))
* Two Modern Paperdoll issues (closure and context menus) - [P.R 468](https://github.com/PlayTazUO/TazUO/pull/468) ([yuval-po](https://github.com/yuval-po))
* Allow resetting of outline color via the SetOutlineColor API - [P.R 471](https://github.com/PlayTazUO/TazUO/pull/471) ([yuval-po](https://github.com/yuval-po))

---

## V5.1.0

### Assistant
* Expanded sound filter to show last 5 sounds, and sound names to make them easier to identify - ([bittiez](https://github.com/bittiez))
* Added music filter similar to sound filter - ([bittiez](https://github.com/bittiez))

### Fixes
* Fix accidentally broken game viewport - ([bittiez](https://github.com/bittiez))

---

## V5.0.0

### Breaking Changes

* Python API classes (`Py___`) renamed to `Api___` or `ApiUi___`
* All `IronPython` types/classes in `LegionAPI` were replaced with standard C# constructs
* Return type for `API.LastTargetPos` changed from `Vector3Int` to `ApiPoint3D`
* `API.Events` signature changes
* `PyOnItemCreated` renamed to `OnItemCreated` and now sends an `ApiItem` as an argument
* `OnItemUpdated` event now sends an `ApiItem` as an argument
* `PyOnBuffAdded` renamed to `OnBuffAdded`
* `PyOnBuffRemoved` renamed to `OnBuffRemoved`
* `Buff` renamed to `ApiBuff` (Affects `OnBuffAdded` & `OnBuffRemoved`)


### Features

* Began replacing Assistant(ImGui) with a new UI (Myra) - ([bittiez](https://github.com/bittiez))
* Added support for *C#* scripting - [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) ([bittiez](https://github.com/bittiez) & [yuval-po](https://github.com/yuval-po))
* Added an `Open Location` to the script manager window- [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) ([yuval-po](https://github.com/yuval-po))
* Added built-in IRC support and channel - [P.R 366](https://github.com/PlayTazUO/TazUO/pull/366) ([bittiez](https://github.com/bittiez))
* Added Auto-Loot priority tiers (High/Normal/Low) - [P.R 363](https://github.com/PlayTazUO/TazUO/pull/363) ([crameep](https://github.com/crameep))
* Added `ToggleAutoLoot` macro to quickly enable/disable autolooting - ([bittiez](https://github.com/bittiez))
* Added a server prompt UI for when servers request input(like naming a rune) - ([bittiez](https://github.com/bittiez))

### API

* Added *Sound* APIs to for `Legion Scripting` - [P.R 362](https://github.com/PlayTazUO/TazUO/pull/362) ([fpw](https://github.com/fpw))
* Added `API.PickUpToCursor`, `API.DropFromCursor` and `API.GetHeldItem` - ([bittiez](https://github.com/bittiez))
* Added `IsHidden`, `IsGargoyle`, `IsMounted`, `IsDrivingBoat`, and `IsRunning` to `ApiMobile` - ([bittiez](https://github.com/bittiez))
* Added `API.ScriptName` and `API.ScriptPath` - ([bittiez](https://github.com/bittiez))
* Added missing API documentation types - [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369), [P.R 370](https://github.com/PlayTazUO/TazUO/pull/370), [P.R 371](https://github.com/PlayTazUO/TazUO/pull/371) ([yuval-po](https://github.com/yuval-po))
* Added `API.GetPartyLeader()` - ([bittiez](https://github.com/bittiez))
* Added optional entries tuple to `ReplyGump` - ([bittiez](https://github.com/bittiez))
* Fixed QueueMoveItem* methods defaulting to 1 item from the stack instead of the entire stack - ([bittiez](https://github.com/bittiez))
* Added `ApiItem.OnGround` to see if an item is on the ground or not - ([bittiez](https://github.com/bittiez))
* Generate py builtins file when updating API to negate the need for import API - ([bittiez](https://github.com/bittiez))
* `ApiGameObject` position(X, Y, Z) are now pulled directly to reflect live changes - ([bittiez](https://github.com/bittiez))
* Incorporate cancellation token to avoid continueing to process api calls after a script has stopped - ([bittiez](https://github.com/bittiez))
* Added `API.DressItems` to use the dress agent from scripts - ([fspy](https://github.com/fspy))
* Fix IronPython type mismatch crash when passing serial lists to API - ([fspy](https://github.com/fspy))
* Added ApiMobile.Direction to see the direction a mob is facing - ([bittiez](https://github.com/bittiez))

### Assistant

* Added a *Skill Management* tab to the *Legion Assistant* - [P.R 359](https://github.com/PlayTazUO/TazUO/pull/359) ([crameep](https://github.com/crameep))
* Organizer tab now shows graphic when hovering over the graphic art - ([bittiez](https://github.com/bittiez))
* Added Mobile outline option - Highlighting mobiles by notoriety - ([bittiez](https://github.com/bittiez))
* Added TazUO chat (Top menu -> More -> TazUO Chat) - ([bittiez](https://github.com/bittiez))
* ItemDatabase search now defaults to not only "this character" - ([bittiez](https://github.com/bittiez))
* Allow bandage agent threshold to range from 1-99(Previously 10-95) - ([bittiez](https://github.com/bittiez))
* Add adjustment for pathfinding max z level difference - ([bittiez](https://github.com/bittiez))
* Auto sell now has Add from container and Clear all buttons - ([bittiez](https://github.com/bittiez))
* Allow setting custom item names via the item database - ([bittiez](https://github.com/bittiez))
* Added an option to auto bandage ally's in bandage manager - ([bittiez](https://github.com/bittiez))
* UI styling overhaul of new Myra windows - ([fspy](https://github.com/fspy))
* Auto loot now allows reordering and renaming when using -1 for any graphic - ([bittiez](https://github.com/bittiez))
* Buy agent now has an option to include sub containers in item counts - ([bittiez](https://github.com/bittiez))

### Fixes

* Fixed empty ability name on active ability when calling `CurrentAbilityNames` - [P.R 373](https://github.com/PlayTazUO/TazUO/pull/373) ([yuval-po](https://github.com/yuval-po))
* Fixed automatic corpse opening when too far away - [P.R 371](https://github.com/PlayTazUO/TazUO/pull/371) ([yuval-po](https://github.com/yuval-po))
* Fixed a reliability issue with `API.OnHotKey` - [P.R 365](https://github.com/PlayTazUO/TazUO/pull/365) ([fpw](https://github.com/fpw))
* Fixed healthbar collector occasionally becoming unresponsive to targeting/clicks - ([bittiez](https://github.com/bittiez))
* Fixed a rare crash when removing messages from system chat - ([bittiez](https://github.com/bittiez))
* Fixed a crash with invalid macros on creation - ([bittiez](https://github.com/bittiez))
* Fixed a race condition crash when attacking a mobile during logout - ([bittiez](https://github.com/bittiez))
* Added a few missing keys to imgui assistant hotkey listener - ([bittiez](https://github.com/bittiez))
* Fixed a crash when resetting map cache before folder exists - ([bittiez](https://github.com/bittiez))
* Fixed a bug in housing customization that places two tiles - ([bittiez](https://github.com/bittiez))
* Fix improved buff bar creeping up the screen on logins when logging out with buffs active - ([bittiez](https://github.com/bittiez))
* Fix vendor nameplates closing when auto sell agent sell something - ([bittiez](https://github.com/bittiez))
* Fix cursor alignment when using a char offset - ([bittiez](https://github.com/bittiez))
* Various bug fixes from CUO
* Bulletin board now only shows 9 messages instead of 11
* Fixes for Hide Hud feature(ImGui -> Myra) - ([bittiez](https://github.com/bittiez))
* Fix a crash when handling io input while loading the game - ([bittiez](https://github.com/bittiez))
* Fix for double clicks accidentally registering as two single clicks sometimes - ([bittiez](https://github.com/bittiez))
* Make renderedtext pool thread safe to prevent rare crashes where the returned value is null - ([bittiez](https://github.com/bittiez)) 
* Fix autoloot regex json export to support special characters - ([bittiez](https://github.com/bittiez))
* Fix drag select positioning when zooming in or out - ([bittiez](https://github.com/bittiez))
* Fixed quest arrow positioning - ([bittiez](https://github.com/bittiez))
* Fixed the occasional X button stuck after logging in - ([bittiez](https://github.com/bittiez))
* Fixed a crash when a server side gump fails to render text - ([bittiez](https://github.com/bittiez))


### Misc

* A `CHANGELOG.md` was added to the repository - ([bittiez](https://github.com/bittiez))
* `ApiUiNineSliceGump` `OnResize` de-bouncer - [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) ([yuval-po](https://github.com/yuval-po))
* Removed *Discord* integration - ([bittiez](https://github.com/bittiez))
* Updated PSL browser UI and backend - ([bittiez](https://github.com/bittiez))
* Move automatic py doc gen to tool usage - ([bittiez](https://github.com/bittiez))
* Added ibm-plex font to embedded fonts - ([bittiez](https://github.com/bittiez))
* Cleaned up a bunch of compile-time warnings - ([bittiez](https://github.com/bittiez))
* Only send metrics login once per session(Swapping chars won't count as additional logins) - ([bittiez](https://github.com/bittiez))
* Changed mobile movement to use packet receive time to determine mobile speed instead of fixed values - ([bittiez](https://github.com/bittiez))
* Added a voice to text option via Vosk - ([bittiez](https://github.com/bittiez))
* Added an option(enabled by default) to single click mobiles to set them as last target - ([bittiez](https://github.com/bittiez))
* Added a set last target macro - ([bittiez](https://github.com/bittiez))
* Added a toggle auto walk macro - ([bittiez](https://github.com/bittiez))
* Added optional quest arrow to tmap and sos bottles - ([bittiez](https://github.com/bittiez))
* Disabled automatic viewport resizing - ([bittiez](https://github.com/bittiez))
* Improved map loading performance thanks to @mandlar's research - ([bittiez](https://github.com/bittiez))
* Update in-game version history gump - ([bittiez](https://github.com/bittiez))

---
