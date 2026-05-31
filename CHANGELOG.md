# Changelog
All notable changes to TazUO will be recorded here.

---
## In Development

### Features
* Pet support for the bandage agent - [P.R 485](https://github.com/PlayTazUO/TazUO/pull/485) ([yuval-po](https://github.com/yuval-po))
* Allow dropping of items onto minimized grid containers - [P.R 487](https://github.com/PlayTazUO/TazUO/pull/487) ([yuval-po](https://github.com/yuval-po))

### Fixes
* Mouse wheel macros hijack scroll from shop gumps - [P.R 479](https://github.com/PlayTazUO/TazUO/pull/479) ([yuval-po](https://github.com/yuval-po))
* Grid container label missing updates - [P.R 487](https://github.com/PlayTazUO/TazUO/pull/487) ([yuval-po](https://github.com/yuval-po))

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
