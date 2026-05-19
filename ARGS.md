# TazUO Command-Line Arguments

All arguments are prefixed with `-` (single dash). These override values loaded from `settings.json` and take higher priority.

## Usage

```
ClassicUO.Client [options]
```

## Arguments

| Argument | Value Type | Description |
|---|---|---|
| `-settings` | filepath | Path to a custom settings file instead of default `settings.json` |
| `-highdpi` | *(flag)* | Enable high-DPI display support |
| `-username` | string | Override login username |
| `-password` | string | Override login password (plaintext, will be encrypted internally) |
| `-password_enc` | string | Override login password (already encrypted) |
| `-ip` | string | Override server IP address |
| `-port` | ushort | Override server port number |
| `-filesoverride` | filepath | Override path for UO data files |
| `-uofilesoverride` | filepath | Alias for `-filesoverride` |
| `-ultimaonlinedirectory` | filepath | Path to the Ultima Online installation directory |
| `-uopath` | filepath | Alias for `-ultimaonlinedirectory` |
| `-profilespath` | filepath | Path to load/save profiles |
| `-clientversion` | string (e.g. `7.0.59.8`) | Client version to use |
| `-lastcharactername` | string | Override the last character name used for auto-login |
| `-lastcharname` | string | Alias for `-lastcharactername` |
| `-lastservernum` | ushort | Override the last server number |
| `-last_server_name` | string | Override the last server name |
| `-fps` | int | Target FPS (clamped to `MIN_FPS`–`MAX_FPS`) |
| `-debug` | *(flag)* | Enable debug mode |
| `-profiler` | bool or *(flag)* | Enable the profiler (`true`/`false`, or omit value to enable) |
| `-saveaccount` | bool | Whether to save account credentials |
| `-autologin` | bool | Enable/disable auto-login |
| `-reconnect` | bool | Enable/disable auto-reconnect |
| `-reconnect_time` | int | Reconnect interval in ms (minimum 1000) |
| `-login_music` | bool | Enable/disable login screen music |
| `-music` | bool | Alias for `-login_music` |
| `-login_music_volume` | int | Login music volume level |
| `-music_volume` | int | Alias for `-login_music_volume` |
| `-fixed_time_step` | bool | Enable/disable fixed time step |
| `-skiploginscreen` | *(flag)* | Skip the login screen entirely |
| `-plugins` | comma-separated | Plugin list to load |
| `-use_verdata` | bool | Enable/disable verdata.mul usage |
| `-maps_layouts` | string | Custom map layout configuration |
| `-encryption` | byte | Encryption mode |
| `-force_driver` | byte | Graphics driver: `0` = default, `1` = OpenGL, `2` = Vulkan |
| `-packetlog` | optional hex IDs | Enable packet logging; optionally filter by comma-separated packet IDs (e.g. `0x1A,0x77`) |
| `-language` | string | Language code (see below) |
| `-no_server_ping` | *(flag)* | Disable server ping |
| `-zlib` | *(flag)* | Force managed zlib implementation |

## Language Codes

| Code | Language |
|---|---|
| `ENU` | English |
| `RUS` | Russian |
| `FRA` | French |
| `DEU` | German |
| `ESP` | Spanish |
| `JPN` | Japanese |
| `KOR` | Korean |
| `PTB` | Portuguese (Brazil) |
| `ITA` | Italian |
| `CHT` | Chinese Traditional |

## Flag-Only Arguments

These arguments take no value:

- `-highdpi`
- `-debug`
- `-skiploginscreen`
- `-no_server_ping`
- `-zlib`

## Examples

```
# Connect to a specific server and auto-login
ClassicUO.Client -ip 127.0.0.1 -port 2593 -username player1 -password secret -autologin true

# Use a custom UO directory with Vulkan renderer
ClassicUO.Client -uopath "C:\Games\Ultima Online" -force_driver 2

# Enable debug mode and packet logging
ClassicUO.Client -debug -packetlog 0x1A,0x77

# Use a custom settings file and skip the login screen
ClassicUO.Client -settings "C:\my_settings.json" -skiploginscreen
```
