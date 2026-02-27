# Changelog
All notable changes to TazUO will be recorded here.

---

## Currently in `dev channel`

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

### API

* Added *Sound* APIs to for `Legion Scripting` - [P.R 362](https://github.com/PlayTazUO/TazUO/pull/362) ([fpw](https://github.com/fpw))
* Added `API.PickUpToCursor`, `API.DropFromCursor` and `API.GetHeldItem` - ([bittiez](https://github.com/bittiez))
* Added `IsHidden`, `IsGargoyle`, `IsMounted`, `IsDrivingBoat`, and `IsRunning` to `ApiMobile` - ([bittiez](https://github.com/bittiez))
* Added `API.ScriptName` and `API.ScriptPath` - ([bittiez](https://github.com/bittiez))
* Added missing API documentation types - [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369), [P.R 370](https://github.com/PlayTazUO/TazUO/pull/370), [P.R 371](https://github.com/PlayTazUO/TazUO/pull/371) ([yuval-po](https://github.com/yuval-po))
* Add `API.GetPartyLeader()` - ([bittiez](https://github.com/bittiez))

### Assistant

* Added a *Skill Management* tab to the *Legion Assistant* - [P.R 359](https://github.com/PlayTazUO/TazUO/pull/359) ([crameep](https://github.com/crameep))
* Organizer tab now shows graphic when hovering over the graphic art - ([bittiez](https://github.com/bittiez))
* Added Mobile outline option - Highlighting mobiles by notoriety - ([bittiez](https://github.com/bittiez))
* Added TazUO chat (Top menu -> More -> TazUO Chat) - ([bittiez](https://github.com/bittiez))
* ItemDatabase search now defaults to not only "this character" - ([bittiez](https://github.com/bittiez))


### Fixes

* Fixed empty ability name on active ability when calling `CurrentAbilityNames` - [P.R 373](https://github.com/PlayTazUO/TazUO/pull/373) ([yuval-po](https://github.com/yuval-po))
* Fixed automatic corpse opening when too far away - [P.R 371](https://github.com/PlayTazUO/TazUO/pull/371) ([yuval-po](https://github.com/yuval-po))
* Fixed a reliability issue with `API.OnHotKey` - [P.R 365](https://github.com/PlayTazUO/TazUO/pull/365) ([fpw](https://github.com/fpw))
* Fixed healthbar collector occasionally becoming unresponsive to targeting/clicks - ([bittiez](https://github.com/bittiez))
* Fixed a rare crash when removing messages from system chat - ([bittiez](https://github.com/bittiez))
* Fixed a crash with invalid macros on creation

### Misc

* A `CHANGELOG.md` was added to the repository - ([bittiez](https://github.com/bittiez))
* `ApiUiNineSliceGump` `OnResize` de-bouncer - [P.R 369](https://github.com/PlayTazUO/TazUO/pull/369) ([yuval-po](https://github.com/yuval-po))
* Removed *Discord* integration - ([bittiez](https://github.com/bittiez))
* Updated PSL browser UI and backend - ([bittiez](https://github.com/bittiez))
* Move automatic py doc gen to tool usage - ([bittiez](https://github.com/bittiez))
* Added ibm-plex font to embedded fonts - ([bittiez](https://github.com/bittiez))
* Cleaned up a bunch of compile-time warnings - ([bittiez](https://github.com/bittiez))


---