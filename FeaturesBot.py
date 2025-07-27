import requests
import random
import os

motd = [
"""
```ini
[ Profile backups ]
```
> TazUO backs up your profiles 3 times, just in-case. \n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Profile-Backups>
""",

"""
```ini
[ Toggle hud visibility ]
```
> You can quickly hide/show your gumps on screen to keep your screen de-cluttered. \n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Hide-Hud>
""",

"""
```ini
[ Damage numbers in your journal ]
```
> You can add dmg numbers to a journal tab(Right click the tab) to see damage numbers in the journal. \n
""",

"""
```ini
[ Spell bar ]
```
> TazUO added a spell bar to easily manage, store, and cast spells via hotkey or click. \n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.SpellBar>
""",

"""
```ini
[ Quick Spell Cast Gump ]
```
> TazUO added a simple gump to easily search for and cast spells from. \n
Top Menu -> More -> Tools -> Quick spell cast
""",

"""
```ini
[ Python Scripting ]
```
> TazUO added built-in pthon scripting to the client. \n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Legion-Scripting>
""",

"""
```ini
[ Legion Scripting ]
```
> TazUO added a custom scripting language similar to UOSteam built directly into the client. \n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Legion-Scripting>
""",

"""
```ini
[ Client commands ]
```
> TazUO added a gump to show you available client commands. This can be opened from the top menu bar -> more -> Client Commands.
""",

"""
```ini
[ Party members ]
```
> TazUO added an option to color your party members so you can more easily see who is on your team.
""",

"""
```ini
[ Auto Follow ]
```
> TazUO improved auto follow by making frozen/paralyzed status not cancel auto follow in addition to customizable follow range from the target.
""",

"""
```ini
[ Language translations ]
```
> TazUO recently starting placing client-side text in a language.json file for users to easily translate the client into their preferred language.
""",

"""
```ini
[ Quick drop ]
```
> With TUO you can hold Ctrl and drop an item anywhere on your game window to try to drop it at your feet.
""",

"""
```ini
[ Screenshots ]
```
> With TUO you can press `Ctrl + Printscreen` to take a screenshot of just the gump you are hovering over.
""",

"""
```ini
[ Advance nameplate options ]
```
> TUO added a very customizable nameplate system.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Nameplate-options>
""",

"""
```ini
[ Spell casting indicators ]
```
> TUO added a very customizable spell casting indicator system, including displaying range, cast time, and area size.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Spell-Indicators>
""",

"""
```ini
[ Item comparisons ]
```
> TUO allows you to compare item tooltips side-by-side by pressing `ctrl` while hovering over an item in your grid container.\n
""",

"""
```ini
[ Simple auto loot ]
```
> TUO added a simple auto loot feature in `3.10.0`, check out the wiki for more info.\n
`Options->TazUO->Misc` | <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Simple-Auto-Loot>
""",

"""
```ini
[ Health indicator ]
```
> TUO added a simple health indicator(red border around the window) to more easily notice when your health drops.\n
`Options->TazUO->Misc` | <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Miscellaneous#health-indicator>
""",

"""
```ini
[ Background ]
```
> Did you know you can change the color of the background in TUO?.\n
`Options->TazUO->Misc`
""",

"""
```ini
[ System chat ]
```
> Did you know you can disable the system chat(the text on the left side of the screen) with TUO?.\n
`Options->TazUO->Misc`
""",

"""
```ini
[ Journal Entries ]
```
> TUO allows you to adjust how many journal entries to save, from 200-2000.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Journal>
""",

"""
```ini
[ Sound overrides ]
```
> TUO allows you to easily override sounds without modifying your client.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TUO.Sound-Override>
""",

"""
```ini
[ -colorpicker command ]
```
> TUO added an easy way to browse colors in your UO install, try it out.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Commands#-colorpicker>
""",

"""
```ini
[ -radius command ]
```
> TUO added an easy way to see a precise radius around you, see the wiki for screenshots and instructions.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Commands#-radius>
""",

"""
```ini
[ Skill progress bar ]
```
> TUO added progress bars when your skills increase or decrease.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Skill-Progress-Bar>
""",

"""
```ini
[ Account selector ]
```
> A simple right-click on the account input of the login screen will bring up an option to select other accounts you have logged into.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Account-Selector>
""",

"""
```ini
[ Alternate paperdoll ]
```
> TUO has a more mordern alternative to the original paperdoll gump.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Alternate-Paperdoll>
""",

"""
```ini
[ Improved buff bar ]
```
> TUO has an improved buff bar with a customizable progress bar letting you easily see when that buff will expire.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Buff-Bars>
""",

"""
```ini
[ Circle of transparency ]
```
> TUO has added a new type of circle of transparency and increased the max radius from 200 -> 1000.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Circle-of-transparency>
""",

"""
```ini
[ Cast command ]
```
> TUO added a `-cast SPELLNAME` command to easily cast spells.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Commands#-cast-spellname>
""",

"""
```ini
[ Skill command ]
```
> TUO added a `-skill SKILLNAME` command to easily use skills.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Commands#-skill-skillname>
""",

"""
```ini
[ Marktile command ]
```
> TUO added a `-marktile` command to highlight specific places in game on the ground. Screenshots and more details available on the wiki.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Commands#-marktile>
""",

"""
```ini
[ Customizable cooldown bars ]
```
> TUO added customizable cool down bars that can be triggered on the text of your choosing, be sure to see the wiki for screenshots and instructions.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Cooldown-bars>
""",

"""
```ini
[ Custom healthbar additions ]
```
> TUO further enhanced the custom healthbars with distance indicators, see the wiki for screenshots and more details.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Custom-Health-Bar>
""",

"""
```ini
[ Damage number hues ]
```
> In TUO you can customize the colors for different types of damage numbers on screen.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Damage-number-hues>
""",

"""
```ini
[ Drag select modifiers ]
```
> TUO added a few optional key modifiers to the drag select(for opening many healthbars at once). See the wiki for a more detailed explanation.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Drag-Select-Modifiers>
""",

"""
```ini
[ Durability gump ]
```
> TUO added a new durability gump to easily track durability of items, see the wiki for screenshots and more details.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Durability-Gump>
""",

"""
```ini
[ Follow mode ]
```
> TUO modified the `alt + left click` to follow a player or npc, now you can adjust the follow distance and alt clicking their healthbar instead of the mobile themselves.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Follow-mode>
""",

"""
```ini
[ Grid highlighting based on item properties ]
```
> TUO allows you to set up custom rules to highlight specific items in a grid container, allowing you to easily see the items that hold value to you.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-highlighting-based-on-item-properties>
""",

"""
```ini
[ Health Lines ]
```
> TUO added the option to scale the size of health lines underneath mobiles.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Health-Lines>
""",

"""
```ini
[ Hidden Characters ]
```
> TUO added the option to customize what you look like while hidden with colors and opacity.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Hidden-Characters>
""",

"""
```ini
[ Hidden gump lock ]
```
> Most gumps can be locked in place to prevent accidental movement or closing by `Ctrl + Alt + Left Click` the gump.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Hidden-Gump-Features#hidden-gump-lock>
""",

"""
```ini
[ Hidden gump opacity ]
```
> Most gumps can have their opacity adjusted by holding `Ctrl + Alt` while scrolling over them.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Hidden-Gump-Features#ctrl--alt--mouse-wheel-opacity-adjustment>
""",

"""
```ini
[ Info bar ]
```
> TUO added the ability to use text or built-in graphics for the info bar along with customizable font and size, see the wiki for screenshots and more details.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Info-Bar>
""",

"""
```ini
[ Item tooltip comparisons ]
```
> TUO added the ability to compare an item in a container to the item you have equiped in that slot by pressing `Ctrl` while hovering over the item.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Item-Comparison>
""",

"""
```ini
[ Modern Journal ]
```
> TUO added a much more modern and advanced journal that replaced the original.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Journal>
""",

"""
```ini
[ Grid containers ]
```
> TUO added a small feature called grid containers. Not related to grid loot built into CUO. Check out all the features of this in the wiki.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-Containers>
""",

"""
```ini
[ Macro buttons ]
```
> TUO added the ability to customize buttons with size, color and graphics for your macro buttons.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Macro-Button-Editor>
""",

"""
```ini
[ Spell icons ]
```
> TUO allows you to scale spell icons, and display linked hotkeys. See the wiki for details and instructions.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Miscellaneous#spell-icons>
""",

"""
```ini
[ Nameplate healthbars ]
```
> TUO allows nameplates to be used as healthbars. More details on the wiki.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Nameplate-Healthbars>
""",

"""
```ini
[ PNG replacer ]
```
> TUO added the ability to replace in-game artwork with your own by using simple png files, no client modifications required.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.PNG-Replacer>
""",

"""
```ini
[ Server owners and utilizing the chat tab ]
```
> TUO added a seperate tab in the journal for built in global chat(osi friendly, works on ServUO but most servers leave it disabled). See more about how to use this simple feature on your server.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Server-Owners>
""",

"""
```ini
[ SOS locator ]
```
> TUO made it easy to sail the seas by decoding those cryptic coords given when opening an SOS.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.SOS-Locator>
""",

"""
```ini
[ Status Gumps ]
```
> TUO made it so you can have your status gump and healthbar gump open at the same time!\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Status-Gump>
""",

"""
```ini
[ Tooltip overrides ]
```
> TUO added the ability to customize tooltips in almost any way you desire, make them easy to read specific to you!\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Tooltip-Override>
""",

"""
```ini
[ Treasure map locator ]
```
> TUO made it easier than ever to locate treasure via treasure maps, see how on the wiki.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Treasure-Maps-&-SOS>
""",

"""
```ini
[ Modern fonts! ]
```
> TUO has made it possible to use your own fonts in many places in the game, see more in wiki.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.TTF-Fonts>
""",

"""
```ini
[ Launcher ]
```
> TUO has a launcher available to easily manage multiple profiles and check for updates to TazUO for you.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Updater-Launcher>
""",

"""
```ini
[ Quick move ]
```
> TUO allows you to select multiple items and move them quickly and easily with `Alt + Left Click`.\n
See more -> <https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-Containers#quick-move>
""",
]

url = os.getenv("DISCORD_WEBHOOK")
if not url:
    raise ValueError("DISCORD_WEBHOOK environment variable not set.")

data = {
    "content" : random.choice(motd)
}

result = requests.post(url, json = data)

try:
    result.raise_for_status()
except requests.exceptions.HTTPError as err:
    print(err)
else:
    print("Payload delivered successfully, code {}.".format(result.status_code))
