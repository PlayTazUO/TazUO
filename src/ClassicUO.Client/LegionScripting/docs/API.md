---
title: Python API Documentation
description: Automatically generated documentation for the Python API scripting system
tableOfContents:
  minHeadingLevel: 1
  maxHeadingLevel: 4
---

This is automatically generated documentation for the Python API scripting.  

:::note[Usage]
All methods, properties, enums, etc need to pre prefaced with `API.` for example:
 `API.Msg("An example")`.
:::

:::tip[API.py File]
If you download the [API.py](https://github.com/PlayTazUO/TazUO/blob/dev/src/ClassicUO.Client/LegionScripting/docs/API.py) file, put it in the same folder as your python scripts and add `import API` to your script, that will enable some mild form of autocomplete in an editor like VS Code.  

You can now type `-updateapi` in game to download the latest API.py file.
:::

[Additional notes](../notes/)  

*This was generated on `10/31/25`.*

## Properties
### `JournalEntries`

**Type:** `ConcurrentQueue<PyJournalEntry>`

### `Backpack`

**Type:** `uint`

 Get the player's backpack serial


### `Player`

**Type:** `PyPlayer`

 Returns the player character object


### `Bank`

**Type:** `uint`

 Return the player's bank container serial if open, otherwise 0


### `Random`

**Type:** `Random`

 Can be used for random numbers.
 `API.Random.Next(1, 100)` will return a number between 1 and 100.
 `API.Random.Next(100)` will return a number between 0 and 100.


### `LastTargetSerial`

**Type:** `uint`

 The serial of the last target, if it has a serial.


### `LastTargetPos`

**Type:** `Vector3Int`

 The last target's position


### `LastTargetGraphic`

**Type:** `ushort`

 The graphic of the last targeting object


### `Found`

**Type:** `uint`

 The serial of the last item or mobile from the various findtype/mobile methods



### `PyProfile`

**Type:** `PyProfile`

 Access useful player settings.


### `Events`

**Type:** `Events`

### `StopRequested`

**Type:** `bool`

 Check if the script has been requested to stop.
 ```py
 while not API.StopRequested:
   DoSomeStuff()
 ```


### `CancellationToken`

**Type:** `CancellationTokenSource`


## Enums
### ScanType

**Values:**
- `Hostile`
- `Party`
- `Followers`
- `Objects`
- `Mobiles`

### Notoriety

**Values:**
- `Unknown`
- `Innocent`
- `Ally`
- `Gray`
- `Criminal`
- `Enemy`
- `Murderer`
- `Invulnerable`

### PersistentVar

**Values:**
- `Char`
- `Account`
- `Server`
- `Global`


## Methods
### ProcessCallbacks

 Use this when you need to wait for players to click buttons.
 Example:
 ```py
 while True:
   API.ProcessCallbacks()
   API.Pause(0.1)
 ```


**Return Type:** `void` *(Does not return anything)*

---

### Dispose

**Return Type:** `void` *(Does not return anything)*

---

### OnHotKey
`(key, callback)`
 Register or unregister a Python callback for a hotkey.
 ### Register:
 ```py
 def on_shift_a():
     API.SysMsg("SHIFT+A pressed!")
 API.OnHotKey("SHIFT+A", on_shift_a)
 while True:
   API.ProcessCallbacks()
   API.Pause(0.1)
 ```
 ### Unregister:
 ```py
 API.OnHotKey("SHIFT+A")
 ```
 The <paramref name="key"/> can include modifiers (CTRL, SHIFT, ALT),
 for example: "CTRL+SHIFT+F1" or "ALT+A".


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `key` | `string` | ❌ No | Key combination to listen for, e.g. "CTRL+SHIFT+F1". |
| `callback` | `object` | ✅ Yes | Python function to invoke when the hotkey is pressed.  
         If <c>null</c> , the hotkey will be unregistered. |

**Return Type:** `void` *(Does not return anything)*

---

### SetSharedVar
`(name, value)`
 Set a variable that is shared between scripts.
 Example:
 ```py
 API.SetSharedVar("myVar", 10)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | Name of the var |
| `value` | `object` | ❌ No | Value, can be a number, text, or *most* other objects too. |

**Return Type:** `void` *(Does not return anything)*

---

### GetSharedVar
`(name)`
 Get the value of a shared variable.
 Example:
 ```py
 myVar = API.GetSharedVar("myVar")
 if myVar:
  API.SysMsg(f"myVar is {myVar}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | Name of the var |

**Return Type:** `object`

---

### RemoveSharedVar
`(name)`
 Try to remove a shared variable.
 Example:
 ```py
 API.RemoveSharedVar("myVar")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | Name of the var |

**Return Type:** `void` *(Does not return anything)*

---

### ClearSharedVars

 Clear all shared vars.
 Example:
 ```py
 API.ClearSharedVars()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### CloseGumps

 Close all gumps created by the API unless marked to remain open.


**Return Type:** `void` *(Does not return anything)*

---

### Attack
`(serial)`
 Attack a mobile
 Example:
 ```py
 enemy = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
 if enemy:
   API.Attack(enemy)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### SetWarMode
`(enabled)`
 Sets the player's war mode state (peace/war toggle).


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `enabled` | `bool` | ❌ No | True to enable war mode, false to disable war mode |

**Return Type:** `void` *(Does not return anything)*

---

### BandageSelf

 Attempt to bandage yourself. Older clients this will not work, you will need to find a bandage, use it, and target yourself.
 Example:
 ```py
 if player.HitsMax - player.Hits > 10 or player.IsPoisoned:
   if API.BandageSelf():
     API.CreateCooldownBar(delay, "Bandaging...", 21)
     API.Pause(8)
   else:
     API.SysMsg("WARNING: No bandages!", 32)
     break
 ```


**Return Type:** `bool`

---

### ClearLeftHand

 If you have an item in your left hand, move it to your backpack
 Sets API.Found to the item's serial.
 Example:
 ```py
 leftHand = API.ClearLeftHand()
 if leftHand:
   API.SysMsg("Cleared left hand: " + leftHand.Name)
 ```


**Return Type:** `PyItem`

---

### ClearRightHand

 If you have an item in your right hand, move it to your backpack
 Sets API.Found to the item's serial.
 Example:
 ```py
 rightHand = API.ClearRightHand()
 if rightHand:
   API.SysMsg("Cleared right hand: " + rightHand.Name)
  ```


**Return Type:** `PyItem`

---

### ClickObject
`(serial)`
 Single click an object
 Example:
 ```py
 API.ClickObject(API.Player)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial, or item/mobile reference |

**Return Type:** `void` *(Does not return anything)*

---

### UseObject
`(serial, skipQueue)`
 Attempt to use(double click) an object.
 Example:
 ```py
 API.UseObject(API.Backpack)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | The serial |
| `skipQueue` | `bool` | ✅ Yes | Defaults true, set to false to use a double click queue |

**Return Type:** `void` *(Does not return anything)*

---

### Contents
`(serial)`
 Get an item count for the contents of a container
 Example:
 ```py
 count = API.Contents(API.Backpack)
 if count > 0:
   API.SysMsg(f"You have {count} items in your backpack")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `int`

---

### ContextMenu
`(serial, entry)`
 Send a context menu(right click menu) response.
 This does not open the menu, you do not need to open the menu first. This handles both in one action.
 Example:
 ```py
 API.ContextMenu(API.Player, 1)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `entry` | `ushort` | ❌ No | Entries start at 0, the top entry will be 0, then 1, 2, etc. (Usually) |

**Return Type:** `void` *(Does not return anything)*

---

### EquipItem
`(serial)`
 Attempt to equip an item. Layer is automatically detected.
 Example:
 ```py
 lefthand = API.ClearLeftHand()
 API.Pause(2)
 API.EquipItem(lefthand)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### ClearMoveQueue

 Clear the move item que of all items.


**Return Type:** `void` *(Does not return anything)*

---

### QueueMoveItem
`(serial, destination, amt, x, y)`
 Move an item to another container.
 Use x, and y if you don't want items stacking in the desination container.
 Example:
 ```py
 items = API.ItemsInContainer(API.Backpack)

 API.SysMsg("Target your fish barrel", 32)
 barrel = API.RequestTarget()


 if len(items) > 0 and barrel:
     for item in items:
         data = API.ItemNameAndProps(item)
         if data and "An Exotic Fish" in data:
             API.QueueMoveItem(item, barrel)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `destination` | `uint` | ❌ No |  |
| `amt` | `ushort` | ✅ Yes | Amount to move |
| `x` | `int` | ✅ Yes | X coordinate inside a container |
| `y` | `int` | ✅ Yes | Y coordinate inside a container |

**Return Type:** `void` *(Does not return anything)*

---

### MoveItem
`(serial, destination, amt, x, y)`
 Move an item to another container.
 Use x, and y if you don't want items stacking in the desination container.
 Example:
 ```py
 items = API.ItemsInContainer(API.Backpack)

 API.SysMsg("Target your fish barrel", 32)
 barrel = API.RequestTarget()


 if len(items) > 0 and barrel:
     for item in items:
         data = API.ItemNameAndProps(item)
         if data and "An Exotic Fish" in data:
             API.MoveItem(item, barrel)
             API.Pause(0.75)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `destination` | `uint` | ❌ No |  |
| `amt` | `int` | ✅ Yes | Amount to move |
| `x` | `int` | ✅ Yes | X coordinate inside a container |
| `y` | `int` | ✅ Yes | Y coordinate inside a container |

**Return Type:** `void` *(Does not return anything)*

---

### QueueMoveItemOffset
`(serial, amt, x, y, z, OSI)`
 Move an item to the ground near you.
 Example:
 ```py
 items = API.ItemsInContainer(API.Backpack)
 for item in items:
   API.QueueMoveItemOffset(item, 0, 1, 0, 0)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `amt` | `ushort` | ✅ Yes | 0 to grab entire stack |
| `x` | `int` | ✅ Yes | Offset from your location |
| `y` | `int` | ✅ Yes | Offset from your location |
| `z` | `int` | ✅ Yes | Offset from your location. Leave blank in most cases |
| `OSI` | `bool` | ✅ Yes | True if you are playing OSI |

**Return Type:** `void` *(Does not return anything)*

---

### MoveItemOffset
`(serial, amt, x, y, z, OSI)`
 Move an item to the ground near you.
 Example:
 ```py
 items = API.ItemsInContainer(API.Backpack)
 for item in items:
   API.MoveItemOffset(item, 0, 1, 0, 0)
   API.Pause(0.75)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `amt` | `int` | ✅ Yes | 0 to grab entire stack |
| `x` | `int` | ✅ Yes | Offset from your location |
| `y` | `int` | ✅ Yes | Offset from your location |
| `z` | `int` | ✅ Yes | Offset from your location. Leave blank in most cases |
| `OSI` | `bool` | ✅ Yes | True if you are playing OSI |

**Return Type:** `void` *(Does not return anything)*

---

### UseSkill
`(skillName)`
 Use a skill.
 Example:
 ```py
 API.UseSkill("Hiding")
 API.Pause(11)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `skillName` | `string` | ❌ No | Can be a partial match. Will match the first skill containing this text. |

**Return Type:** `void` *(Does not return anything)*

---

### CastSpell
`(spellName)`
 Attempt to cast a spell by its name.
 Example:
 ```py
 API.CastSpell("Fireball")
 API.WaitForTarget()
 API.Target(API.Player)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `spellName` | `string` | ❌ No | This can be a partial match. Fireba will cast Fireball. |

**Return Type:** `void` *(Does not return anything)*

---

### Dress
`(name)`
 Dress from a saved dress configuration.
 Example:
 ```py
 API.Dress("PvP Gear")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | The name of the dress configuration |

**Return Type:** `void` *(Does not return anything)*

---

### GetAvailableDressOutfits

 Get all available dress configurations.
 Example:
 ```py
 outfits = API.GetAvailableDressOutfits()
 if outfits:
   Dress(outfits[0])
 ```


**Return Type:** `PythonList`

---

### Organizer
`(name, source, destination)`
 Runs an organizer agent to move items between containers.
 Example:
 ```py
 # Run organizer with default containers
 API.Organizer("MyOrganizer")

 # Run organizer with specific source and destination
 API.Organizer("MyOrganizer", 0x40001234, 0x40005678)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | The name of the organizer configuration to run |
| `source` | `uint` | ✅ Yes | Optional serial of the source container (0 for default) |
| `destination` | `uint` | ✅ Yes | Optional serial of the destination container (0 for default) |

**Return Type:** `void` *(Does not return anything)*

---

### ClientCommand
`(command)`
 Executes a client command as if typed in the game console


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `command` | `string` | ❌ No | The command to execute (including any arguments) |

**Return Type:** `void` *(Does not return anything)*

---

### BuffExists
`(buffName)`
 Check if a buff is active.
 Example:
 ```py
 if API.BuffExists("Bless"):
   API.SysMsg("You are blessed!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `buffName` | `string` | ❌ No | The name/title of the buff |

**Return Type:** `bool`

---

### ActiveBuffs

 Get a list of all buffs that are active.
 See [Buff](Buff.md) to see what attributes are available.
 Buff does not get updated after you access it in python, you will need to call this again to get the latest buff data.
 Example:
 ```py
 buffs = API.ActiveBuffs()
 for buff in buffs:
     API.SysMsg(buff.Title)
 ```


**Return Type:** `Buff[]`

---

### SysMsg
`(message, hue)`
 Show a system message(Left side of screen).
 Example:
 ```py
 API.SysMsg("Script started!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No | Message |
| `hue` | `ushort` | ✅ Yes | Color of the message |

**Return Type:** `void` *(Does not return anything)*

---

### Msg
`(message)`
 Say a message outloud.
 Example:
 ```py
 API.Say("Hello friend!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No | The message to say |

**Return Type:** `void` *(Does not return anything)*

---

### HeadMsg
`(message, serial, hue)`
 Show a message above a mobile or item, this is only visible to you.
 Example:
 ```py
 API.HeadMsg("Only I can see this!", API.Player)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No | The message |
| `serial` | `uint` | ❌ No | The item or mobile |
| `hue` | `ushort` | ✅ Yes | Message hue |

**Return Type:** `void` *(Does not return anything)*

---

### PartyMsg
`(message)`
 Send a message to your party.
 Example:
 ```py
 API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No | The message |

**Return Type:** `void` *(Does not return anything)*

---

### GuildMsg
`(message)`
 Send your guild a message.
 Example:
 ```py
 API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### AllyMsg
`(message)`
 Send a message to your alliance.
 Example:
 ```py
 API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### WhisperMsg
`(message)`
 Whisper a message.
 Example:
 ```py
 API.WhisperMsg("Psst, bet you didn't see me here..")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### YellMsg
`(message)`
 Yell a message.
 Example:
 ```py
 API.YellMsg("Vendor restocked, get your fresh feathers!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### EmoteMsg
`(message)`
 Emote a message.
 Example:
 ```py
 API.EmoteMsg("laughing")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### GlobalMsg
`(message)`
 Send a chat message via the global chat msg system ( ,message here ).


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### PromptResponse
`(message)`
 Send a response to a server prompt(Like renaming a rune for example).


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `message` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### FindItem
`(serial)`
 Try to get an item by its serial.
 Sets API.Found to the serial of the item found.
 Example:
 ```py
 donkey = API.RequestTarget()
 item = API.FindItem(donkey)
 if item:
   API.SysMsg("Found the donkey!")
   API.UseObject(item)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | The serial |

**Return Type:** `PyItem`

---

### FindType
`(graphic, container, range, hue, minamount)`
 Attempt to find an item by type(graphic).
 Sets API.Found to the serial of the item found.
 Example:
 ```py
 item = API.FindType(0x0EED, API.Backpack)
 if item:
   API.SysMsg("Found the item!")
   API.UseObject(item)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `uint` | ❌ No | Graphic/Type of item to find |
| `container` | `uint` | ✅ Yes | Container to search |
| `range` | `ushort` | ✅ Yes | Max range of item |
| `hue` | `ushort` | ✅ Yes | Hue of item |
| `minamount` | `ushort` | ✅ Yes | Only match if item stack is at least this much |

**Return Type:** `PyItem`

---

### FindTypeAll
`(graphic, container, range, hue, minamount)`
 Return a list of items matching the parameters set.
 Example:
 ```py
 items = API.FindTypeAll(0x0EED, API.Backpack)
 if items:
   API.SysMsg("Found " + str(len(items)) + " items!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `uint` | ❌ No | Graphic/Type of item to find |
| `container` | `uint` | ✅ Yes | Container to search |
| `range` | `ushort` | ✅ Yes | Max range of item(if on ground) |
| `hue` | `ushort` | ✅ Yes | Hue of item |
| `minamount` | `ushort` | ✅ Yes | Only match if item stack is at least this much |

**Return Type:** `PyItem[]`

---

### FindLayer
`(layer, serial)`
 Attempt to find an item on a layer.
 Sets API.Found to the serial of the item found.
 Example:
 ```py
 item = API.FindLayer("Helmet")
 if item:
   API.SysMsg("Wearing a helmet!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `layer` | `string` | ❌ No | The layer to check, see https://github.com/PlayTazUO/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs |
| `serial` | `uint` | ✅ Yes | Optional, if not set it will check yourself, otherwise it will check the mobile requested |

**Return Type:** `PyItem`

---

### GetItemsOnGround
`(distance, graphic)`
 Get all items on the ground within specified range.
 Example:
 ```py
 items = API.GetItemsOnGround(10)  # All items within 10 tiles
 if items:
   API.SysMsg("Found " + str(len(items)) + " items on ground!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `distance` | `int` | ✅ Yes | Optional max distance to search (default: no limit) |
| `graphic` | `uint` | ✅ Yes | Optional graphic/type filter (default: no filter) |

**Return Type:** `PythonList`

---

### ItemsInContainer
`(container, recursive)`
 Get all items in a container.
 Example:
 ```py
 items = API.ItemsInContainer(API.Backpack)
 if items:
   API.SysMsg("Found " + str(len(items)) + " items!")
   for item in items:
     API.SysMsg(item.Name)
     API.Pause(0.5)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `container` | `uint` | ❌ No |  |
| `recursive` | `bool` | ✅ Yes | Search sub containers also? |

**Return Type:** `PyItem[]`

---

### UseType
`(graphic, hue, container, skipQueue)`
 Attempt to use the first item found by graphic(type).
 Example:
 ```py
 API.UseType(0x3434, API.Backpack)
 API.WaitForTarget()
 API.Target(API.Player)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `uint` | ❌ No | Graphic/Type |
| `hue` | `ushort` | ✅ Yes | Hue of item |
| `container` | `uint` | ✅ Yes | Parent container |
| `skipQueue` | `bool` | ✅ Yes | Defaults to true, set to false to queue the double click |

**Return Type:** `void` *(Does not return anything)*

---

### CreateCooldownBar
`(seconds, text, hue)`
 Create a cooldown bar.
 Example:
 ```py
 API.CreateCooldownBar(5, "Healing", 21)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `seconds` | `double` | ❌ No | Duration in seconds for the cooldown bar |
| `text` | `string` | ❌ No | Text on the cooldown bar |
| `hue` | `ushort` | ❌ No | Hue to color the cooldown bar |

**Return Type:** `void` *(Does not return anything)*

---

### IgnoreObject
`(serial)`
 Adds an item or mobile to your ignore list.
 These are unique lists per script. Ignoring an item in one script, will not affect other running scripts.
 Example:
 ```py
 for item in ItemsInContainer(API.Backpack):
   if item.Name == "Dagger":
   API.IgnoreObject(item)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | The item/mobile serial |

**Return Type:** `void` *(Does not return anything)*

---

### ClearIgnoreList

 Clears the ignore list. Allowing functions to see those items again.
 Example:
 ```py
 API.ClearIgnoreList()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### OnIgnoreList
`(serial)`
 Check if a serial is on the ignore list.
 Example:
 ```py
 if API.OnIgnoreList(API.Backpack):
   API.SysMsg("Currently ignoring backpack")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `bool`

---

### Pathfind
`(x, y, z, distance, wait, timeout)`
 Attempt to pathfind to a location.  This will fail with large distances.
 Example:
 ```py
 API.Pathfind(1414, 1515)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `z` | `int` | ✅ Yes |  |
| `distance` | `int` | ✅ Yes | Distance away from goal to stop. |
| `wait` | `bool` | ✅ Yes | True/False if you want to wait for pathfinding to complete or time out |
| `timeout` | `int` | ✅ Yes | Seconds to wait before cancelling waiting |

**Return Type:** `bool`

---

### PathfindEntity
`(entity, distance, wait, timeout)`
 Attempt to pathfind to a mobile or item.
 Example:
 ```py
 mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
 if mob:
   API.PathfindEntity(mob)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `entity` | `uint` | ❌ No | The mobile or item |
| `distance` | `int` | ✅ Yes | Distance to stop from goal |
| `wait` | `bool` | ✅ Yes | True/False if you want to wait for pathfinding to complete or time out |
| `timeout` | `int` | ✅ Yes | Seconds to wait before cancelling waiting |

**Return Type:** `bool`

---

### Pathfinding

 Check if you are already pathfinding.
 Example:
 ```py
 if API.Pathfinding():
   API.SysMsg("Pathfinding...!")
   API.Pause(0.25)
 ```


**Return Type:** `bool`

---

### CancelPathfinding

 Cancel pathfinding.
 Example:
 ```py
 if API.Pathfinding():
   API.CancelPathfinding()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### GetPath
`(x, y, z, distance)`
 Attempt to build a path to a location.  This will fail with large distances.
 Example:
 ```py
 API.RequestTarget()
 path = API.GetPath(int(API.LastTargetPos.X), int(API.LastTargetPos.Y))
 if path is not None:
     for x, y, z in path:
         tile = API.GetTile(x, y)
         tile.Hue = 53
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `z` | `int` | ✅ Yes |  |
| `distance` | `int` | ✅ Yes | Distance away from goal to stop. |

**Return Type:** `PythonList`

---

### AutoFollow
`(mobile)`
 Automatically follow a mobile. This is different than pathfinding. This will continune to follow the mobile.
 Example:
 ```py
 mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
 if mob:
   API.AutoFollow(mob)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `mobile` | `uint` | ❌ No | The mobile |

**Return Type:** `void` *(Does not return anything)*

---

### CancelAutoFollow

 Cancel auto follow mode.
 Example:
 ```py
 if API.Pathfinding():
   API.CancelAutoFollow()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### Run
`(direction)`
 Run in a direction.
 Example:
 ```py
 API.Run("north")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `direction` | `string` | ❌ No | north/northeast/south/west/etc |

**Return Type:** `void` *(Does not return anything)*

---

### Walk
`(direction)`
 Walk in a direction.
 Example:
 ```py
 API.Walk("north")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `direction` | `string` | ❌ No | north/northeast/south/west/etc |

**Return Type:** `void` *(Does not return anything)*

---

### Turn
`(direction)`
 Turn your character a specific direction.
 Example:
 ```py
 API.Turn("north")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `direction` | `string` | ❌ No | north, northeast, etc |

**Return Type:** `void` *(Does not return anything)*

---

### Rename
`(serial, name)`
 Attempt to rename something like a pet.
 Example:
 ```py
 API.Rename(0x12345678, "My Handsome Pet")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial of the mobile to rename |
| `name` | `string` | ❌ No | The new name |

**Return Type:** `void` *(Does not return anything)*

---

### Dismount
`(skipQueue)`
 Attempt to dismount if mounted.
 Example:
 ```py
 API.Dismount()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `skipQueue` | `bool` | ✅ Yes | Defaults true, set to false to use a double click queue |

**Return Type:** `void` *(Does not return anything)*

---

### Mount
`(serial, skipQueue)`
 Attempt to mount(double click)
 Example:
 ```py
 API.Mount(0x12345678)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ✅ Yes | Defaults to saved mount |
| `skipQueue` | `bool` | ✅ Yes | Defaults true, set to false to use a double click queue |

**Return Type:** `void` *(Does not return anything)*

---

### SetMount
`(serial)`
 This will set your saved mount for this character.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### WaitForTarget
`(targetType, timeout)`
 Wait for a target cursor.
 Example:
 ```py
 API.WaitForTarget()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `targetType` | `string` | ✅ Yes | neutral/harmful/beneficial/any/harm/ben |
| `timeout` | `double` | ✅ Yes | Max duration in seconds to wait |

**Return Type:** `bool`

---

### Target
`(serial)`
 Target an item or mobile.
 Example:
 ```py
 if API.WaitForTarget():
   API.Target(0x12345678)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial of the item/mobile to target |

**Return Type:** `void` *(Does not return anything)*

---

### Target
`(x, y, z, graphic)`
 Target a location. Include graphic if targeting a static.
 Example:
 ```py
 if API.WaitForTarget():
   API.Target(1243, 1337, 0)
  ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `ushort` | ❌ No |  |
| `y` | `ushort` | ❌ No |  |
| `z` | `short` | ❌ No |  |
| `graphic` | `ushort` | ✅ Yes | Graphic of the static to target |

**Return Type:** `void` *(Does not return anything)*

---

### RequestTarget
`(timeout)`
 Request the player to target something.
 Example:
 ```py
 target = API.RequestTarget()
 if target:
   API.SysMsg("Targeted serial: " + str(target))
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `timeout` | `double` | ✅ Yes | Mac duration to wait for them to target something. |

**Return Type:** `uint`

---

### RequestAnyTarget
`(timeout)`
 Prompts the player to target any object in the game world, including an <c>Item</c> , <c>Mobile</c> , <c>Land</c> tile, <c>Static</c> , or <c>Multi</c> .
 Waits for the player to select a target within a given timeout period.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `timeout` | `double` | ✅ Yes | The maximum time, in seconds, to wait for a valid target selection.  
         If the timeout expires without a selection, the method returns <c>null</c> . |

**Return Type:** `PyGameObject`

---

### TargetSelf

 Target yourself.
 Example:
 ```py
 API.TargetSelf()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### TargetLandRel
`(xOffset, yOffset)`
 Target a land tile relative to your position.
 If this doesn't work, try TargetTileRel instead.
 Example:
 ```py
 API.TargetLand(1, 1)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `xOffset` | `int` | ❌ No | X from your position |
| `yOffset` | `int` | ❌ No | Y from your position |

**Return Type:** `void` *(Does not return anything)*

---

### TargetTileRel
`(xOffset, yOffset, graphic)`
 Target a tile relative to your location.
 If this doesn't work, try TargetLandRel instead.'
 Example:
 ```py
 API.TargetTileRel(1, 1)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `xOffset` | `int` | ❌ No | X Offset from your position |
| `yOffset` | `int` | ❌ No | Y Offset from your position |
| `graphic` | `ushort` | ✅ Yes | Optional graphic, will try to use the graphic of the tile at that location if left empty. |

**Return Type:** `void` *(Does not return anything)*

---

### TargetResource
`(itemSerial, resource)`
 This will attempt to use an item and target a resource, some servers may not support this.
 ```
 0: ore
 1: sand
 2: wood
 3: graves
 4: red_mushrooms
 ```
 Example:
 ```py
 API.TargetResource(MY_SHOVEL_SERIAL, 0)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `itemSerial` | `uint` | ❌ No |  |
| `resource` | `uint` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### CancelTarget

 Cancel targeting.
 Example:
 ```py
 if API.WaitForTarget():
   API.CancelTarget()
   API.SysMsg("Targeting cancelled, april fools made you target something!")
 ```


**Return Type:** `void` *(Does not return anything)*

---

### PreTarget
`(serial, targetType)`
 Sets a pre-target that will be automatically applied when the next targeting request comes from the server.
 This is useful for automating actions that require targeting, like using bandages or spells.
 Example:
 ```py
 # Pre-target self for healing
 API.PreTarget(API.Player.Serial, "beneficial")
 API.UseObject(bandage_item)  # This will automatically target self when targeting request comes

 # Pre-target an enemy for attack spells
 enemy = API.FindMobile(mobile_serial)
 API.PreTarget(enemy.Serial, "harmful")
 API.CastSpell("Lightning")  # This will automatically target the enemy
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial of the entity to pre-target |
| `targetType` | `string` | ✅ Yes | Type of target: "neutral"/"neut"/"n", "harmful"/"harm"/"h", "beneficial"/"ben"/"heal"/"b" (default: "neutral") |

**Return Type:** `void` *(Does not return anything)*

---

### CancelPreTarget

 Cancels any active pre-target.
 Example:
 ```py
 API.PreTarget(enemy.Serial, "harmful")
 # Changed my mind, cancel the pre-target
 API.CancelPreTarget()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### HasTarget
`(targetType)`
 Check if the player has a target cursor.
 Example:
 ```py
 if API.HasTarget():
     API.CancelTarget()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `targetType` | `string` | ✅ Yes | neutral/harmful/beneficial/any/harm/ben |

**Return Type:** `bool`

---

### GetMap

 Get the current map index.
 Standard maps are:
 0 = Fel
 1 = Tram
 2 = Ilshenar
 3 = Malas
 4 = Tokuno
 5 = TerMur


**Return Type:** `int`

---

### SetSkillLock
`(skill, up_down_locked)`
 Set a skills lock status.
 Example:
 ```py
 API.SetSkillLock("Hiding", "locked")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `skill` | `string` | ❌ No | The skill name, can be partia; |
| `up_down_locked` | `string` | ❌ No | up/down/locked |

**Return Type:** `void` *(Does not return anything)*

---

### SetStatLock
`(stat, up_down_locked)`
 Set a skills lock status.
 Example:
 ```py
 API.SetStatLock("str", "locked")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `stat` | `string` | ❌ No | The stat name, str, dex, int; Defaults to str. |
| `up_down_locked` | `string` | ❌ No | up/down/locked |

**Return Type:** `void` *(Does not return anything)*

---

### Logout

 Logout of the game.
 Example:
 ```py
 API.Logout()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### ItemNameAndProps
`(serial, wait, timeout)`
 Gets item name and properties.
 This returns the name and properties in a single string. You can split it by new line if you want to separate them.
 Example:
 ```py
 data = API.ItemNameAndProps(0x12345678, True)
 if data:
   API.SysMsg("Item data: " + data)
   if "An Exotic Fish" in data:
     API.SysMsg("Found an exotic fish!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |
| `wait` | `bool` | ✅ Yes | True or false to wait for name and props |
| `timeout` | `int` | ✅ Yes | Timeout in seconds |

**Return Type:** `string`

---

### RequestOPLData
`(serials)`
 Requests Object Property List (OPL) data for the specified serials.
 If the OPL data doesn't already exist, it will be requested from the server.
 OPL consists of item name and tooltip text(properties).


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serials` | `IList<uint>` | ❌ No | A list of object serials to request OPL data for |

**Return Type:** `void` *(Does not return anything)*

---

### HasGump
`(ID)`
 Check if a player has a server gump. Leave blank to check if they have any server gump.
 Example:
 ```py
 if API.HasGump(0x12345678):
   API.SysMsg("Found a gump!")
```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ID` | `uint` | ✅ Yes | Skip to check if player has any gump from server. |

**Return Type:** `uint`

---

### ReplyGump
`(button, gump)`
 Reply to a gump.
 Example:
 ```py
 API.ReplyGump(21)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `button` | `int` | ❌ No | Button ID |
| `gump` | `uint` | ✅ Yes | Gump ID, leave blank to reply to last gump |

**Return Type:** `bool`

---

### CloseGump
`(ID)`
 Close the last gump open, or a specific gump.
 Example:
 ```py
 API.CloseGump()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ID` | `uint` | ✅ Yes | Gump ID |

**Return Type:** `void` *(Does not return anything)*

---

### ConfigNextGump
`(serial, x, y, isVisible, autoClose, autoRespond, autoRespondButton)`
 Configure how the next gump should be handled.
 Example:
 ```py
 # Position gump at coordinates
 API.ConfigNextGump(x=100, y=200)

 # Auto-close any gump
 API.ConfigNextGump(autoClose=True)

 # Auto-respond to specific gump
 API.ConfigNextGump(serial=0x12345678, autoRespond=True, autoRespondButton=1)

 # Clear configuration
 API.ConfigNextGump()

 Note: This is only applied once. You cannot stack multiple configs. This is reset after successfully applied and only applies to server-sent gumps.
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint?` | ✅ Yes | Gump serial to match (0 = match any gump) |
| `x` | `int?` | ✅ Yes | X position |
| `y` | `int?` | ✅ Yes | Y position |
| `isVisible` | `bool?` | ✅ Yes | Whether gump should be visible |
| `autoClose` | `bool?` | ✅ Yes | Automatically close the gump |
| `autoRespond` | `bool?` | ✅ Yes | Automatically respond to the gump |
| `autoRespondButton` | `int?` | ✅ Yes | Button ID to use for auto-response |

**Return Type:** `void` *(Does not return anything)*

---

### GumpContains
`(text, ID)`
 Check if a gump contains a specific text.
 Example:
 ```py
 if API.GumpContains("Hello"):
   API.SysMsg("Found the text!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No | Can be regex if you start with $, otherwise it's just regular search. Case Sensitive. |
| `ID` | `uint` | ✅ Yes | Gump ID, blank to use the last gump. |

**Return Type:** `bool`

---

### GetGumpContents
`(ID)`
 This will return a string of all the text in a server-side gump.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ID` | `uint` | ✅ Yes | Gump ID, blank to use the last gump. |

**Return Type:** `string`

---

### GetGump
`(ID)`
 Get a gump by ID.
 Example:
 ```py
 gump = API.GetGump()
 if gump:
   API.SysMsg("Found the gump!")
   gump.Dispose() #Close it
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ID` | `uint` | ✅ Yes | Leave blank to use last gump opened from server |

**Return Type:** `Gump`

---

### GetAllGumps

 Gets all currently open server-side gumps.


**Return Type:** `PythonList`

---

### WaitForGump
`(ID, delay)`
 Wait for a server-side gump.
 Example:
 ```py
 if API.WaitForGump(1951773915):
   API.HeadMsg("SUCCESS", API.Player, 62)
 else:
  API.HeadMsg("FAILURE", API.Player, 32)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ID` | `uint` | ✅ Yes |  |
| `delay` | `double` | ✅ Yes | Seconds to wait |

**Return Type:** `bool`

---

### ToggleFly

 Toggle flying if you are a gargoyle.
 Example:
 ```py
 API.ToggleFly()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### ToggleAbility
`(ability)`
 Toggle an ability.
 Example:
 ```py
 if not API.PrimaryAbilityActive():
   API.ToggleAbility("primary")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `ability` | `string` | ❌ No | primary/secondary/stun/disarm |

**Return Type:** `void` *(Does not return anything)*

---

### PrimaryAbilityActive

 Check if your primary ability is active.
 Example:
 ```py
 if API.PrimaryAbilityActive():
   API.SysMsg("Primary ability is active!")
 ```


**Return Type:** `bool`

---

### SecondaryAbilityActive

 Check if your secondary ability is active.
 Example:
 ```py
 if API.SecondaryAbilityActive():
   API.SysMsg("Secondary ability is active!")
 ```


**Return Type:** `bool`

---

### InJournal
`(msg, clearMatches)`
 Check if your journal contains a message.
 Example:
 ```py
 if API.InJournal("You have been slain"):
   API.SysMsg("You have been slain!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `msg` | `string` | ❌ No | The message to check for. Can be regex, prepend your msg with $ |
| `clearMatches` | `bool` | ✅ Yes |  |

**Return Type:** `bool`

---

### InJournalAny
`(msgs, clearMatches)`
 Check if the journal contains *any* of the strings in this list.
 Can be regex, prepend your msgs with $
 Example:
 ```py
 if API.InJournalAny(["You have been slain", "You are dead"]):
   API.SysMsg("You have been slain or dead!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `msgs` | `IList<string>` | ❌ No |  |
| `clearMatches` | `bool` | ✅ Yes |  |

**Return Type:** `bool`

---

### GetJournalEntries
`(seconds, matchingText)`
 Get all the journal entires in the last X seconds.
 matchingText supports regex with $ prepended.
 Example:
 ```py
 list = API.GetJournalEntries(30)
 if list:
   for entry in list:
     entry.Text # Do something with this
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `seconds` | `double` | ❌ No |  |
| `matchingText` | `string` | ✅ Yes | Only add if text matches |

**Return Type:** `PythonList`

---

### ClearJournal
`(matchingEntries)`
 Clear your journal(This is specific for each script).
 Supports regex matching if prefixed with $
 Example:
 ```py
 API.ClearJournal()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `matchingEntries` | `string` | ✅ Yes | String or regex to match with. If this is set, only matching entries will be removed. |

**Return Type:** `void` *(Does not return anything)*

---

### Pause
`(seconds)`
 Pause the script.
 Example:
 ```py
 API.Pause(5)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `seconds` | `double` | ❌ No | 0-30 seconds. |

**Return Type:** `void` *(Does not return anything)*

---

### Stop

 Stops the current script.
 Example:
 ```py
 API.Stop()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### ToggleAutoLoot

 Toggle autolooting on or off.
 Example:
 ```py
 API.ToggleAutoLoot()
 ```


**Return Type:** `void` *(Does not return anything)*

---

### AutoLootContainer
`(container)`
 Use autoloot on a specific container.
 Example:
 ```py
 targ = API.RequestTarget()
 if targ:
   API.AutoLootContainer(targ)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `container` | `uint` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### Virtue
`(virtue)`
 Use a virtue.
 Example:
 ```py
 API.Virtue("honor")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `virtue` | `string` | ❌ No | honor/sacrifice/valor |

**Return Type:** `void` *(Does not return anything)*

---

### NearestEntity
`(scanType, maxDistance)`
 Find the nearest item/mobile based on scan type.
 Sets API.Found to the serial of the item/mobile.
 Example:
 ```py
 item = API.NearestEntity(API.ScanType.Item, 5)
 if item:
   API.SysMsg("Found an item!")
   API.UseObject(item)
   # You can use API.FindItem or API.FindMobile(item.Serial) to determine if it's an item or mobile
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `scanType` | `ScanType` | ❌ No |  |
| `maxDistance` | `int` | ✅ Yes |  |

**Return Type:** `PyEntity`

---

### NearestMobile
`(notoriety, maxDistance)`
 Get the nearest mobile by Notoriety.
 Sets API.Found to the serial of the mobile.
 Example:
 ```py
 mob = API.NearestMobile([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)
 if mob:
   API.SysMsg("Found a criminal!")
   API.Msg("Guards!")
   API.Attack(mob)
   ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `notoriety` | `IList<Notoriety>` | ❌ No | List of notorieties |
| `maxDistance` | `int` | ✅ Yes |  |

**Return Type:** `PyMobile`

---

### NearestCorpse
`(distance)`
 Get the nearest corpse within a distance.
 Sets API.Found to the serial of the corpse.
 Example:
 ```py
 corpse = API.NearestCorpse()
 if corpse:
   API.SysMsg("Found a corpse!")
   API.UseObject(corpse)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `distance` | `int` | ✅ Yes |  |

**Return Type:** `PyItem`

---

### NearestMobiles
`(notoriety, maxDistance)`
 Get all mobiles matching Notoriety and distance.
 Example:
 ```py
 mob = API.NearestMobiles([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)
 if len(mob) > 0:
   API.SysMsg("Found enemies!")
   API.Msg("Guards!")
   API.Attack(mob[0])
   ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `notoriety` | `IList<Notoriety>` | ❌ No | List of notorieties |
| `maxDistance` | `int` | ✅ Yes |  |

**Return Type:** `PyMobile[]`

---

### FindMobile
`(serial)`
 Get a mobile from its serial.
 Sets API.Found to the serial of the mobile.
 Example:
 ```py
 mob = API.FindMobile(0x12345678)
 if mob:
   API.SysMsg("Found the mobile!")
   API.UseObject(mob)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No |  |

**Return Type:** `PyMobile`

---

### GetAllMobiles
`(graphic, distance, notoriety)`
 Return a list of all mobiles the client is aware of, optionally filtered by graphic, distance, and/or notoriety.
 Example:
 ```py
 # Get all mobiles
 mobiles = API.GetAllMobiles()
 # Get all mobiles with graphic 400
 humans = API.GetAllMobiles(400)
 # Get all humans within 5 tiles
 nearby_humans = API.GetAllMobiles(400, 5)
 # Get all enemies (murderers and criminals) within 15 tiles
 enemies = API.GetAllMobiles(distance=15, notoriety=[API.Notoriety.Murderer, API.Notoriety.Criminal])
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `ushort?` | ✅ Yes | Optional graphic ID to filter by |
| `distance` | `int?` | ✅ Yes | Optional maximum distance from player |
| `notoriety` | `IList<Notoriety>` | ✅ Yes | Optional list of notoriety flags to filter by |

**Return Type:** `PyMobile[]`

---

### GetTile
`(x, y)`
 Get the tile at a location.
 Example:
 ```py
 tile = API.GetTile(1414, 1515)
 if tile:
   API.SysMsg(f"Found a tile with graphic: {tile.Graphic}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |

**Return Type:** `PyGameObject`

---

### GetStaticsAt
`(x, y)`
 Gets all static objects at a specific position (x, y coordinates).
 This includes trees, vegetation, buildings, and other non-movable scenery.
 Example:
 ```py
 statics = API.GetStaticsAt(1000, 1000)
 for s in statics:
     API.SysMsg(f"Static Graphic: {s.Graphic}, Z: {s.Z}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | X coordinate |
| `y` | `int` | ❌ No | Y coordinate |

**Return Type:** `List<PyStatic>`

---

### GetStaticsInArea
`(x1, y1, x2, y2)`
 Gets all static objects within a rectangular area defined by coordinates.
 This includes trees, vegetation, buildings, and other non-movable scenery.
 Example:
 ```py
 statics = API.GetStaticsInArea(1000, 1000, 1010, 1010)
 API.SysMsg(f"Found {len(statics)} statics in area")
 for s in statics:
     if s.IsVegetation:
         API.SysMsg(f"Vegetation Graphic: {s.Graphic} at {s.X}, {s.Y}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x1` | `int` | ❌ No | Starting X coordinate |
| `y1` | `int` | ❌ No | Starting Y coordinate |
| `x2` | `int` | ❌ No | Ending X coordinate |
| `y2` | `int` | ❌ No | Ending Y coordinate |

**Return Type:** `List<PyStatic>`

---

### GetMultisAt
`(x, y)`
 Gets all multi objects at a specific position (x, y coordinates).
 This includes server-side house data.
 Example:
 ```py
 multis = API.GetMultisAt(1000, 1000)
 for m in multis:
     API.SysMsg(f"Multi Graphic: {m.Graphic}, Z: {m.Z}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | X coordinate |
| `y` | `int` | ❌ No | Y coordinate |

**Return Type:** `List<PyMulti>`

---

### GetMultisInArea
`(x1, y1, x2, y2)`
 Gets all multi objects within a rectangular area defined by coordinates.
 This includes server-side house data.
 Example:
 ```py
 multis = API.GetMultisInArea(1000, 1000, 1010, 1010)
 API.SysMsg(f"Found {len(multis)} multis in area")
 for m in multis:
     API.SysMsg(f"Multi Graphic: {m.Graphic} at {m.X}, {m.Y}")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x1` | `int` | ❌ No | Starting X coordinate |
| `y1` | `int` | ❌ No | Starting Y coordinate |
| `x2` | `int` | ❌ No | Ending X coordinate |
| `y2` | `int` | ❌ No | Ending Y coordinate |

**Return Type:** `List<PyMulti>`

---

### IsFriend
`(serial)`
 Check if a mobile is in the friends list.
 Example:
 ```py
 if API.IsFriend(player.Serial):
     API.SysMsg("This player is your friend!")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial number of the mobile to check |

**Return Type:** `bool`

---

### AddFriend
`(serial)`
 Add a mobile to the friends list by serial number.
 Example:
 ```py
 mobile = API.FindMobile(0x12345)
 if mobile:
     API.AddFriend(mobile.Serial)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial number of the mobile to add |

**Return Type:** `bool`

---

### RemoveFriend
`(serial)`
 Remove a mobile from the friends list by serial number.
 Example:
 ```py
 API.RemoveFriend(0x12345)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | Serial number of the mobile to remove |

**Return Type:** `bool`

---

### GetAllFriends

 Get all friends as an array of serials.
 Example:
 ```py
 friends = API.GetAllFriends()
 for friend in friends:
     API.FindMobile(friend)
 ```


**Return Type:** `PythonList`

---

### CreateGump
`(acceptMouseInput, canMove, keepOpen)`
 Get a blank gump.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `acceptMouseInput` | `bool` | ✅ Yes | Allow clicking the gump |
| `canMove` | `bool` | ✅ Yes | Allow the player to move this gump |
| `keepOpen` | `bool` | ✅ Yes | If true, the gump won't be closed if the script stops. Otherwise, it will be closed when the script is stopped. Defaults to false. |

**Return Type:** `PyBaseGump`

---

### AddGump
`(g)`
 Add a gump to the players screen.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `g` | `object` | ❌ No | The gump to add |

**Return Type:** `void` *(Does not return anything)*

---

### CreateGumpCheckbox
`(text, hue, isChecked)`
 Create a checkbox for gumps.
  Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 cb = API.CreateGumpCheckbox("Check me?!")
 g.Add(cb)
 API.AddGump(g)

 API.SysMsg("Checkbox checked: " + str(cb.IsChecked))
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes | Optional text label |
| `hue` | `ushort` | ✅ Yes | Optional hue |
| `isChecked` | `bool` | ✅ Yes | Default false, set to true if you want this checkbox checked on creation |

**Return Type:** `PyCheckbox`

---

### CreateGumpLabel
`(text, hue)`
 Create a label for a gump.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No | The text |
| `hue` | `ushort` | ✅ Yes | The hue of the text |

**Return Type:** `Label`

---

### CreateGumpColorBox
`(opacity, color)`
 Get a transparent color box for gumps.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 cb = API.CreateGumpColorBox(0.5, "#000000")
 cb.SetWidth(200)
 cb.SetHeight(200)
 g.Add(cb)
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `opacity` | `float` | ✅ Yes | 0.5 = 50% |
| `color` | `string` | ✅ Yes | Html color code like #000000 |

**Return Type:** `AlphaBlendControl`

---

### CreateGumpItemPic
`(graphic, width, height)`
 Create a picture of an item.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `uint` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `ResizableStaticPic`

---

### CreateGumpButton
`(text, hue, normal, pressed, hover)`
 Create a button for gumps.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 button = API.CreateGumpButton("Click Me!")
 g.Add(button)
 API.AddGump(g)

 while True:
   API.SysMsg("Button currently clicked?: " + str(button.IsClicked))
   API.SysMsg("Button clicked since last check?: " + str(button.HasBeenClicked()))
   API.Pause(0.2)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes |  |
| `hue` | `ushort` | ✅ Yes |  |
| `normal` | `ushort` | ✅ Yes | Graphic when not clicked or hovering |
| `pressed` | `ushort` | ✅ Yes | Graphic when pressed |
| `hover` | `ushort` | ✅ Yes | Graphic on hover |

**Return Type:** `Button`

---

### CreateSimpleButton
`(text, width, height)`
 Create a simple button, does not use graphics.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 button = API.CreateSimpleButton("Click Me!", 100, 20)
 g.Add(button)
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `NiceButton`

---

### CreateGumpRadioButton
`(text, group, inactive, active, hue, isChecked)`
 Create a radio button for gumps, use group numbers to only allow one item to be checked at a time.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 rb = API.CreateGumpRadioButton("Click Me!", 1)
 g.Add(rb)
 API.AddGump(g)
 API.SysMsg("Radio button checked?: " + str(rb.IsChecked))
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes | Optional text |
| `group` | `int` | ✅ Yes | Group ID |
| `inactive` | `ushort` | ✅ Yes | Unchecked graphic |
| `active` | `ushort` | ✅ Yes | Checked graphic |
| `hue` | `ushort` | ✅ Yes | Text color |
| `isChecked` | `bool` | ✅ Yes | Defaults false, set to true if you want this button checked by default. |

**Return Type:** `RadioButton`

---

### CreateGumpTextBox
`(text, width, height, multiline)`
 Create a text area control.
 Example:
 ```py
 w = 500
 h = 600

 gump = API.CreateGump(True, True)
 gump.SetWidth(w)
 gump.SetHeight(h)
 gump.CenterXInViewPort()
 gump.CenterYInViewPort()

 bg = API.CreateGumpColorBox(0.7, "#D4202020")
 bg.SetWidth(w)
 bg.SetHeight(h)

 gump.Add(bg)

 textbox = API.CreateGumpTextBox("Text example", w, h, True)

 gump.Add(textbox)

 API.AddGump(gump)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes |  |
| `width` | `int` | ✅ Yes |  |
| `height` | `int` | ✅ Yes |  |
| `multiline` | `bool` | ✅ Yes |  |

**Return Type:** `TTFTextInputField`

---

### CreateGumpTTFLabel
`(text, size, color, font, aligned, maxWidth, applyStroke)`
 Create a TTF label with advanced options.
 Example:
 ```py
 gump = API.CreateGump()
 gump.SetRect(100, 100, 200, 200)

 ttflabel = API.CreateGumpTTFLabel("Example label", 25, "#F100DD", "alagard")
 ttflabel.SetRect(10, 10, 180, 30)
 gump.Add(ttflabel)

 API.AddGump(gump) #Add the gump to the players screen
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No |  |
| `size` | `float` | ❌ No | Font size |
| `color` | `string` | ✅ Yes | Hex color: #FFFFFF. Must begin with #. |
| `font` | `string` | ✅ Yes | Must have the font installed in TazUO |
| `aligned` | `string` | ✅ Yes | left/center/right. Must set a max width for this to work. |
| `maxWidth` | `int` | ✅ Yes | Max width before going to the next line |
| `applyStroke` | `bool` | ✅ Yes | Uses players stroke settings, this turns it on or off |

**Return Type:** `TextBox`

---

### CreateGumpSimpleProgressBar
`(width, height, backgroundColor, foregroundColor, value, max)`
 Create a progress bar. Can be updated as needed with `bar.SetProgress(current, max)`.
 Example:
 ```py
 gump = API.CreateGump()
 gump.SetRect(100, 100, 400, 200)

 pb = API.CreateGumpSimpleProgressBar(400, 200)
 gump.Add(pb)

 API.AddGump(gump)

 cur = 0
 max = 100

 while True:
   pb.SetProgress(cur, max)
   if cur >= max:
   break
   cur += 1
   API.Pause(0.5)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The width of the bar |
| `height` | `int` | ❌ No | The height of the bar |
| `backgroundColor` | `string` | ✅ Yes | The background color(Hex color like #616161) |
| `foregroundColor` | `string` | ✅ Yes | The foreground color(Hex color like #212121) |
| `value` | `int` | ✅ Yes | The current value, for example 70 |
| `max` | `int` | ✅ Yes | The max value(or what would be 100%), for example 100 |

**Return Type:** `SimpleProgressBar`

---

### CreateGumpScrollArea
`(x, y, width, height)`
 Create a scrolling area, add and position controls to it directly.
 Example:
 ```py
 sa = API.CreateGumpScrollArea(0, 60, 200, 140)
 gump.Add(sa)

 for i in range(10):
     label = API.CreateGumpTTFLabel(f"Label {i + 1}", 20, "#FFFFFF", "alagard")
     label.SetRect(5, i * 20, 180, 20)
     sa.Add(label)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `PyScrollArea`

---

### CreateGumpPic
`(graphic, x, y, hue)`
 Create a gump pic(Use this for gump art, not item art)
 Example:
 ```py
 gumpPic = API.CreateGumpPic(0xafb)
 gump.Add(gumpPic)


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `ushort` | ❌ No |  |
| `x` | `int` | ✅ Yes |  |
| `y` | `int` | ✅ Yes |  |
| `hue` | `ushort` | ✅ Yes |  |

**Return Type:** `GumpPic`

---

### CreateDropDown
`(width, items, selectedIndex)`
 Creates a dropdown control (combobox) with the specified width and items.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The width of the dropdown control |
| `items` | `string[]` | ❌ No | Array of strings to display as dropdown options |
| `selectedIndex` | `int` | ✅ Yes | The initially selected item index (default: 0) |

**Return Type:** `PyControlDropDown`

---

### CreateModernGump
`(x, y, width, height, resizable, minWidth, minHeight, onResized)`
 Creates a modern nine-slice gump using ModernUIConstants for consistent styling.
 The gump uses the standard modern UI panel texture and border size internally.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | X position |
| `y` | `int` | ❌ No | Y position |
| `width` | `int` | ❌ No | Initial width |
| `height` | `int` | ❌ No | Initial height |
| `resizable` | `bool` | ✅ Yes | Whether the gump can be resized by dragging corners (default: true) |
| `minWidth` | `int` | ✅ Yes | Minimum width (default: 50) |
| `minHeight` | `int` | ✅ Yes | Minimum height (default: 50) |
| `onResized` | `object` | ✅ Yes | Optional callback function called when the gump is resized |

**Return Type:** `PyNineSliceGump`

---

### AddControlOnClick
`(control, onClick, leftOnly)`
 Add an onClick callback to a control.
 Example:
 ```py
 def myfunc:
   API.SysMsg("Something clicked!")
 bg = API.CreateGumpColorBox(0.7, "#D4202020")
 API.AddControlOnClick(bg, myfunc)
 while True:
   API.ProcessCallbacks()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `control` | `object` | ❌ No | The control listening for clicks |
| `onClick` | `object` | ❌ No | The callback function |
| `leftOnly` | `bool` | ✅ Yes | Only accept left mouse clicks? |

**Return Type:** `object`

---

### AddControlOnDisposed
`(control, onDispose)`
 Add onDispose(Closed) callback to a control.
 Example:
 ```py
 def onClose():
     API.Stop()

 gump = API.CreateGump()
 gump.SetRect(100, 100, 200, 200)

 bg = API.CreateGumpColorBox(opacity=0.7, color="#000000")
 gump.Add(bg.SetRect(0, 0, 200, 200))

 API.AddControlOnDisposed(gump, onClose)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `control` | `PyBaseControl` | ❌ No |  |
| `onDispose` | `object` | ❌ No |  |

**Return Type:** `PyBaseControl`

---

### GetSkill
`(skill)`
 Get a skill from the player. See the Skill class for what properties are available: https://github.com/PlayTazUO/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs
 Example:
 ```py
 skill = API.GetSkill("Hiding")
 if skill:
   API.SysMsg("Skill: " + skill.Name)
   API.SysMsg("Skill Value: " + str(skill.Value))
   API.SysMsg("Skill Cap: " + str(skill.Cap))
   API.SysMsg("Skill Lock: " + str(skill.Lock))
   ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `skill` | `string` | ❌ No | Skill name, case-sensitive |

**Return Type:** `Skill`

---

### DisplayRange
`(distance, hue)`
 Show a radius around the player.
 Example:
 ```py
 API.DisplayRange(7, 32)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `distance` | `ushort` | ❌ No | Distance from the player |
| `hue` | `ushort` | ✅ Yes | The color to change the tiles at that distance |

**Return Type:** `void` *(Does not return anything)*

---

### ToggleScript
`(scriptName)`
 Toggle another script on or off.
 Example:
 ```py
 API.ToggleScript("MyScript.py")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `scriptName` | `string` | ❌ No | Full name including extension. Can be .py or .lscript. |

**Return Type:** `void` *(Does not return anything)*

---

### PlayScript
`(scriptName)`
 Play a legion script.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `scriptName` | `string` | ❌ No | This is the file name including extension. |

**Return Type:** `void` *(Does not return anything)*

---

### StopScript
`(scriptName)`
 Stop a legion script.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `scriptName` | `string` | ❌ No | This is the file name including extension. |

**Return Type:** `void` *(Does not return anything)*

---

### AddMapMarker
`(name, x, y, map, color)`
 Add a marker to the current World Map (If one is open)
 Example:
 ```py
 API.AddMapMarker("Death")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No |  |
| `x` | `int` | ✅ Yes | Defaults to current player X. |
| `y` | `int` | ✅ Yes | Defaults to current player Y. |
| `map` | `int` | ✅ Yes | Defaults to current map. |
| `color` | `string` | ✅ Yes | red/green/blue/purple/black/yellow/white. Default purple. |

**Return Type:** `void` *(Does not return anything)*

---

### RemoveMapMarker
`(name)`
 Remove a marker from the world map.
 Example:
 ```py
 API.RemoveMapMarker("Death")
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### IsProcessingMoveQueue

 Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.
 Example:
 ```py
 if API.IsProcessingMoveQueue():
   API.Pause(0.5)
 ```


**Return Type:** `bool`

---

### IsProcessingUseItemQueue

 Check if the use item queue is being processed. You can use this to prevent actions if the queue is being processed.
 Example:
 ```py
 if API.IsProcessingUseItemQueue():
   API.Pause(0.5)
 ```


**Return Type:** `bool`

---

### IsGlobalCooldownActive

 Check if the global cooldown is currently active. This applies to actions like moving or using items,
 and prevents new actions from executing until the cooldown has expired.

 Example:
 ```py
 if API.IsGlobalCooldownActive():
     API.Pause(0.5)
 ```


**Return Type:** `bool`

---

### SavePersistentVar
`(name, value, scope)`
 Save a variable that persists between sessions and scripts.
 Example:
 ```py
 API.SavePersistentVar("TotalKills", "5", API.PersistentVar.Char)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No |  |
| `value` | `string` | ❌ No |  |
| `scope` | `PersistentVar` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### RemovePersistentVar
`(name, scope)`
 Delete/remove a persistent variable.
 Example:
 ```py
 API.RemovePersistentVar("TotalKills", API.PersistentVar.Char)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No |  |
| `scope` | `PersistentVar` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### GetPersistentVar
`(name, defaultValue, scope)`
 Get a persistent variable.
 Example:
 ```py
 API.GetPersistentVar("TotalKills", "0", API.PersistentVar.Char)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No |  |
| `defaultValue` | `string` | ❌ No | The value returned if no value was saved |
| `scope` | `PersistentVar` | ❌ No |  |

**Return Type:** `string`

---

### MarkTile
`(x, y, hue, map)`
 Mark a tile with a specific hue.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `hue` | `ushort` | ❌ No |  |
| `map` | `int` | ✅ Yes | Defaults to current map |

**Return Type:** `void` *(Does not return anything)*

---

### RemoveMarkedTile
`(x, y, map)`
 Remove a marked tile. See MarkTile for more info.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `map` | `int` | ✅ Yes |  |

**Return Type:** `void` *(Does not return anything)*

---

### TrackingArrow
`(x, y, identifier)`
 Create a tracking arrow pointing towards a location.
 Set x or y to a negative value to close existing tracker arrow.
 ```py
 API.TrackingArrow(400, 400)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `identifier` | `uint` | ✅ Yes | An identified number if you want multiple arrows. |

**Return Type:** `void` *(Does not return anything)*

---

