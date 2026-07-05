# Changelog

All notable changes to Legacy TazUO will be documented in this file.

---
## [4.5.23]
- Fixed an IndexOutOfRangeException crash when the mouse hovered over items/corpses with a graphic id outside the animation data index bounds

## [4.5.22]
- More fixes for EA publish

## [4.5.21]
- Fixed an issue with recent animation loading causing a crash after latest EA publish

## [4.5.20]
- Only send login metric once per session(Swapping chars won't count as additional logins until client is closed/reopened)

### Fixes
- Fixed a crash in legion API when setting display range during logout

## [4.5.19]

### Fixes
- Crash fix when checking buffs in API on client logout
- Ensure metrics isn't sending account names in server name(Likely by connecting to stealth, also added server-side prevention)