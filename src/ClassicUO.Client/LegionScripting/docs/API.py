class Buff:
    Graphic: int = None
    Text: str = None
    Timer: int = None
    Type = None
    Title: str = None

class PyControl:

    def SetRect(x: int, y: int, w: int, h: int) -> "PyControl":
        """
         Used in python API
        
        """
        pass

    def SetWidth(width: int) -> "PyControl":
        """
         Used in python API
        
        """
        pass

    def SetHeight(height: int) -> "PyControl":
        """
         Used in python API
        
        """
        pass

    def SetX(x: int) -> "PyControl":
        """
         Used in python API
        
        """
        pass

    def SetY(y: int) -> "PyControl":
        """
         Used in python API
        
        """
        pass

    def SetPos(x: int, y: int) -> "PyControl":
        """
         Use int python API
        
        """
        pass

    def GetX() -> int:
        """
         Used in python API
        
        """
        pass

    def GetY() -> int:
        """
         Used in python API
        
        """
        pass

class PyProfile:
    CharacterName: str = None
    ServerName: str = None
    LootBagSerial: int = None
    FavoriteBagSerial: int = None
    MoveItemDelay: int = None
    AutoLootEnabled: bool = None

JournalEntries = None
Backpack: int = None
Player = None
Bank: int = None
Random = None
LastTargetSerial: int = None
LastTargetPos = None
LastTargetGraphic: int = None
Found: int = None
PyProfile: PyProfile = None

class ScanType:
    Hostile = 0
    Party = 1
    Followers = 2
    Objects = 3
    Mobiles = 4

class Notoriety:
    Unknown = 1
    Innocent = 1
    Ally = 1
    Gray = 1
    Criminal = 1
    Enemy = 1
    Murderer = 1
    Invulnerable = 1

class PersistentVar:
    Char = 1
    Account = 2
    Server = 3
    Global = 4

def ProcessCallbacks() -> None:
    """
     Use this when you need to wait for players to click buttons.
     Example:
     ```py
     while True:
       API.ProcessCallbacks()
       API.Pause(0.1)
     ```
    
    """
    pass

def SetSharedVar(name: str, value: Any) -> None:
    """
     Set a variable that is shared between scripts.
     Example:
     ```py
     API.SetSharedVar("myVar", 10)
     ```
    
    """
    pass

def GetSharedVar(name: str) -> Any:
    """
     Get the value of a shared variable.
     Example:
     ```py
     myVar = API.GetSharedVar("myVar")
     if myVar:
      API.SysMsg(f"myVar is {myVar}")
     ```
    
    """
    pass

def RemoveSharedVar(name: str) -> None:
    """
     Try to remove a shared variable.
     Example:
     ```py
     API.RemoveSharedVar("myVar")
     ```
    
    """
    pass

def ClearSharedVars() -> None:
    """
     Clear all shared vars.
     Example:
     ```py
     API.ClearSharedVars()
     ```
    
    """
    pass

def CloseGumps() -> None:
    """
     Close all gumps created by the API unless marked to remain open.
    
    """
    pass

def Attack(serial: int) -> None:
    """
     Attack a mobile
     Example:
     ```py
     enemy = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
     if enemy:
       API.Attack(enemy)
     ```
    
    """
    pass

def BandageSelf() -> bool:
    """
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
    
    """
    pass

def ClearLeftHand() -> int:
    """
     If you have an item in your left hand, move it to your backpack
     Sets API.Found to the item's serial.
     Example:
     ```py
     leftHand = API.ClearLeftHand()
     if leftHand:
       API.SysMsg("Cleared left hand: " + leftHand.Name)
     ```
    
    """
    pass

def ClearRightHand() -> int:
    """
     If you have an item in your right hand, move it to your backpack
     Sets API.Found to the item's serial.
     Example:
     ```py
     rightHand = API.ClearRightHand()
     if rightHand:
       API.SysMsg("Cleared right hand: " + rightHand.Name)
      ```
    
    """
    pass

def ClickObject(serial: int) -> None:
    """
     Single click an object
     Example:
     ```py
     API.ClickObject(API.Player)
     ```
    
    """
    pass

def UseObject(serial: int, skipQueue: bool = True) -> None:
    """
     Attempt to use(double click) an object.
     Example:
     ```py
     API.UseObject(API.Backpack)
     ```
    
    """
    pass

def Contents(serial: int) -> int:
    """
     Get an item count for the contents of a container
     Example:
     ```py
     count = API.Contents(API.Backpack)
     if count > 0:
       API.SysMsg(f"You have {count} items in your backpack")
     ```
    
    """
    pass

def ContextMenu(serial: int, entry: int) -> None:
    """
     Send a context menu(right click menu) response.
     This does not open the menu, you do not need to open the menu first. This handles both in one action.
     Example:
     ```py
     API.ContextMenu(API.Player, 1)
     ```
    
    """
    pass

def EquipItem(serial: int) -> None:
    """
     Attempt to equip an item. Layer is automatically detected.
     Example:
     ```py
     lefthand = API.ClearLeftHand()
     API.Pause(2)
     API.EquipItem(lefthand)
     ```
    
    """
    pass

def ClearMoveQueue() -> None:
    """
     Clear the move item que of all items.
    
    """
    pass

def QueMoveItem(serial: int, destination: int, amt: int = 0, x: int = 0xFFFF, y: int = 0xFFFF) -> None:
    """
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
    
    """
    pass

def MoveItem(serial: int, destination: int, amt: int = 0, x: int = 0xFFFF, y: int = 0xFFFF) -> None:
    """
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
    
    """
    pass

def QueMoveItemOffset(serial: int, amt: int = 0, x: int = 0, y: int = 0, z: int = 0, OSI: bool = False) -> None:
    """
     Move an item to the ground near you.
     Example:
     ```py
     items = API.ItemsInContainer(API.Backpack)
     for item in items:
       API.QueMoveItemOffset(item, 0, 1, 0, 0)
     ```
    
    """
    pass

def MoveItemOffset(serial: int, amt: int = 0, x: int = 0, y: int = 0, z: int = 0, OSI: bool = False) -> None:
    """
     Move an item to the ground near you.
     Example:
     ```py
     items = API.ItemsInContainer(API.Backpack)
     for item in items:
       API.MoveItemOffset(item, 0, 1, 0, 0)
       API.Pause(0.75)
     ```
    
    """
    pass

def UseSkill(skillName: str) -> None:
    """
     Use a skill.
     Example:
     ```py
     API.UseSkill("Hiding")
     API.Pause(11)
     ```
    
    """
    pass

def CastSpell(spellName: str) -> None:
    """
     Attempt to cast a spell by its name.
     Example:
     ```py
     API.CastSpell("Fireball")
     API.WaitForTarget()
     API.Target(API.Player)
     ```
    
    """
    pass

def BuffExists(buffName: str) -> bool:
    """
     Check if a buff is active.
     Example:
     ```py
     if API.BuffExists("Bless"):
       API.SysMsg("You are blessed!")
     ```
    
    """
    pass

def ActiveBuffs() -> list[Buff]:
    """
     Get a list of all buffs that are active.
     See [Buff](Buff.md) to see what attributes are available.
     Buff does not get updated after you access it in python, you will need to call this again to get the latest buff data.
     Example:
     ```py
     buffs = API.ActiveBuffs()
     for buff in buffs:
         API.SysMsg(buff.Title)
     ```
    
    """
    pass

def SysMsg(message: str, hue: int = 946) -> None:
    """
     Show a system message(Left side of screen).
     Example:
     ```py
     API.SysMsg("Script started!")
     ```
    
    """
    pass

def Msg(message: str) -> None:
    """
     Say a message outloud.
     Example:
     ```py
     API.Say("Hello friend!")
     ```
    
    """
    pass

def HeadMsg(message: str, serial: int, hue: int = 1337) -> None:
    """
     Show a message above a mobile or item, this is only visible to you.
     Example:
     ```py
     API.HeadMsg("Only I can see this!", API.Player)
     ```
    
    """
    pass

def PartyMsg(message: str) -> None:
    """
     Send a message to your party.
     Example:
     ```py
     API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")
     ```
    
    """
    pass

def GuildMsg(message: str) -> None:
    """
     Send your guild a message.
     Example:
     ```py
     API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
     ```
    
    """
    pass

def AllyMsg(message: str) -> None:
    """
     Send a message to your alliance.
     Example:
     ```py
     API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
     ```
    
    """
    pass

def WhisperMsg(message: str) -> None:
    """
     Whisper a message.
     Example:
     ```py
     API.WhisperMsg("Psst, bet you didn't see me here..")
     ```
    
    """
    pass

def YellMsg(message: str) -> None:
    """
     Yell a message.
     Example:
     ```py
     API.YellMsg("Vendor restocked, get your fresh feathers!")
     ```
    
    """
    pass

def EmoteMsg(message: str) -> None:
    """
     Emote a message.
     Example:
     ```py
     API.EmoteMsg("laughing")
     ```
    
    """
    pass

def FindItem(serial: int) -> Item:
    """
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
    
    """
    pass

def FindType(graphic: int, container: int = 1337, range: int = 1337, hue: int = 1337, minamount: int = 0) -> Item:
    """
     Attempt to find an item by type(graphic).
     Sets API.Found to the serial of the item found.
     Example:
     ```py
     item = API.FindType(0x0EED, API.Backpack)
     if item:
       API.SysMsg("Found the item!")
       API.UseObject(item)
     ```
    
    """
    pass

def FindTypeAll(graphic: int, container: int = 1337, range: int = 1337, hue: int = 1337, minamount: int = 0) -> list[Item]:
    """
     Return a list of items matching the parameters set.
     Example:
     ```py
     items = API.FindTypeAll(0x0EED, API.Backpack)
     if items:
       API.SysMsg("Found " + str(len(items)) + " items!")
     ```
    
    """
    pass

def FindLayer(layer: str, serial: int = 1337) -> Item:
    """
     Attempt to find an item on a layer.
     Sets API.Found to the serial of the item found.
     Example:
     ```py
     item = API.FindLayer("Helmet")
     if item:
       API.SysMsg("Wearing a helmet!")
     ```
    
    """
    pass

def ItemsInContainer(container: int, recursive: bool = False) -> list[Item]:
    """
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
    
    """
    pass

def UseType(graphic: int, hue: int = 1337, container: int = 1337, skipQueue: bool = True) -> None:
    """
     Attempt to use the first item found by graphic(type).
     Example:
     ```py
     API.UseType(0x3434, API.Backpack)
     API.WaitForTarget()
     API.Target(API.Player)
     ```
    
    """
    pass

def CreateCooldownBar(seconds: float, text: str, hue: int) -> None:
    """
     Create a cooldown bar.
     Example:
     ```py
     API.CreateCooldownBar(5, "Healing", 21)
     ```
    
    """
    pass

def IgnoreObject(serial: int) -> None:
    """
     Adds an item or mobile to your ignore list.
     These are unique lists per script. Ignoring an item in one script, will not affect other running scripts.
     Example:
     ```py
     for item in ItemsInContainer(API.Backpack):
       if item.Name == "Dagger":
       API.IgnoreObject(item)
     ```
    
    """
    pass

def ClearIgnoreList() -> None:
    """
     Clears the ignore list. Allowing functions to see those items again.
     Example:
     ```py
     API.ClearIgnoreList()
     ```
    
    """
    pass

def OnIgnoreList(serial: int) -> bool:
    """
     Check if a serial is on the ignore list.
     Example:
     ```py
     if API.OnIgnoreList(API.Backpack):
       API.SysMsg("Currently ignoring backpack")
     ```
    
    """
    pass

def Pathfind(x: int, y: int, z: int = 1337, distance: int = 1, wait: bool = False, timeout: int = 10) -> bool:
    """
     Attempt to pathfind to a location.  This will fail with large distances.
     Example:
     ```py
     API.Pathfind(1414, 1515)
     ```
    
    """
    pass

def PathfindEntity(entity: int, distance: int = 1, wait: bool = False, timeout: int = 10) -> bool:
    """
     Attempt to pathfind to a mobile or item.
     Example:
     ```py
     mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
     if mob:
       API.PathfindEntity(mob)
     ```
    
    """
    pass

def Pathfinding() -> bool:
    """
     Check if you are already pathfinding.
     Example:
     ```py
     if API.Pathfinding():
       API.SysMsg("Pathfinding...!")
       API.Pause(0.25)
     ```
    
    """
    pass

def CancelPathfinding() -> None:
    """
     Cancel pathfinding.
     Example:
     ```py
     if API.Pathfinding():
       API.CancelPathfinding()
     ```
    
    """
    pass

def GetPath(x: int, y: int, z: int = 1337, distance: int = 1) -> Any:
    """
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
    
    """
    pass

def AutoFollow(mobile: int) -> None:
    """
     Automatically follow a mobile. This is different than pathfinding. This will continune to follow the mobile.
     Example:
     ```py
     mob = API.NearestMobile([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
     if mob:
       API.AutoFollow(mob)
     ```
    
    """
    pass

def CancelAutoFollow() -> None:
    """
     Cancel auto follow mode.
     Example:
     ```py
     if API.Pathfinding():
       API.CancelAutoFollow()
     ```
    
    """
    pass

def Run(direction: str) -> None:
    """
     Run in a direction.
     Example:
     ```py
     API.Run("north")
     ```
    
    """
    pass

def Walk(direction: str) -> None:
    """
     Walk in a direction.
     Example:
     ```py
     API.Walk("north")
     ```
    
    """
    pass

def Turn(direction: str) -> None:
    """
     Turn your character a specific direction.
     Example:
     ```py
     API.Turn("north")
     ```
    
    """
    pass

def Rename(serial: int, name: str) -> None:
    """
     Attempt to rename something like a pet.
     Example:
     ```py
     API.Rename(0x12345678, "My Handsome Pet")
     ```
    
    """
    pass

def Dismount() -> None:
    """
     Attempt to dismount if mounted.
     Example:
     ```py
     API.Dismount()
     ```
    
    """
    pass

def Mount(serial: int) -> None:
    """
     Attempt to mount(double click)
     Example:
     ```py
     API.Mount(0x12345678)
     ```
    
    """
    pass

def WaitForTarget(targetType: str = "any", timeout: float = 5) -> bool:
    """
     Wait for a target cursor.
     Example:
     ```py
     API.WaitForTarget()
     ```
    
    """
    pass

def Target(serial: int) -> None:
    """
     Target an item or mobile.
     Example:
     ```py
     if API.WaitForTarget():
       API.Target(0x12345678)
     ```
    
    """
    pass

def Target(x: int, y: int, z: int, graphic: int = 1337) -> None:
    """
     Target a location. Include graphic if targeting a static.
     Example:
     ```py
     if API.WaitForTarget():
       API.Target(1243, 1337, 0)
      ```
    
    """
    pass

def RequestTarget(timeout: float = 5) -> int:
    """
     Request the player to target something.
     Example:
     ```py
     target = API.RequestTarget()
     if target:
       API.SysMsg("Targeted serial: " + str(target))
     ```
    
    """
    pass

def TargetSelf() -> None:
    """
     Target yourself.
     Example:
     ```py
     API.TargetSelf()
     ```
    
    """
    pass

def TargetLandRel(xOffset: int, yOffset: int) -> None:
    """
     Target a land tile relative to your position.
     If this doesn't work, try TargetTileRel instead.
     Example:
     ```py
     API.TargetLand(1, 1)
     ```
    
    """
    pass

def TargetTileRel(xOffset: int, yOffset: int, graphic: int = 1337) -> None:
    """
     Target a tile relative to your location.
     If this doesn't work, try TargetLandRel instead.'
     Example:
     ```py
     API.TargetTileRel(1, 1)
     ```
    
    """
    pass

def CancelTarget() -> None:
    """
     Cancel targeting.
     Example:
     ```py
     if API.WaitForTarget():
       API.CancelTarget()
       API.SysMsg("Targeting cancelled, april fools made you target something!")
     ```
    
    """
    pass

def HasTarget(targetType: str = "any") -> bool:
    """
     Check if the player has a target cursor.
     Example:
     ```py
     if API.HasTarget():
         API.CancelTarget()
     ```
    
    """
    pass

def GetMap() -> int:
    """
     Get the current map index.
     Standard maps are:
     0 = Fel
     1 = Tram
     2 = Ilshenar
     3 = Malas
     4 = Tokuno
     5 = TerMur
    
    """
    pass

def SetSkillLock(skill: str, up_down_locked: str) -> None:
    """
     Set a skills lock status.
     Example:
     ```py
     API.SetSkillLock("Hiding", "locked")
     ```
    
    """
    pass

def SetStatLock(stat: str, up_down_locked: str) -> None:
    """
     Set a skills lock status.
     Example:
     ```py
     API.SetStatLock("str", "locked")
     ```
    
    """
    pass

def Logout() -> None:
    """
     Logout of the game.
     Example:
     ```py
     API.Logout()
     ```
    
    """
    pass

def ItemNameAndProps(serial: int, wait: bool = False, timeout: int = 10) -> str:
    """
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
    
    """
    pass

def HasGump(ID: int = 1337) -> int:
    """
     Check if a player has a server gump. Leave blank to check if they have any server gump.
     Example:
     ```py
     if API.HasGump(0x12345678):
       API.SysMsg("Found a gump!")
    ```
    
    """
    pass

def ReplyGump(button: int, gump: int = 1337) -> bool:
    """
     Reply to a gump.
     Example:
     ```py
     API.ReplyGump(21)
     ```
    
    """
    pass

def CloseGump(ID: int = 1337) -> None:
    """
     Close the last gump open, or a specific gump.
     Example:
     ```py
     API.CloseGump()
     ```
    
    """
    pass

def GumpContains(text: str, ID: int = 1337) -> bool:
    """
     Check if a gump contains a specific text.
     Example:
     ```py
     if API.GumpContains("Hello"):
       API.SysMsg("Found the text!")
     ```
    
    """
    pass

def GetGump(ID: int = 1337) -> Gump:
    """
     Get a gump by ID.
     Example:
     ```py
     gump = API.GetGump()
     if gump:
       API.SysMsg("Found the gump!")
       API.CloseGump(gump)
     ```
    
    """
    pass

def ToggleFly() -> None:
    """
     Toggle flying if you are a gargoyle.
     Example:
     ```py
     API.ToggleFly()
     ```
    
    """
    pass

def ToggleAbility(ability: str) -> None:
    """
     Toggle an ability.
     Example:
     ```py
     if not API.PrimaryAbilityActive():
       API.ToggleAbility("primary")
     ```
    
    """
    pass

def PrimaryAbilityActive() -> bool:
    """
     Check if your primary ability is active.
     Example:
     ```py
     if API.PrimaryAbilityActive():
       API.SysMsg("Primary ability is active!")
     ```
    
    """
    pass

def SecondaryAbilityActive() -> bool:
    """
     Check if your secondary ability is active.
     Example:
     ```py
     if API.SecondaryAbilityActive():
       API.SysMsg("Secondary ability is active!")
     ```
    
    """
    pass

def InJournal(msg: str) -> bool:
    """
     Check if your journal contains a message.
     Example:
     ```py
     if API.InJournal("You have been slain"):
       API.SysMsg("You have been slain!")
     ```
    
    """
    pass

def InJournalAny(msgs: list[str]) -> bool:
    """
     Check if the journal contains *any* of the strings in this list.
     Can be regex, prepend your msgs with $
     Example:
     ```py
     if API.InJournalAny(["You have been slain", "You are dead"]):
       API.SysMsg("You have been slain or dead!")
     ```
    
    """
    pass

def ClearJournal() -> None:
    """
     Clear your journal(This is specific for each script).
     Example:
     ```py
     API.ClearJournal()
     ```
    
    """
    pass

def Pause(seconds: float) -> None:
    """
     Pause the script.
     Example:
     ```py
     API.Pause(5)
     ```
    
    """
    pass

def Stop() -> None:
    """
     Stops the current script.
     Example:
     ```py
     API.Stop()
     ```
    
    """
    pass

def ToggleAutoLoot() -> None:
    """
     Toggle autolooting on or off.
     Example:
     ```py
     API.ToggleAutoLoot()
     ```
    
    """
    pass

def AutoLootContainer(container: int) -> None:
    """
     Use autoloot on a specific container.
     Example:
     ```py
     targ = API.RequestTarget()
     if targ:
       API.AutoLootContainer(targ)
     ```
    
    """
    pass

def Virtue(virtue: str) -> None:
    """
     Use a virtue.
     Example:
     ```py
     API.Virtue("honor")
     ```
    
    """
    pass

def NearestEntity(scanType: ScanType, maxDistance: int = 10) -> Any:
    """
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
    
    """
    pass

def NearestMobile(notoriety: list[Notoriety], maxDistance: int = 10) -> Mobile:
    """
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
    
    """
    pass

def NearestCorpse(distance: int = 3) -> Item:
    """
     Get the nearest corpse within a distance.
     Sets API.Found to the serial of the corpse.
     Example:
     ```py
     corpse = API.NearestCorpse()
     if corpse:
       API.SysMsg("Found a corpse!")
       API.UseObject(corpse)
     ```
    
    """
    pass

def NearestMobiles(notoriety: list[Notoriety], maxDistance: int = 10) -> list[Mobile]:
    """
     Get all mobiles matching Notoriety and distance.
     Example:
     ```py
     mob = API.NearestMobiles([API.Notoriety.Murderer, API.Notoriety.Criminal], 7)
     if len(mob) > 0:
       API.SysMsg("Found enemies!")
       API.Msg("Guards!")
       API.Attack(mob[0])
       ```
    
    """
    pass

def FindMobile(serial: int) -> Mobile:
    """
     Get a mobile from its serial.
     Sets API.Found to the serial of the mobile.
     Example:
     ```py
     mob = API.FindMobile(0x12345678)
     if mob:
       API.SysMsg("Found the mobile!")
       API.UseObject(mob)
     ```
    
    """
    pass

def GetAllMobiles() -> list[Mobile]:
    """
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
    
    """
    pass

def GetTile(x: int, y: int) -> GameObject:
    """
     Get the tile at a location.
     Example:
     ```py
     tile = API.GetTile(1414, 1515)
     if tile:
       API.SysMsg(f"Found a tile with graphic: {tile.Graphic}")
     ```
    
    """
    pass

def CreateGump(acceptMouseInput: bool = True, canMove: bool = True, keepOpen: bool = False) -> Gump:
    """
     Get a blank gump.
     Example:
     ```py
     g = API.CreateGump()
     g.SetRect(100, 100, 200, 200)
     g.Add(API.CreateGumpLabel("Hello World!"))
     API.AddGump(g)
     ```
    
    """
    pass

def AddGump(g: Gump) -> None:
    """
     Add a gump to the players screen.
     Example:
     ```py
     g = API.CreateGump()
     g.SetRect(100, 100, 200, 200)
     g.Add(API.CreateGumpLabel("Hello World!"))
     API.AddGump(g)
     ```
    
    """
    pass

def CreateGumpCheckbox(text: str = "", hue: int = 0, isChecked: bool = False) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpLabel(text: str, hue: int = 996) -> PyControl:
    """
     Create a label for a gump.
     Example:
     ```py
     g = API.CreateGump()
     g.SetRect(100, 100, 200, 200)
     g.Add(API.CreateGumpLabel("Hello World!"))
     API.AddGump(g)
     ```
    
    """
    pass

def CreateGumpColorBox(opacity: float = 0.7, color: str = "#000000") -> PyControl:
    """
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
    
    """
    pass

def CreateGumpItemPic(graphic: int, width: int, height: int) -> PyControl:
    """
     Create a picture of an item.
     Example:
     ```py
     g = API.CreateGump()
     g.SetRect(100, 100, 200, 200)
     g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))
     API.AddGump(g)
     ```
    
    """
    pass

def CreateGumpButton(text: str = "", hue: int = 996, normal: int = 0x00EF, pressed: int = 0x00F0, hover: int = 0x00EE) -> PyControl:
    """
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
    
    """
    pass

def CreateSimpleButton(text: str, width: int, height: int) -> PyControl:
    """
     Create a simple button, does not use graphics.
     Example:
     ```py
     g = API.CreateGump()
     g.SetRect(100, 100, 200, 200)
     button = API.CreateSimpleButton("Click Me!", 100, 20)
     g.Add(button)
     API.AddGump(g)
     ```
    
    """
    pass

def CreateGumpRadioButton(text: str = "", group: int = 0, inactive: int = 0x00D0, active: int = 0x00D1, hue: int = 0xFFFF, isChecked: bool = False) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpTextBox(text: str = "", width: int = 200, height: int = 30, multiline: bool = False) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpTTFLabel(text: str, size: float, color: str = "#FFFFFF", font: str = TrueTypeLoader.EMBEDDED_FONT, aligned: str = "let", maxWidth: int = 0, applyStroke: bool = False) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpSimpleProgressBar(width: int, height: int, backgroundColor: str = "#616161", foregroundColor: str = "#212121", value: int = 100, max: int = 100) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpScrollArea(x: int, y: int, width: int, height: int) -> PyControl:
    """
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
    
    """
    pass

def CreateGumpPic(graphic: int, x: int = 0, y: int = 0, hue: int = 0) -> PyControl:
    """
     Create a gump pic(Use this for gump art, not item art)
     Example:
     ```py
     gumpPic = API.CreateGumpPic(0xafb)
     gump.Add(gumpPic)
    
    """
    pass

def AddControlOnClick(control: PyControl, onClick: Any, leftOnly: bool = True) -> PyControl:
    """
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
    
    """
    pass

def AddControlOnDisposed(control: PyControl, onDispose: Any) -> PyControl:
    """
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
    
    """
    pass

def GetSkill(skill: str) -> Skill:
    """
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
    
    """
    pass

def DisplayRange(distance: int, hue: int = 22) -> None:
    """
     Show a radius around the player.
     Example:
     ```py
     API.DisplayRange(7, 32)
     ```
    
    """
    pass

def ToggleScript(scriptName: str) -> None:
    """
     Toggle another script on or off.
     Example:
     ```py
     API.ToggleScript("MyScript.py")
     ```
    
    """
    pass

def PlayScript(scriptName: str) -> None:
    """
     Play a legion script.
    
    """
    pass

def StopScript(scriptName: str) -> None:
    """
     Stop a legion script.
    
    """
    pass

def AddMapMarker(name: str, x: int = int.MaxValue, y: int = int.MaxValue, map: int = int.MaxValue, color: str = "purple") -> None:
    """
     Add a marker to the current World Map (If one is open)
     Example:
     ```py
     API.AddMapMarker("Death")
     ```
    
    """
    pass

def RemoveMapMarker(name: str) -> None:
    """
     Remove a marker from the world map.
     Example:
     ```py
     API.RemoveMapMarker("Death")
     ```
    
    """
    pass

def IsProcessingMoveQue() -> bool:
    """
     Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.
     Example:
     ```py
     if API.IsProcessingMoveQue():
       API.Pause(0.5)
     ```
    
    """
    pass

def SavePersistentVar(name: str, value: str, scope: Any) -> None:
    """
     Save a variable that persists between sessions and scripts.
     Example:
     ```py
     API.SavePersistentVar("TotalKills", "5", API.PersistentVar.Char)
     ```
    
    """
    pass

def RemovePersistentVar(name: str, scope: Any) -> None:
    """
     Delete/remove a persistent variable.
     Example:
     ```py
     API.RemovePersistentVar("TotalKills", API.PersistentVar.Char)
     ```
    
    """
    pass

def GetPersistentVar(name: str, defaultValue: str, scope: Any) -> str:
    """
     Get a persistent variable.
     Example:
     ```py
     API.GetPersistentVar("TotalKills", "0", API.PersistentVar.Char)
     ```
    
    """
    pass

