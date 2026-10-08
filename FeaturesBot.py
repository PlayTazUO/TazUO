import requests
import random
import os

motd = []

def entry(name, desc, link):
    motd.append(
    f"""
```ini
[ {name} ]
```
> {desc}\n
See more -> <{link}>
    """
    )

def search_entry(name, desc, *extra_params):
    combined_extras = "+".join(extra_params)
    link = f"https://tazuo.org?q={combined_extras}"
    
    print(link)

    motd.append(
    f"""
```ini
[ {name} ]
```
> {desc}\n
See more -> <{link}>
    """
    )

entry("Background", "Did you know you can change the color of the background in TUO?. `Options->TazUO->Misc`", "https://tazuo.org/")
entry("System chat", "Did you know you can disable the system chat(the text on the left side of the screen) with TUO?. `Options->TazUO->Misc`", "https://tazuo.org/")
entry("Pet Scaling", "Did you know you can scale your pets down to not block so much of your screen?", "https://tazuo.org/wiki/pet-scaling")
entry("Auto Sell Agent", "Did you know you can setup items to automatically sell to vendors?", "https://tazuo.org/wiki/auto-sell-agent")
search_entry("Tooltip Overrides", "TUO added the ability to customize tooltips in almost any way you desire, make them easy to read specific to you!", "tooltip", "override")
entry("Fine tuned positioning", "You can hold Ctrl and drag a gump to get more precise positioning!", "https://tazuo.org/")
entry("Sound filters", "Don't like a sound? You can filter it out!", "https://tazuo.org/")
search_entry("Discord", "Did you know TazUO has discord features built directly into the client?", "discord")
entry("Game scaling", "Monitor too big? Eyes too old? Scale your entire game with a simple slider!", "https://tazuo.org/")
entry("Web Map", "TazUO allows you to see your map in your web browser.", "https://tazuo.org/wiki/web-map")
entry("Quick move", "TUO allows you to select multiple items and move them quickly and easily with `Alt + Left Click`.", "https://tazuo.org/wiki/tazuogrid-containers#quick-move")
search_entry("Launcher", "TUO has a launcher available to easily manage multiple profiles and check for updates to TazUO for you.", "launcher")
search_entry("Modern fonts!", "TUO has made it possible to use your own fonts in many places in the game, see more in wiki.", "fonts")
search_entry("Treasure map locator", "TUO made it easier than ever to locate treasure via treasure maps, see how on the wiki.", "treasure", "maps", "sos")
search_entry("Status Gumps", "TUO made it so you can have your status gump and health-bar gump open at the same time!", "status+gump")
search_entry("SOS locator", "TUO made it easy to sail the seas by decoding those cryptic coords given when opening an SOS.", "sos+locator")
search_entry("Server owners and utilizing the chat tab", "TUO added a separate tab in the journal for built in global chat(osi friendly, works on ServUO but most servers leave it disabled). See more about how to use this simple feature on your server.", "server+owners")
search_entry("PNG replacer", "TUO added the ability to replace in-game artwork with your own by using simple png files, no client modifications required.", "png+replacer")
search_entry("Nameplate health-bars", "TUO allows nameplates to be used as health-bars. More details on the wiki.", "nameplate", "healthbars")
search_entry("Spell icons", "TUO allows you to scale spell icons, and display linked hotkeys. See the wiki for details and instructions.", "miscellaneous+spell+icons")
search_entry("Macro buttons", "TUO added the ability to customize buttons with size, color and graphics for your macro buttons.", "macro+button=editor")
search_entry("Grid containers", "TUO added grid containers. Not related to grid loot built into CUO. Check out all the features of this in the wiki.", "grid+containers")
search_entry("Modern Journal", "TUO added a much more modern and advanced journal that replaced the original.", "journal")
search_entry("Item tooltip comparisons", "TUO added the ability to compare an item in a container to the item you have equipped in that slot by pressing `Ctrl` while hovering over the item.", "item+comparison")
search_entry("Info bar", "TUO added the ability to use text or built-in graphics for the info bar along with customizable font and size, see the wiki for screenshots and more details.", "info+bar")
search_entry("Hidden gump opacity", "Most gumps can have their opacity adjusted by holding `Ctrl + Alt` while scrolling over them.", "hidden+gump+features+opacity")
search_entry("Hidden gump lock", "Most gumps can be locked in place to prevent accidental movement or closing by `Ctrl + Alt + Left Click` the gump.", "hidden+gump+features+lock")
search_entry("Hidden Characters", "TUO added the option to customize what you look like while hidden with colors and opacity.", "hidden+characters")
search_entry("Health Lines", "TUO added the option to scale the size of health lines underneath mobiles.", "health+lines")
search_entry("Grid highlighting based on item properties", "TUO allows you to set up custom rules to highlight specific items in a grid container, allowing you to easily see the items that hold value to you.", "grid+highlight")
search_entry("Follow mode", "TUO modified the `alt + left click` to follow a player or npc, now you can adjust the follow distance and alt clicking their health-bar instead of the mobile themselves.", "follow+mode")
search_entry("Durability gump", "TUO added a new durability gump to easily track durability of items, see the wiki for screenshots and more details.", "durability+gump")
search_entry("Drag select modifiers", "TUO added a few optional key modifiers to the drag select(for opening many health-bars at once). See the wiki for a more detailed explanation.", "drag+select+modifiers")
search_entry("Damage number hues", "In TUO you can customize the colors for different types of damage numbers on screen.", "damage+number+hues")
search_entry("Custom health-bar additions", "TUO further enhanced the custom health-bars with distance indicators, see the wiki for screenshots and more details.", "custom+health+bar")
search_entry("Customizable cooldown bars", "TUO added customizable cool down bars that can be triggered on the text of your choosing, be sure to see the wiki for screenshots and instructions.", "cooldown+bars")
search_entry("Marktile command", "TUO added a `-marktile` command to highlight specific places in game on the ground. Screenshots and more details available on the wiki.", "commands+marktile")
search_entry("Skill command", "TUO added a `-skill SKILLNAME` command to easily use skills.", "commands+skill")
search_entry("Cast command", "TUO added a `-cast SPELLNAME` command to easily cast spells.", "commands+cast")
search_entry("Circle of transparency", "TUO has added a new type of circle of transparency and increased the max radius from 200 -> 1000.", "circle+of+transparency")
search_entry("Improved buff bar", "TUO has an improved buff bar with a customizable progress bar letting you easily see when that buff will expire.", "buff+bars")
search_entry("Alternate paperdoll", "TUO has a more modern alternative to the original paperdoll gump.", "alternate+paperdoll")
search_entry("Account selector", "A simple right-click on the account input of the login screen will bring up an option to select other accounts you have logged into.", "account+selector")
search_entry("Skill progress bar", "TUO added progress bars when your skills increase or decrease.", "skill+progress+bar")
search_entry("-radius command", "TUO added an easy way to see a precise radius around you, see the wiki for screenshots and instructions.", "commands+radius")
search_entry("-colorpicker command", "TUO added an easy way to browse colors in your UO install, try it out.", "commands+colorpicker")
search_entry("Sound overrides", "TUO allows you to easily override sounds without modifying your client.", "sound+override")
search_entry("Journal Entries", "TUO allows you to adjust how many journal entries to save, from 200-2000.", "journal")
search_entry("Spell casting indicators", "TUO added a very customizable spell casting indicator system, including displaying range, cast time, and area size.", "spell+indicators")
search_entry("Advance nameplate options", "TUO added a very customizable nameplate system.", "nameplate+options")
search_entry("Simple auto loot", "TUO added a simple auto loot feature in `3.10.0`, check out the wiki for more info. `Options->TazUO->Misc`", "auto+loot")
search_entry("Health indicator", "TUO added a simple health indicator(red border around the window) to more easily notice when your health drops. `Options->TazUO->Misc`", "miscellaneous+health")
entry("Quick drop", "With TUO you can hold Ctrl and drop an item anywhere on your game window to try to drop it at your feet.", "https://tazuo.org/")
entry("Screenshots", "With TUO you can press `Ctrl + Printscreen` to take a screenshot of just the gump you are hovering over.", "https://tazuo.org/")
entry("Item comparisons", "TUO allows you to compare item tooltips side-by-side by pressing `ctrl` while hovering over an item in your grid container.", "https://tazuo.org/")
entry("Party members", "TazUO added an option to color your party members so you can more easily see who is on your team.", "https://tazuo.org/")
entry("Auto Follow", "TazUO improved auto follow by making frozen/paralyzed status not cancel auto follow in addition to customizable follow range from the target.", "https://tazuo.org/")
search_entry("Language translations", "TazUO allows you to add additional language files.", "language", "translation")
search_entry("Python Scripting", "TazUO added built-in python scripting to the client.", "legion+scripting")
entry("Client commands", "TazUO added a gump to show you available client commands. This can be opened from the top menu bar -> more -> Client Commands.", "https://tazuo.org/")
entry("Damage numbers in your journal", "You can add dmg numbers to a journal tab(Right click the tab) to see damage numbers in the journal.", "https://tazuo.org/")
entry("Quick Spell Cast Gump", "TazUO added a simple gump to easily search for and cast spells from. Top Menu -> More -> Tools -> Quick spell cast.", "https://tazuo.org/")
search_entry("Auto Bandage", "TazUO added auto bandaging to keep you healed.", "auto+bandage")
search_entry("Profile backups", "TazUO backs up your profiles 3 times, just in-case.", "profile+backups")
search_entry("Toggle hud visibility", "You can quickly hide/show your gumps on screen to keep your screen de-cluttered.", "hide+hud")
search_entry("Scavenger", "TazUO added a scavenger to pick up those pesky items on the ground.", "scavenger")
search_entry("Dress Agent", "TazUO added a dress agent to protect everyone else from your bad outfits.", "dress+agent")
search_entry("Organizer Agent", "TazUO added an organizer agent to help clean up your mess.", "organizer+agent")
search_entry("Graphic Filter", "You can swap graphics of mobiles, items and terrain from the assistant.", "graphic", "filter")
search_entry("Journal Filter", "You can filter out specific messages from the journal, keeping your journal cleaner.", "journal", "filter")
search_entry("Season Filter", "You can swap out seasons for another style that may suite your eyes better.", "season", "filter")
search_entry("Map Files", "We have a guide for creating, organizing and managing map marker files.", "map", "file")
search_entry("Notable Options", "Check out our list of notable options, these few settings may make a world of difference.", "notable", "options")
search_entry("Post Processing", "We have a few options you can try out for post processing effects.", "post", "processing")
search_entry("Cave Tiles", "Want to see a border around cave tiles for easier mining?.", "cave", "tile")
search_entry("Controller", "Have you tried playing with your controller yet?.", "controller", "support")
search_entry("Health Bar Collector", "Easily see all nearby mobiles with the health bar collector.", "health", "bar", "collector")
entry("Options menu fine controls", "Did you know, in the options menu if you are trying to set a slider to an exact number, you can hover over the slider and use your left/right arrow keys?.", "https://tazuo.org/")
search_entry("Ingame Tools", "There are a few ingame tools built to make your time in UO easier.", "ingame", "tools")
search_entry("Auto stat lock", "Help manager your stats while building your char.", "stat", "lock")
search_entry("Auto Buy Agent", "TUO added an auto buy agent so you can automatically purchase items from vendors.", "auto", "buy", "agent")
search_entry("Counter Bar", "TUO added a counter bar to keep an eye on counts of items, spells and more at a glance.", "counter", "bar")
search_entry("Item Database", "TUO records every item it sees into a searchable database so you can look up anything later.", "item", "database")
search_entry("Nearby Items", "TUO has an overlay that shows nearby ground items with one-click loot and use.", "nearby", "items")
search_entry("Nearby Loot", "TUO added a nearby loot window to quickly loot corpses and containers around you.", "nearby", "loot")
search_entry("Multi-Move", "TUO added a multi-move window to bulk move items to a container, mobile or trade window.", "multi", "move")
search_entry("Friends List", "TUO added a friends list used for highlighting, health bars and bandage targeting.", "friendslist")
search_entry("Tinkerer", "TUO has a built in developer window(tinkerer) to browse clilocs, hues, art, textures, gumps and sounds.", "tinkerer")
search_entry("Title Bar Status", "TUO can show your HP, mana and stamina in the window title as text, percent or an ascii bar.", "title", "bar", "status")
search_entry("Voice to Text", "TUO can turn your speech into in-game chat with voice to text.", "voice", "to", "text")
search_entry("Weather Effects", "TUO added enhanced weather effects such as rain/snow splashes and ripples.", "weather")
search_entry("Visual Effects and Overlays", "TUO added configurable full-screen overlay layers and screen-shake effects.", "visual", "effects")
search_entry("Polls", "You can view and vote on community polls right in the client.", "polls")
search_entry("Advanced Shop Gump", "TUO has a modernized vendor buy/sell window.", "advanced", "shop", "gump")
search_entry("Trade Gump", "TUO has a modernized player-to-player trade window.", "trade", "gump")
search_entry("Transparent Houses", "TUO can force house transparency with configurable opacity and hue.", "transparent", "houses")
search_entry("World Map", "TUO has a resizable world map with markers, pathfinding, zone files and sextant coordinates.", "world", "map")
search_entry("XML Gumps", "Servers can define custom gumps that TUO renders client-side without a client mod.", "xml", "gumps")
search_entry("Hotkeys", "TUO has a centralized hotkey window where you can find and rebind nearly everything.", "hotkeys")
search_entry("Custom Macros", "TUO allows creating and editing macros of every type from the macro window.", "macros")
search_entry("Customizing Gumps", "You can anchor gumps and their positions persist between sessions.", "customizing", "gumps")
search_entry("Login and Character Select", "TUO improved the login, server select and character select screens.", "login", "and", "character", "select")
search_entry("Public Script Library", "You can browse and download community scripts from the public script library browser.", "public", "script", "library")
search_entry("C# Scripting", "TUO supports C# scripting in addition to python scripting.", "csharp", "scripting")
search_entry("Packet Logger", "TUO can hex-dump network packets to help debug or develop.", "packet", "logger")
search_entry("Command Line Arguments", "You can start TUO with command line arguments to control how it launches.", "commandline", "arguments")
search_entry("Different setting files", "You can run multiple settings/profiles from a single TUO install.", "different", "setting", "files")
search_entry("Asset Zip", "You can load your own custom PNG assets from a tuoassets.zip file.", "asset", "zip")
search_entry("OSI Support", "TUO has an option set for OSI/official server behavior.", "osi", "support")
search_entry("Long Messages", "TUO handles unusually long chat and journal messages gracefully.", "long", "messages")
entry("Self Heal Agent", "TUO added a self heal agent, just hold a key to heal or cure yourself with magery or chivalry.", "https://tazuo.org/")
entry("Auto Skinning", "TUO can automatically skin corpses with a knife as you open them.", "https://tazuo.org/")
entry("Auto Unequip", "TUO can automatically unequip gear for casting spells or drinking potions.", "https://tazuo.org/")
entry("Last Equipment on Paperdoll", "Paperdolls remember the last equipment seen for out-of-range characters.", "https://tazuo.org/")
entry("Advanced Skills Gump", "TUO has an advanced sortable skills gump with custom skill groups.", "https://tazuo.org/")
entry("Inspector Gump", "TUO has an inspector gump that exposes raw properties of game objects.", "https://tazuo.org/")
entry("Modern Color Picker", "TUO has a modern color picker for browsing hues and RGB colors.", "https://tazuo.org/")
entry("Modern Book Gump", "TUO has an improved book gump for reading and writing.", "https://tazuo.org/")
entry("Boat Control", "TUO has on-screen boat controls and smooth boat movement.", "https://tazuo.org/")
entry("Action Bars", "TUO added multiple named, freely-placeable action bars.", "https://tazuo.org/")
entry("Aura Indicators", "TUO can draw configurable auras under your feet, on your cursor and for party members.", "https://tazuo.org/")
entry("Sallos Easy Grab", "TUO has single-click ground item grabbing similar to the Sallos client.", "https://tazuo.org/")
entry("Container Customization", "You can customize container styles, corpse styles, scaling and positions.", "https://tazuo.org/")
entry("Object and Text Fading", "You can fade distant objects and text for a cleaner view of the world.", "https://tazuo.org/")
entry("Location Go-To", "Enter raw or sextant coordinates in the world map to travel to a location.", "https://tazuo.org/")
entry("Pathfinding Tuning", "You can tune pathfinding with Z-level, max nodes, house buffer and timeout settings.", "https://tazuo.org/")


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
