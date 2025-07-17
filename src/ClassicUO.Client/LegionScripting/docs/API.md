# Python API Documentation  
This is automatically generated documentation for the Python API scripting.  
All methods, properties, enums, etc need to pre prefaced with `API.` for example: `API.Msg("An example")`.  
  
If you download the [API.py](API.py) file, put it in the same folder as your python scripts and add `import API` to your script, that will enable some mild form of autocomplete in an editor like VS Code.  
You can now type `-updateapi` in game to download the latest API.py file.  
  
[Additional notes](notes.md)  
  
This was generated on `7/17/25`.
  
# API  

## Class Description
 Python scripting access point


## Properties
- **JournalEntries** (*ConcurrentQueue<JournalEntry>*)
- **Backpack** (*uint*)
  -  Get the player's backpack serial

- **Player** (*PlayerMobile*)
  -  Returns the player character object

- **Bank** (*uint*)
  -  Return the player's bank container serial if open, otherwise 0

- **Random** (*Random*)
  -  Can be used for random numbers.
 `API.Random.Next(1, 100)` will return a number between 1 and 100.
 `API.Random.Next(100)` will return a number between 0 and 100.

- **LastTargetSerial** (*uint*)
  -  The serial of the last target, if it has a serial.

- **LastTargetPos** (*Vector3*)
  -  The last target's position

- **LastTargetGraphic** (*ushort*)
  -  The graphic of the last targeting object

- **Found** (*uint*)
  -  The serial of the last item or mobile from the various findtype/mobile methods

- **PyProfile** (*PyProfile*)
  -  Access useful player settings.



## Enums
### ScanType

**Values:**
- Hostile
- Party
- Followers
- Objects
- Mobiles

### Notoriety

**Values:**
- Unknown
- Innocent
- Ally
- Gray
- Criminal
- Enemy
- Murderer
- Invulnerable

### PersistentVar

**Values:**
- Char
- Account
- Server
- Global


## Methods

<details><summary><h3>ProcessCallbacks()</h3></summary>

 Use this when you need to wait for players to click buttons.  
 Example:  
 ```py  
 while True:  
   API.ProcessCallbacks()  
   API.Pause(0.1)  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>SetSharedVar(name, value)</h3></summary>

 Set a variable that is shared between scripts.  
 Example:  
 ```py  
 API.SetSharedVar("myVar", 10)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No | Name of the var |
| value | object | No | Value, can be a number, text, or *most* other objects too. |
---> Does not return anything

</details>

***


<details><summary><h3>GetSharedVar(name)</h3></summary>

 Get the value of a shared variable.  
 Example:  
 ```py  
 myVar = API.GetSharedVar("myVar")  
 if myVar:  
  API.SysMsg(f"myVar is {myVar}")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No | Name of the var |
---> Return Type: *object*

</details>

***


<details><summary><h3>RemoveSharedVar(name)</h3></summary>

 Try to remove a shared variable.  
 Example:  
 ```py  
 API.RemoveSharedVar("myVar")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No | Name of the var |
---> Does not return anything

</details>

***


<details><summary><h3>ClearSharedVars()</h3></summary>

 Clear all shared vars.  
 Example:  
 ```py  
 API.ClearSharedVars()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>CloseGumps()</h3></summary>

 Close all gumps created by the API unless marked to remain open.  
  

---> Does not return anything

</details>

***


<details><summary><h3>Attack(serial)</h3></summary>

 Attack a mobile  
 Example:  
 ```py  
 enemy = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)  
 if enemy:  
   API.Attack(enemy)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>BandageSelf()</h3></summary>

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
  

---> Return Type: *bool*

</details>

***


<details><summary><h3>ClearLeftHand()</h3></summary>

 If you have an item in your left hand, move it to your backpack  
 Sets API.Found to the item's serial.  
 Example:  
 ```py  
 leftHand = API.ClearLeftHand()  
 if leftHand:  
   API.SysMsg("Cleared left hand: " + leftHand.Name)  
 ```  
  

---> Return Type: *uint*

</details>

***


<details><summary><h3>ClearRightHand()</h3></summary>

 If you have an item in your right hand, move it to your backpack  
 Sets API.Found to the item's serial.  
 Example:  
 ```py  
 rightHand = API.ClearRightHand()  
 if rightHand:  
   API.SysMsg("Cleared right hand: " + rightHand.Name)  
  ```  
  

---> Return Type: *uint*

</details>

***


<details><summary><h3>ClickObject(serial)</h3></summary>

 Single click an object  
 Example:  
 ```py  
 API.ClickObject(API.Player)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial, or item/mobile reference |
---> Does not return anything

</details>

***


<details><summary><h3>UseObject(serial, skipQueue)</h3></summary>

 Attempt to use(double click) an object.  
 Example:  
 ```py  
 API.UseObject(API.Backpack)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The serial |
| skipQueue | bool | Yes | Defaults true, set to false to use a double click queue |
---> Does not return anything

</details>

***


<details><summary><h3>Contents(serial)</h3></summary>

 Get an item count for the contents of a container  
 Example:  
 ```py  
 count = API.Contents(API.Backpack)  
 if count > 0:  
   API.SysMsg(f"You have {count} items in your backpack")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Return Type: *int*

</details>

***


<details><summary><h3>ContextMenu(serial, entry)</h3></summary>

 Send a context menu(right click menu) response.  
 This does not open the menu, you do not need to open the menu first. This handles both in one action.  
 Example:  
 ```py  
 API.ContextMenu(API.Player, 1)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| entry | ushort | No | Entries start at 0, the top entry will be 0, then 1, 2, etc. (Usually) |
---> Does not return anything

</details>

***


<details><summary><h3>EquipItem(serial)</h3></summary>

 Attempt to equip an item. Layer is automatically detected.  
 Example:  
 ```py  
 lefthand = API.ClearLeftHand()  
 API.Pause(2)  
 API.EquipItem(lefthand)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>ClearMoveQueue()</h3></summary>

 Clear the move item que of all items.  
  

---> Does not return anything

</details>

***


<details><summary><h3>QueMoveItem(serial, destination, amt, x, y)</h3></summary>

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
             API.QueMoveItem(item, barrel)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| destination | uint | No |  |
| amt | ushort | Yes | Amount to move |
| x | int | Yes | X coordinate inside a container |
| y | int | Yes | Y coordinate inside a container |
---> Does not return anything

</details>

***


<details><summary><h3>MoveItem(serial, destination, amt, x, y)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| destination | uint | No |  |
| amt | int | Yes | Amount to move |
| x | int | Yes | X coordinate inside a container |
| y | int | Yes | Y coordinate inside a container |
---> Does not return anything

</details>

***


<details><summary><h3>QueMoveItemOffset(serial, amt, x, y, z, OSI)</h3></summary>

 Move an item to the ground near you.  
 Example:  
 ```py  
 items = API.ItemsInContainer(API.Backpack)  
 for item in items:  
   API.QueMoveItemOffset(item, 0, 1, 0, 0)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| amt | ushort | Yes | 0 to grab entire stack |
| x | int | Yes | Offset from your location |
| y | int | Yes | Offset from your location |
| z | int | Yes | Offset from your location. Leave blank in most cases |
| OSI | bool | Yes | True if you are playing OSI |
---> Does not return anything

</details>

***


<details><summary><h3>MoveItemOffset(serial, amt, x, y, z, OSI)</h3></summary>

 Move an item to the ground near you.  
 Example:  
 ```py  
 items = API.ItemsInContainer(API.Backpack)  
 for item in items:  
   API.MoveItemOffset(item, 0, 1, 0, 0)  
   API.Pause(0.75)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| amt | int | Yes | 0 to grab entire stack |
| x | int | Yes | Offset from your location |
| y | int | Yes | Offset from your location |
| z | int | Yes | Offset from your location. Leave blank in most cases |
| OSI | bool | Yes | True if you are playing OSI |
---> Does not return anything

</details>

***


<details><summary><h3>UseSkill(skillName)</h3></summary>

 Use a skill.  
 Example:  
 ```py  
 API.UseSkill("Hiding")  
 API.Pause(11)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skillName | string | No | Can be a partial match. Will match the first skill containing this text. |
---> Does not return anything

</details>

***


<details><summary><h3>CastSpell(spellName)</h3></summary>

 Attempt to cast a spell by its name.  
 Example:  
 ```py  
 API.CastSpell("Fireball")  
 API.WaitForTarget()  
 API.Target(API.Player)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| spellName | string | No | This can be a partial match. Fireba will cast Fireball. |
---> Does not return anything

</details>

***


<details><summary><h3>BuffExists(buffName)</h3></summary>

 Check if a buff is active.  
 Example:  
 ```py  
 if API.BuffExists("Bless"):  
   API.SysMsg("You are blessed!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| buffName | string | No | The name/title of the buff |
---> Return Type: *bool*

</details>

***


<details><summary><h3>ActiveBuffs()</h3></summary>

 Get a list of all buffs that are active.  
 See [Buff](Buff.md) to see what attributes are available.  
 Buff does not get updated after you access it in python, you will need to call this again to get the latest buff data.  
 Example:  
 ```py  
 buffs = API.ActiveBuffs()  
 for buff in buffs:  
     API.SysMsg(buff.Title)  
 ```  
  

---> Return Type: *Buff[]*

</details>

***


<details><summary><h3>SysMsg(message, hue)</h3></summary>

 Show a system message(Left side of screen).  
 Example:  
 ```py  
 API.SysMsg("Script started!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | Message |
| hue | ushort | Yes | Color of the message |
---> Does not return anything

</details>

***


<details><summary><h3>Msg(message)</h3></summary>

 Say a message outloud.  
 Example:  
 ```py  
 API.Say("Hello friend!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message to say |
---> Does not return anything

</details>

***


<details><summary><h3>HeadMsg(message, serial, hue)</h3></summary>

 Show a message above a mobile or item, this is only visible to you.  
 Example:  
 ```py  
 API.HeadMsg("Only I can see this!", API.Player)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message |
| serial | uint | No | The item or mobile |
| hue | ushort | Yes | Message hue |
---> Does not return anything

</details>

***


<details><summary><h3>PartyMsg(message)</h3></summary>

 Send a message to your party.  
 Example:  
 ```py  
 API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message |
---> Does not return anything

</details>

***


<details><summary><h3>GuildMsg(message)</h3></summary>

 Send your guild a message.  
 Example:  
 ```py  
 API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>AllyMsg(message)</h3></summary>

 Send a message to your alliance.  
 Example:  
 ```py  
 API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>WhisperMsg(message)</h3></summary>

 Whisper a message.  
 Example:  
 ```py  
 API.WhisperMsg("Psst, bet you didn't see me here..")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>YellMsg(message)</h3></summary>

 Yell a message.  
 Example:  
 ```py  
 API.YellMsg("Vendor restocked, get your fresh feathers!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>EmoteMsg(message)</h3></summary>

 Emote a message.  
 Example:  
 ```py  
 API.EmoteMsg("laughing")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>FindItem(serial)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The serial |
---> Return Type: *Item*

</details>

***


<details><summary><h3>FindType(graphic, container, range, hue, minamount)</h3></summary>

 Attempt to find an item by type(graphic).  
 Sets API.Found to the serial of the item found.  
 Example:  
 ```py  
 item = API.FindType(0x0EED, API.Backpack)  
 if item:  
   API.SysMsg("Found the item!")  
   API.UseObject(item)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type of item to find |
| container | uint | Yes | Container to search |
| range | ushort | Yes | Max range of item |
| hue | ushort | Yes | Hue of item |
| minamount | ushort | Yes | Only match if item stack is at least this much |
---> Return Type: *Item*

</details>

***


<details><summary><h3>FindTypeAll(graphic, container, range, hue, minamount)</h3></summary>

 Return a list of items matching the parameters set.  
 Example:  
 ```py  
 items = API.FindTypeAll(0x0EED, API.Backpack)  
 if items:  
   API.SysMsg("Found " + str(len(items)) + " items!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type of item to find |
| container | uint | Yes | Container to search |
| range | ushort | Yes | Max range of item(if on ground) |
| hue | ushort | Yes | Hue of item |
| minamount | ushort | Yes | Only match if item stack is at least this much |
---> Return Type: *Item[]*

</details>

***


<details><summary><h3>FindLayer(layer, serial)</h3></summary>

 Attempt to find an item on a layer.  
 Sets API.Found to the serial of the item found.  
 Example:  
 ```py  
 item = API.FindLayer("Helmet")  
 if item:  
   API.SysMsg("Wearing a helmet!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| layer | string | No | The layer to check, see https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs |
| serial | uint | Yes | Optional, if not set it will check yourself, otherwise it will check the mobile requested |
---> Return Type: *Item*

</details>

***


<details><summary><h3>ItemsInContainer(container, recursive)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| container | uint | No |  |
| recursive | bool | Yes | Search sub containers also? |
---> Return Type: *Item[]*

</details>

***


<details><summary><h3>UseType(graphic, hue, container, skipQueue)</h3></summary>

 Attempt to use the first item found by graphic(type).  
 Example:  
 ```py  
 API.UseType(0x3434, API.Backpack)  
 API.WaitForTarget()  
 API.Target(API.Player)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type |
| hue | ushort | Yes | Hue of item |
| container | uint | Yes | Parent container |
| skipQueue | bool | Yes | Defaults to true, set to false to queue the double click |
---> Does not return anything

</details>

***


<details><summary><h3>CreateCooldownBar(seconds, text, hue)</h3></summary>

 Create a cooldown bar.  
 Example:  
 ```py  
 API.CreateCooldownBar(5, "Healing", 21)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| seconds | double | No | Duration in seconds for the cooldown bar |
| text | string | No | Text on the cooldown bar |
| hue | ushort | No | Hue to color the cooldown bar |
---> Does not return anything

</details>

***


<details><summary><h3>IgnoreObject(serial)</h3></summary>

 Adds an item or mobile to your ignore list.  
 These are unique lists per script. Ignoring an item in one script, will not affect other running scripts.  
 Example:  
 ```py  
 for item in ItemsInContainer(API.Backpack):  
   if item.Name == "Dagger":  
   API.IgnoreObject(item)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The item/mobile serial |
---> Does not return anything

</details>

***


<details><summary><h3>ClearIgnoreList()</h3></summary>

 Clears the ignore list. Allowing functions to see those items again.  
 Example:  
 ```py  
 API.ClearIgnoreList()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>OnIgnoreList(serial)</h3></summary>

 Check if a serial is on the ignore list.  
 Example:  
 ```py  
 if API.OnIgnoreList(API.Backpack):  
   API.SysMsg("Currently ignoring backpack")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Return Type: *bool*

</details>

***


<details><summary><h3>Pathfind(x, y, z, distance, wait, timeout)</h3></summary>

 Attempt to pathfind to a location.  This will fail with large distances.  
 Example:  
 ```py  
 API.Pathfind(1414, 1515)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
| z | int | Yes |  |
| distance | int | Yes | Distance away from goal to stop. |
| wait | bool | Yes | True/False if you want to wait for pathfinding to complete or time out |
| timeout | int | Yes | Seconds to wait before cancelling waiting |
---> Return Type: *bool*

</details>

***


<details><summary><h3>PathfindEntity(entity, distance, wait, timeout)</h3></summary>

 Attempt to pathfind to a mobile or item.  
 Example:  
 ```py  
 mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)  
 if mob:  
   API.PathfindEntity(mob)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| entity | uint | No | The mobile or item |
| distance | int | Yes | Distance to stop from goal |
| wait | bool | Yes | True/False if you want to wait for pathfinding to complete or time out |
| timeout | int | Yes | Seconds to wait before cancelling waiting |
---> Return Type: *bool*

</details>

***


<details><summary><h3>Pathfinding()</h3></summary>

 Check if you are already pathfinding.  
 Example:  
 ```py  
 if API.Pathfinding():  
   API.SysMsg("Pathfinding...!")  
   API.Pause(0.25)  
 ```  
  

---> Return Type: *bool*

</details>

***


<details><summary><h3>CancelPathfinding()</h3></summary>

 Cancel pathfinding.  
 Example:  
 ```py  
 if API.Pathfinding():  
   API.CancelPathfinding()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>GetPath(x, y, z, distance)</h3></summary>

 Attempt to build a path to a location.  This will fail with large distances.  
 Example:  
 ```py  
 API.GetPath(1414, 1515)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
| z | int | Yes |  |
| distance | int | Yes | Distance away from goal to stop. |
---> Return Type: *List[Tuple[int, int, int]]*

</details>

***


<details><summary><h3>AutoFollow(mobile)</h3></summary>

 Automatically follow a mobile. This is different than pathfinding. This will continune to follow the mobile.  
 Example:  
 ```py  
 mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)  
 if mob:  
   API.AutoFollow(mob)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| mobile | uint | No | The mobile |
---> Does not return anything

</details>

***


<details><summary><h3>CancelAutoFollow()</h3></summary>

 Cancel auto follow mode.  
 Example:  
 ```py  
 if API.Pathfinding():  
   API.CancelAutoFollow()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>Run(direction)</h3></summary>

 Run in a direction.  
 Example:  
 ```py  
 API.Run("north")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north/northeast/south/west/etc |
---> Does not return anything

</details>

***


<details><summary><h3>Walk(direction)</h3></summary>

 Walk in a direction.  
 Example:  
 ```py  
 API.Walk("north")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north/northeast/south/west/etc |
---> Does not return anything

</details>

***


<details><summary><h3>Turn(direction)</h3></summary>

 Turn your character a specific direction.  
 Example:  
 ```py  
 API.Turn("north")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north, northeast, etc |
---> Does not return anything

</details>

***


<details><summary><h3>Rename(serial, name)</h3></summary>

 Attempt to rename something like a pet.  
 Example:  
 ```py  
 API.Rename(0x12345678, "My Handsome Pet")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial of the mobile to rename |
| name | string | No | The new name |
---> Does not return anything

</details>

***


<details><summary><h3>Dismount()</h3></summary>

 Attempt to dismount if mounted.  
 Example:  
 ```py  
 API.Dismount()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>Mount(serial)</h3></summary>

 Attempt to mount(double click)  
 Example:  
 ```py  
 API.Mount(0x12345678)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>WaitForTarget(targetType, timeout)</h3></summary>

 Wait for a target cursor.  
 Example:  
 ```py  
 API.WaitForTarget()  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| targetType | string | Yes | neutral/harmful/beneficial/any/harm/ben |
| timeout | double | Yes | Max duration in seconds to wait |
---> Return Type: *bool*

</details>

***


<details><summary><h3>Target(serial)</h3></summary>

 Target an item or mobile.  
 Example:  
 ```py  
 if API.WaitForTarget():  
   API.Target(0x12345678)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial of the item/mobile to target |
---> Does not return anything

</details>

***


<details><summary><h3>Target(x, y, z, graphic)</h3></summary>

 Target a location. Include graphic if targeting a static.  
 Example:  
 ```py  
 if API.WaitForTarget():  
   API.Target(1243, 1337, 0)  
  ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | ushort | No |  |
| y | ushort | No |  |
| z | short | No |  |
| graphic | ushort | Yes | Graphic of the static to target |
---> Does not return anything

</details>

***


<details><summary><h3>RequestTarget(timeout)</h3></summary>

 Request the player to target something.  
 Example:  
 ```py  
 target = API.RequestTarget()  
 if target:  
   API.SysMsg("Targeted serial: " + str(target))  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| timeout | double | Yes | Mac duration to wait for them to target something. |
---> Return Type: *uint*

</details>

***


<details><summary><h3>TargetSelf()</h3></summary>

 Target yourself.  
 Example:  
 ```py  
 API.TargetSelf()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>TargetLandRel(xOffset, yOffset)</h3></summary>

 Target a land tile relative to your position.  
 If this doesn't work, try TargetTileRel instead.  
 Example:  
 ```py  
 API.TargetLand(1, 1)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| xOffset | int | No | X from your position |
| yOffset | int | No | Y from your position |
---> Does not return anything

</details>

***


<details><summary><h3>TargetTileRel(xOffset, yOffset, graphic)</h3></summary>

 Target a tile relative to your location.  
 If this doesn't work, try TargetLandRel instead.'  
 Example:  
 ```py  
 API.TargetTileRel(1, 1)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| xOffset | int | No | X Offset from your position |
| yOffset | int | No | Y Offset from your position |
| graphic | ushort | Yes | Optional graphic, will try to use the graphic of the tile at that location if left empty. |
---> Does not return anything

</details>

***


<details><summary><h3>CancelTarget()</h3></summary>

 Cancel targeting.  
 Example:  
 ```py  
 if API.WaitForTarget():  
   API.CancelTarget()  
   API.SysMsg("Targeting cancelled, april fools made you target something!")  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>HasTarget(targetType)</h3></summary>

 Check if the player has a target cursor.  
 Example:  
 ```py  
 if API.HasTarget():  
     API.CancelTarget()  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| targetType | string | Yes | neutral/harmful/beneficial/any/harm/ben |
---> Return Type: *bool*

</details>

***


<details><summary><h3>GetMap()</h3></summary>

 Get the current map index.  
 Standard maps are:  
 0 = Fel  
 1 = Tram  
 2 = Ilshenar  
 3 = Malas  
 4 = Tokuno  
 5 = TerMur  
  

---> Return Type: *int*

</details>

***


<details><summary><h3>SetSkillLock(skill, up_down_locked)</h3></summary>

 Set a skills lock status.  
 Example:  
 ```py  
 API.SetSkillLock("Hiding", "locked")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skill | string | No | The skill name, can be partia; |
| up_down_locked | string | No | up/down/locked |
---> Does not return anything

</details>

***


<details><summary><h3>SetStatLock(stat, up_down_locked)</h3></summary>

 Set a skills lock status.  
 Example:  
 ```py  
 API.SetStatLock("str", "locked")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| stat | string | No | The stat name, str, dex, int; Defaults to str. |
| up_down_locked | string | No | up/down/locked |
---> Does not return anything

</details>

***


<details><summary><h3>Logout()</h3></summary>

 Logout of the game.  
 Example:  
 ```py  
 API.Logout()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>ItemNameAndProps(serial, wait, timeout)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| wait | bool | Yes | True or false to wait for name and props |
| timeout | int | Yes | Timeout in seconds |
---> Return Type: *string*

</details>

***


<details><summary><h3>HasGump(ID)</h3></summary>

 Check if a player has a server gump. Leave blank to check if they have any server gump.  
 Example:  
 ```py  
 if API.HasGump(0x12345678):  
   API.SysMsg("Found a gump!")  
```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ID | uint | Yes | Skip to check if player has any gump from server. |
---> Return Type: *uint*

</details>

***


<details><summary><h3>ReplyGump(button, gump)</h3></summary>

 Reply to a gump.  
 Example:  
 ```py  
 API.ReplyGump(21)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| button | int | No | Button ID |
| gump | uint | Yes | Gump ID, leave blank to reply to last gump |
---> Return Type: *bool*

</details>

***


<details><summary><h3>CloseGump(ID)</h3></summary>

 Close the last gump open, or a specific gump.  
 Example:  
 ```py  
 API.CloseGump()  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ID | uint | Yes | Gump ID |
---> Does not return anything

</details>

***


<details><summary><h3>GumpContains(text, ID)</h3></summary>

 Check if a gump contains a specific text.  
 Example:  
 ```py  
 if API.GumpContains("Hello"):  
   API.SysMsg("Found the text!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No | Can be regex if you start with $, otherwise it's just regular search. Case Sensitive. |
| ID | uint | Yes | Gump ID, blank to use the last gump. |
---> Return Type: *bool*

</details>

***


<details><summary><h3>GetGump(ID)</h3></summary>

 Get a gump by ID.  
 Example:  
 ```py  
 gump = API.GetGump()  
 if gump:  
   API.SysMsg("Found the gump!")  
   API.CloseGump(gump)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ID | uint | Yes | Leabe blank to use last gump opened from server |
---> Return Type: *Gump*

</details>

***


<details><summary><h3>ToggleFly()</h3></summary>

 Toggle flying if you are a gargoyle.  
 Example:  
 ```py  
 API.ToggleFly()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>ToggleAbility(ability)</h3></summary>

 Toggle an ability.  
 Example:  
 ```py  
 if not API.PrimaryAbilityActive():  
   API.ToggleAbility("primary")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ability | string | No | primary/secondary/stun/disarm |
---> Does not return anything

</details>

***


<details><summary><h3>PrimaryAbilityActive()</h3></summary>

 Check if your primary ability is active.  
 Example:  
 ```py  
 if API.PrimaryAbilityActive():  
   API.SysMsg("Primary ability is active!")  
 ```  
  

---> Return Type: *bool*

</details>

***


<details><summary><h3>SecondaryAbilityActive()</h3></summary>

 Check if your secondary ability is active.  
 Example:  
 ```py  
 if API.SecondaryAbilityActive():  
   API.SysMsg("Secondary ability is active!")  
 ```  
  

---> Return Type: *bool*

</details>

***


<details><summary><h3>InJournal(msg)</h3></summary>

 Check if your journal contains a message.  
 Example:  
 ```py  
 if API.InJournal("You have been slain"):  
   API.SysMsg("You have been slain!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| msg | string | No | The message to check for. Can be regex, prepend your msg with $ |
---> Return Type: *bool*

</details>

***


<details><summary><h3>InJournalAny(msgs)</h3></summary>

 Check if the journal contains *any* of the strings in this list.  
 Can be regex, prepend your msgs with $  
 Example:  
 ```py  
 if API.InJournalAny(["You have been slain", "You are dead"]):  
   API.SysMsg("You have been slain or dead!")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| msgs | IList<string> | No |  |
---> Return Type: *bool*

</details>

***


<details><summary><h3>ClearJournal()</h3></summary>

 Clear your journal(This is specific for each script).  
 Example:  
 ```py  
 API.ClearJournal()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>Pause(seconds)</h3></summary>

 Pause the script.  
 Example:  
 ```py  
 API.Pause(5)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| seconds | double | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>Stop()</h3></summary>

 Stops the current script.  
 Example:  
 ```py  
 API.Stop()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>ToggleAutoLoot()</h3></summary>

 Toggle autolooting on or off.  
 Example:  
 ```py  
 API.ToggleAutoLoot()  
 ```  
  

---> Does not return anything

</details>

***


<details><summary><h3>AutoLootContainer(container)</h3></summary>

 Use autoloot on a specific container.  
 Example:  
 ```py  
 targ = API.RequestTarget()  
 if targ:  
   API.AutoLootContainer(targ)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| container | uint | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>Virtue(virtue)</h3></summary>

 Use a virtue.  
 Example:  
 ```py  
 API.Virtue("honor")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| virtue | string | No | honor/sacrifice/valor |
---> Does not return anything

</details>

***


<details><summary><h3>NearestEntity(scanType, maxDistance)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| scanType | ScanType | No |  |
| maxDistance | int | Yes |  |
---> Return Type: *Entity*

</details>

***


<details><summary><h3>NearestMobile(notoriety, maxDistance)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| notoriety | IList<Notoriety> | No | List of notorieties |
| maxDistance | int | Yes |  |
---> Return Type: *Mobile*

</details>

***


<details><summary><h3>NearestCorpse(distance)</h3></summary>

 Get the nearest corpse within a distance.  
 Sets API.Found to the serial of the corpse.  
 Example:  
 ```py  
 corpse = API.NearestCorpse()  
 if corpse:  
   API.SysMsg("Found a corpse!")  
   API.UseObject(corpse)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| distance | int | Yes |  |
---> Return Type: *Item*

</details>

***


<details><summary><h3>NearestMobiles(notoriety, maxDistance)</h3></summary>

 Get all mobiles matching Notoriety and distance.  
 Example:  
 ```py  
 mob = API.NearestMobiles([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)  
 if len(mob) > 0:  
   API.SysMsg("Found enemies!")  
   API.Msg("Guards!")  
   API.Attack(mob[0])  
   ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| notoriety | IList<Notoriety> | No | List of notorieties |
| maxDistance | int | Yes |  |
---> Return Type: *Mobile[]*

</details>

***


<details><summary><h3>FindMobile(serial)</h3></summary>

 Get a mobile from its serial.  
 Sets API.Found to the serial of the mobile.  
 Example:  
 ```py  
 mob = API.FindMobile(0x12345678)  
 if mob:  
   API.SysMsg("Found the mobile!")  
   API.UseObject(mob)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
---> Return Type: *Mobile*

</details>

***


<details><summary><h3>GetAllMobiles()</h3></summary>

 Return a list of all mobiles the client is aware of.  
 Example:  
 ```py  
 mobiles = API.GetAllMobiles()  
 if mobiles:  
   API.SysMsg("Found " + str(len(mobiles)) + " mobiles!")  
   for mob in mobiles:  
     API.SysMsg(mob.Name)  
     API.Pause(0.5)  
 ```  
  

---> Return Type: *Mobile[]*

</details>

***


<details><summary><h3>GetTile(x, y)</h3></summary>

 Get the tile at a location.  
 Example:  
 ```py  
 tile = API.GetTile(1414, 1515)  
 if tile:  
   API.SysMsg(f"Found a tile with graphic: {tile.Graphic}")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
---> Return Type: *GameObject*

</details>

***


<details><summary><h3>CreateGump(acceptMouseInput, canMove, keepOpen)</h3></summary>

 Get a blank gump.  
 Example:  
 ```py  
 g = API.CreateGump()  
 g.SetRect(100, 100, 200, 200)  
 g.Add(API.CreateGumpLabel("Hello World!"))  
 API.AddGump(g)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| acceptMouseInput | bool | Yes | Allow clicking the gump |
| canMove | bool | Yes | Allow the player to move this gump |
| keepOpen | bool | Yes | If true, the gump won't be closed if the script stops. Otherwise, it will be closed when the script is stopped. Defaults to false. |
---> Return Type: *Gump*

</details>

***


<details><summary><h3>AddGump(g)</h3></summary>

 Add a gump to the players screen.  
 Example:  
 ```py  
 g = API.CreateGump()  
 g.SetRect(100, 100, 200, 200)  
 g.Add(API.CreateGumpLabel("Hello World!"))  
 API.AddGump(g)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| g | Gump | No | The gump to add |
---> Does not return anything

</details>

***


<details><summary><h3>CreateGumpCheckbox(text, hue, isChecked)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes | Optional text label |
| hue | ushort | Yes | Optional hue |
| isChecked | bool | Yes | Default false, set to true if you want this checkbox checked on creation |
---> Return Type: *Checkbox*

</details>

***


<details><summary><h3>CreateGumpLabel(text, hue)</h3></summary>

 Create a label for a gump.  
 Example:  
 ```py  
 g = API.CreateGump()  
 g.SetRect(100, 100, 200, 200)  
 g.Add(API.CreateGumpLabel("Hello World!"))  
 API.AddGump(g)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No | The text |
| hue | ushort | Yes | The hue of the text |
---> Return Type: *Label*

</details>

***


<details><summary><h3>CreateGumpColorBox(opacity, color)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| opacity | float | Yes | 0.5 = 50% |
| color | string | Yes | Html color code like #000000 |
---> Return Type: *AlphaBlendControl*

</details>

***


<details><summary><h3>CreateGumpItemPic(graphic, width, height)</h3></summary>

 Create a picture of an item.  
 Example:  
 ```py  
 g = API.CreateGump()  
 g.SetRect(100, 100, 200, 200)  
 g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))  
 API.AddGump(g)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No |  |
| width | int | No |  |
| height | int | No |  |
---> Return Type: *ResizableStaticPic*

</details>

***


<details><summary><h3>CreateGumpButton(text, hue, normal, pressed, hover)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes |  |
| hue | ushort | Yes |  |
| normal | ushort | Yes | Graphic when not clicked or hovering |
| pressed | ushort | Yes | Graphic when pressed |
| hover | ushort | Yes | Graphic on hover |
---> Return Type: *Button*

</details>

***


<details><summary><h3>CreateSimpleButton(text, width, height)</h3></summary>

 Create a simple button, does not use graphics.  
 Example:  
 ```py  
 g = API.CreateGump()  
 g.SetRect(100, 100, 200, 200)  
 button = API.CreateSimpleButton("Click Me!", 100, 20)  
 g.Add(button)  
 API.AddGump(g)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No |  |
| width | int | No |  |
| height | int | No |  |
---> Return Type: *NiceButton*

</details>

***


<details><summary><h3>CreateGumpRadioButton(text, group, inactive, active, hue, isChecked)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes | Optional text |
| group | int | Yes | Group ID |
| inactive | ushort | Yes | Unchecked graphic |
| active | ushort | Yes | Checked graphic |
| hue | ushort | Yes | Text color |
| isChecked | bool | Yes | Defaults false, set to true if you want this button checked by default. |
---> Return Type: *RadioButton*

</details>

***


<details><summary><h3>CreateGumpTextBox(text, width, height, multiline)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes |  |
| width | int | Yes |  |
| height | int | Yes |  |
| multiline | bool | Yes |  |
---> Return Type: *TTFTextInputField*

</details>

***


<details><summary><h3>CreateGumpTTFLabel(text, size, color, font, aligned, maxWidth, applyStroke)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No |  |
| size | float | No | Font size |
| color | string | Yes | Hex color: #FFFFFF. Must begin with #. |
| font | string | Yes | Must have the font installed in TazUO |
| aligned | string | Yes | left/center/right. Must set a max width for this to work. |
| maxWidth | int | Yes | Max width before going to the next line |
| applyStroke | bool | Yes | Uses players stroke settings, this turns it on or off |
---> Return Type: *TextBox*

</details>

***


<details><summary><h3>CreateGumpSimpleProgressBar(width, height, backgroundColor, foregroundColor, value, max)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| width | int | No | The width of the bar |
| height | int | No | The height of the bar |
| backgroundColor | string | Yes | The background color(Hex color like #616161) |
| foregroundColor | string | Yes | The foreground color(Hex color like #212121) |
| value | int | Yes | The current value, for example 70 |
| max | int | Yes | The max value(or what would be 100%), for example 100 |
---> Return Type: *SimpleProgressBar*

</details>

***


<details><summary><h3>CreateGumpScrollArea(x, y, width, height)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
| width | int | No |  |
| height | int | No |  |
---> Return Type: *ScrollArea*

</details>

***


<details><summary><h3>CreateGumpPic(graphic, x, y, hue)</h3></summary>

 Create a gump pic(Use this for gump art, not item art)  
 Example:  
 ```py  
 gumpPic = API.CreateGumpPic(0xafb)  
 gump.Add(gumpPic)  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | ushort | No |  |
| x | int | Yes |  |
| y | int | Yes |  |
| hue | ushort | Yes |  |
---> Return Type: *GumpPic*

</details>

***


<details><summary><h3>AddControlOnClick(control, onClick, leftOnly)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| control | Control | No | The control listening for clicks |
| onClick | object | No | The callback function |
| leftOnly | bool | Yes | Only accept left mouse clicks? |
---> Return Type: *Control*

</details>

***


<details><summary><h3>AddControlOnDisposed(control, onDispose)</h3></summary>

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
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| control | Control | No |  |
| onDispose | object | No |  |
---> Return Type: *Control*

</details>

***


<details><summary><h3>GetSkill(skill)</h3></summary>

 Get a skill from the player. See the Skill class for what properties are available: https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs  
 Example:  
 ```py  
 skill = API.GetSkill("Hiding")  
 if skill:  
   API.SysMsg("Skill: " + skill.Name)  
   API.SysMsg("Skill Value: " + str(skill.Value))  
   API.SysMsg("Skill Cap: " + str(skill.Cap))  
   API.SysMsg("Skill Lock: " + str(skill.Lock))  
   ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skill | string | No | Skill name, case-sensitive |
---> Return Type: *Skill*

</details>

***


<details><summary><h3>DisplayRange(distance, hue)</h3></summary>

 Show a radius around the player.  
 Example:  
 ```py  
 API.DisplayRange(7, 32)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| distance | ushort | No | Distance from the player |
| hue | ushort | Yes | The color to change the tiles at that distance |
---> Does not return anything

</details>

***


<details><summary><h3>ToggleScript(scriptName)</h3></summary>

 Toggle another script on or off.  
 Example:  
 ```py  
 API.ToggleScript("MyScript.py")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| scriptName | string | No | Full name including extension. Can be .py or .lscript. |
---> Does not return anything

</details>

***


<details><summary><h3>PlayScript(scriptName)</h3></summary>

 Play a legion script.  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| scriptName | string | No | This is the file name including extension. |
---> Does not return anything

</details>

***


<details><summary><h3>StopScript(scriptName)</h3></summary>

 Stop a legion script.  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| scriptName | string | No | This is the file name including extension. |
---> Does not return anything

</details>

***


<details><summary><h3>AddMapMarker(name, x, y, map, color)</h3></summary>

 Add a marker to the current World Map (If one is open)  
 Example:  
 ```py  
 API.AddMapMarker("Death")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No |  |
| x | int | Yes | Defaults to current player X. |
| y | int | Yes | Defaults to current player Y. |
| map | int | Yes | Defaults to current map. |
| color | string | Yes | red/green/blue/purple/black/yellow/white. Default purple. |
---> Does not return anything

</details>

***


<details><summary><h3>RemoveMapMarker(name)</h3></summary>

 Remove a marker from the world map.  
 Example:  
 ```py  
 API.RemoveMapMarker("Death")  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>IsProcessingMoveQue()</h3></summary>

 Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.  
 Example:  
 ```py  
 if API.IsProcessingMoveQue():  
   API.Pause(0.5)  
 ```  
  

---> Return Type: *bool*

</details>

***


<details><summary><h3>SavePersistentVar(name, value, scope)</h3></summary>

 Save a variable that persists between sessions and scripts.  
 Example:  
 ```py  
 API.SavePersistentVar("TotalKills", "5", API.PersistentVar.Char)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No |  |
| value | string | No |  |
| scope | PersistentVar | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>RemovePersistentVar(name, scope)</h3></summary>

 Delete/remove a persistent variable.  
 Example:  
 ```py  
 API.RemovePersistentVar("TotalKills", API.PersistentVar.Char)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No |  |
| scope | PersistentVar | No |  |
---> Does not return anything

</details>

***


<details><summary><h3>GetPersistentVar(name, defaultValue, scope)</h3></summary>

 Get a persistent variable.  
 Example:  
 ```py  
 API.GetPersistentVar("TotalKills", "0", API.PersistentVar.Char)  
 ```  
  

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| name | string | No |  |
| defaultValue | string | No | The value returned if no value was saved |
| scope | PersistentVar | No |  |
---> Return Type: *string*

</details>

***

