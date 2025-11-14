class Buff:
   ""
    Graphic: int = None
    Text: str = None
    Timer: int = None
    Type = None
    Title: str = None

class PyAlphaBlendControl(PyBaseControl):
   ""
    Hue: int = None
    Alpha: float = None
    BaseColorR: int = None
    BaseColorG: int = None
    BaseColorB: int = None
    BaseColorA: int = None

    def SetBaseColor(self, r: "int", g: "int", b: "int", a: "int" = 255) -> None:
        """
         Sets the base color of the alpha blend control using RGBA values (0-255)
        
        """
        pass

class PyBaseControl:
   ""
    CanMove: bool = None
    IsVisible: bool = None
    IsDisposed: bool = None

    def Add(self, childControl: "Any") -> None:
        """
         Adds a child control to this control. Works with gumps too (gump.Add(control)).
         Used in python API
        
        """
        pass

    def GetX(self) -> "int":
        """
         Returns the control's X position.
         Used in python API
        
        """
        pass

    def GetY(self) -> "int":
        """
         Returns the control's Y position.
         Used in python API
        
        """
        pass

    def SetX(self, x: "int") -> "PyBaseControl":
        """
         Sets the control's X position.
         Used in python API
        
        """
        pass

    def SetY(self, y: "int") -> "PyBaseControl":
        """
         Sets the control's Y position.
         Used in python API
        
        """
        pass

    def SetPos(self, x: "int", y: "int") -> "PyBaseControl":
        """
         Sets the control's X and Y positions.
         Used in python API
        
        """
        pass

    def GetWidth(self) -> "int":
        pass

    def GetHeight(self) -> "int":
        pass

    def SetWidth(self, width: "int") -> "PyBaseControl":
        """
         Sets the control's width.
         Used in python API
        
        """
        pass

    def SetHeight(self, height: "int") -> "PyBaseControl":
        """
         Sets the control's height.
         Used in python API
        
        """
        pass

    def SetRect(self, x: "int", y: "int", width: "int", height: "int") -> "PyBaseControl":
        """
         Sets the control's position and size in one operation.
         Used in python API
        
        """
        pass

    def CenterXInViewPort(self) -> "PyBaseControl":
        """
         Centers a GUMP horizontally in the viewport. Only works on Gump instances.
         Used in python API
        
        """
        pass

    def CenterYInViewPort(self) -> "PyBaseControl":
        """
         Centers a GUMP vertically in the viewport. Only works on Gump instances.
         Used in python API
        
        """
        pass

    def GetAlpha(self) -> "float":
        """
         Returns the control's Alpha value.
         Used in python API
        
        """
        pass

    def SetAlpha(self, alpha: "float") -> "PyBaseControl":
        """
         Sets the control's Alpha value.
         Used in python API
        
        """
        pass

    def Clear(self) -> "PyBaseControl":
        """
         Clears all child controls from this control.
         Used in python API
        
        """
        pass

    def Dispose(self) -> None:
        """
         Close/Destroy the control
        
        """
        pass

class PyBaseGump(PyBaseControl, IPyGump):
   ""
    IsDisposed: bool = None
    PacketGumpText: str = None
    CanCloseWithRightClick: bool = None
    Gump: PyBaseGump = None

    def SetInScreen(self) -> None:
        """
         Ensures the gump is fully visible within the screen boundaries.
         Adjusts the gump's position if it extends beyond the screen edges.
         Used in python API
        
        """
        pass

    def CenterYInScreen(self) -> None:
        """
         Centers the gump vertically within the entire screen.
         This accounts for the full screen dimensions, including all UI elements.
         Used in python API
        
        """
        pass

    def CenterXInScreen(self) -> None:
        """
         Centers the gump horizontally within the entire screen.
         This accounts for the full screen dimensions, including all UI elements.
         Used in python API
        
        """
        pass

class PyButton(PyBaseControl):
   ""
    ButtonID: int = None
    IsClicked: bool = None
    ButtonAction: int = None
    ToPage: int = None
    ButtonGraphicNormal: int = None
    ButtonGraphicPressed: int = None
    ButtonGraphicOver: int = None
    Hue: int = None
    FontCenter: bool = None
    ContainsByBounds: bool = None

    def HasBeenClicked(self) -> "bool":
        pass

class PyCheckbox(PyBaseControl):
   ""
    IsChecked: bool = None
    Text: str = None

    def GetIsChecked(self) -> "bool":
        """
         Gets the checked state of the checkbox.
         Used in python API
        
        """
        pass

    def SetIsChecked(self, isChecked: "bool") -> None:
        """
         Sets the checked state of the checkbox.
         Used in python API
        
        """
        pass

    def GetText(self) -> "str":
        """
         Gets the text label displayed next to the checkbox.
         Used in python API
        
        """
        pass

class PyControlDropDown(PyBaseControl):
   ""

    def GetSelectedIndex(self) -> "int":
        """
         Get the selected index of the dropdown. The first entry is 0.
        
        """
        pass

class PyEntity(PyGameObject):
   ""
    Name: str = None
    __class__: str = None
    Serial: int = None

    def ToString(self) -> "str":
        """
         Returns a readable string representation of the entity.
         Used when printing or converting the object to a string in Python scripts.
        
        """
        pass

    def Destroy(self) -> None:
        """
         This will remove the item from the client, it will reappear if you leave the area and come back.
         This object will also no longer be available and may cause issues if you try to interact with it further.
        
        """
        pass

class PyEvents:
   ""

    def OnPlayerHitsChanged(self, callback: "Any") -> None:
        """
         Subscribe to player hits changed event. Callback receives the new hits value as an integer.
         Example:
         ```py
         def on_hits_changed(new_hits):
           API.SysMsg(f"Player hits changed to: {new_hits}")
         API.Events.OnPlayerHitsChanged(on_hits_changed)
         while not API.StopRequested:
           API.ProcessCallbacks()
           API.Pause(0.25)
         ```
        
        """
        pass

    def OnBuffAdded(self, callback: "Any") -> None:
        """
         Called when a buff is added to your char. Callback receives a Buff object.
        
        """
        pass

    def OnBuffRemoved(self, callback: "Any") -> None:
        """
         Called when a buff is removed from your char. Callback receives a Buff object.
        
        """
        pass

    def OnPlayerDeath(self, callback: "Any") -> None:
        """
         Called when the player dies. Callback receives your characters serial.
        
        """
        pass

    def OnOpenContainer(self, callback: "Any") -> None:
        """
         Called when a container is opened. Callback receives the container serial.
        
        """
        pass

    def OnPlayerMoved(self, callback: "Any") -> None:
        """
         Called when the player moves. Callback receives a PositionChangedArgs object with .NewLocation available in the object.
        
        """
        pass

    def OnItemCreated(self, callback: "Any") -> None:
        """
         Called when a new item is created. Callback receives the item serial.
        
        """
        pass

class PyGameObject:
   ""
    Impassible: bool = None
    X: int = None
    Y: int = None
    Z: int = None
    Graphic: int = None
    Hue: int = None
    Distance: int = None
    __class__: str = None

    def SetHue(self, hue: "int") -> None:
        """
         Set the hue of a game object.
        
        """
        pass

    def HasLineOfSightFrom(self, observer: "PyGameObject" = None) -> "bool":
        """
         Determines if there is line of sight from the specified observer to this object.
         If no observer is specified, it defaults to the player.
        
        """
        pass

    def ToString(self) -> "str":
        """
         Returns a readable string representation of the game object.
         Used when printing or converting the object to a string in Python scripts.
        
        """
        pass

    def __repr__(self) -> "str":
        """
         Returns a detailed string representation of the object.
         This string is used by Python’s built-in <c>repr()</c> function.
        
        """
        pass

class PyGumpPic(PyBaseControl):
   ""
    Graphic: int = None
    Hue: int = None
    IsPartialHue: bool = None
    ContainsByBounds: bool = None

class PyGumps:
   ""

    def CreateGump(self, acceptMouseInput: "bool" = True, canMove: "bool" = True, keepOpen: "bool" = False) -> "PyBaseGump":
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

    def CreateModernGump(self, x: "int", y: "int", width: "int", height: "int", resizable: "bool" = True, minWidth: "int" = 50, minHeight: "int" = 50, onResized: "Any" = None) -> "PyNineSliceGump":
        """
         Creates a modern nine-slice gump using ModernUIConstants for consistent styling.
         The gump uses the standard modern UI panel texture and border size internally.
        
        """
        pass

    def AddGump(self, g: "Any") -> None:
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

    def CreateGumpCheckbox(self, text: "str" = "", hue: "int" = 0, isChecked: "bool" = False) -> "PyCheckbox":
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

    def CreateGumpLabel(self, text: "str", hue: "int" = 996) -> "PyLabel":
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

    def CreateGumpColorBox(self, opacity: "float" = 0.7, color: "str" = "#000000") -> "PyAlphaBlendControl":
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

    def CreateGumpItemPic(self, graphic: "int", width: "int", height: "int") -> "PyResizableStaticPic":
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

    def CreateGumpButton(self, text: "str" = "", hue: "int" = 996, normal: "int" = 0x00EF, pressed: "int" = 0x00F0, hover: "int" = 0x00EE) -> "PyButton":
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

    def CreateSimpleButton(self, text: "str", width: "int", height: "int") -> "PyNiceButton":
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

    def CreateGumpRadioButton(self, text: "str" = "", group: "int" = 0, inactive: "int" = 0x00D0, active: "int" = 0x00D1, hue: "int" = 0xFFFF, isChecked: "bool" = False) -> "PyRadioButton":
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

    def CreateGumpTextBox(self, text: "str" = "", width: "int" = 200, height: "int" = 30, multiline: "bool" = False) -> "PyTTFTextInputField":
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

    def CreateGumpTTFLabel(self, text: "str", size: "float", color: "str" = "#FFFFFF", font: "str" = TrueTypeLoader.EMBEDDED_FONT, aligned: "str" = "let", maxWidth: "int" = 0, applyStroke: "bool" = False) -> "PyTextBox":
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

    def CreateGumpSimpleProgressBar(self, width: "int", height: "int", backgroundColor: "str" = "#616161", foregroundColor: "str" = "#212121", value: "int" = 100, max: "int" = 100) -> "PySimpleProgressBar":
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

    def CreateGumpScrollArea(self, x: "int", y: "int", width: "int", height: "int") -> "PyScrollArea":
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

    def CreateGumpPic(self, graphic: "int", x: "int" = 0, y: "int" = 0, hue: "int" = 0) -> "PyGumpPic":
        """
         Create a gump pic(Use this for gump art, not item art)
         Example:
         ```py
         gumpPic = API.CreateGumpPic(0xafb)
         gump.Add(gumpPic)
        
        """
        pass

    def CreateTiledGumpPic(self, graphic: "int", width: "int", height: "int", hue: "int" = 0) -> "Any":
        """
         Create a gump pic that tiles(repeats) (Use this for gump art, not item art)
         Example:
         ```py
         gumpPic = API.CreateTiledGumpPic(0xafb, 100, 100)
         gump.Add(gumpPic)
        
        """
        pass

    def CreateDropDown(self, width: "int", items: "list[str]", selectedIndex: "int" = 0) -> "PyControlDropDown":
        """
         Creates a dropdown control (combobox) with the specified width and items.
        
        """
        pass

    def AddControlOnClick(self, control: "Any", onClick: "Any", leftOnly: "bool" = True) -> "Any":
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

    def AddControlOnDisposed(self, control: "PyBaseControl", onDispose: "Any") -> "PyBaseControl":
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

class PyItem(PyEntity):
   ""
    Amount: int = None
    Opened: bool = None
    Container: int = None
    RootContainer: int = None
    RootEntity = None
    __class__: str = None
    IsCorpse: bool = None
    IsContainer: bool = None
    MatchingHighlightName: str = None
    MatchesHighlight: bool = None

    def GetContainerGump(self) -> "PyBaseControl":
        """
         If this item is a container ( item.IsContainer ) and is open, this will return the grid container or container gump for it.
        
        """
        pass

class PyJournalEntry:
   ""
    Hue: int = None
    Name: str = None
    Text: str = None
    TextType = None
    Time: datetime = None
    MessageType = None
    Disposed: bool = None

class PyLabel(PyBaseControl):
   ""
    Text: str = None
    Hue: int = None

class PyLand(PyGameObject):
   ""
    __class__: str = None

class PyMobile(PyEntity):
   ""
    X: int = None
    Y: int = None
    Z: int = None
    HitsDiff: int = None
    ManaDiff: int = None
    StamDiff: int = None
    IsDead: bool = None
    IsPoisoned: bool = None
    HitsMax: int = None
    Hits: int = None
    StaminaMax: int = None
    Stamina: int = None
    ManaMax: int = None
    Mana: int = None
    IsRenamable: bool = None
    IsHuman: bool = None
    IsYellowHits: bool = None
    InWarMode: bool = None
    Backpack: PyItem = None
    Mount: PyItem = None
    __class__: str = None

class PyMulti(PyGameObject):
   ""
    __class__: str = None

class PyNiceButton(PyBaseControl):
   ""
    ButtonParameter: int = None
    IsSelectable: bool = None
    IsSelected: bool = None
    DisplayBorder: bool = None
    AlwaysShowBackground: bool = None
    Text: str = None
    TextHue: int = None
    BackgroundHue: int = None

    def SetText(self, text: "str") -> None:
        pass

    def SetBackgroundHue(self, hue: "int") -> None:
        pass

    def SetBackgroundColor(self, r: "int | None", g: "int | None", b: "int | None", a: "int | None" = 255) -> None:
        """
         Sets the background color of the button. Pass null to clear.
        
        """
        pass

    def ClearBackgroundColor(self) -> None:
        """
         Clears the background color of the button.
        
        """
        pass

class PyNineSliceGump(PyBaseControl, IPyGump):
   ""
    NineSliceGump = None
    Gump: PyBaseGump = None

    def GetHue(self) -> "int":
        """
         Gets the current hue of the nine-slice gump
        
        """
        pass

    def SetHue(self, hue: "int") -> None:
        """
         Sets the hue of the nine-slice gump
        
        """
        pass

    def GetResizable(self) -> "bool":
        """
         Gets whether the gump is resizable
        
        """
        pass

    def SetResizable(self, resizable: "bool") -> None:
        """
         Sets whether the gump is resizable
        
        """
        pass

    def GetBorderSize(self) -> "int":
        """
         Gets the border size of the nine-slice
        
        """
        pass

    def SetBorderSize(self, borderSize: "int") -> None:
        """
         Sets the border size of the nine-slice
        
        """
        pass

class ModernNineSliceGump(NineSliceGump):
   ""

    def SetResizeCallback(self, callback: "Any") -> None:
        pass

class PyPlayer(PyMobile):
   ""
    X: int = None
    Y: int = None
    Z: int = None
    Strength: int = None
    Dexterity: int = None
    Intelligence: int = None
    StrengthIncrease: int = None
    DexterityIncrease: int = None
    IntelligenceIncrease: int = None
    StrLock = None
    DexLock = None
    IntLock = None
    HitPointsIncrease: int = None
    ManaIncrease: int = None
    StaminaIncrease: int = None
    HitPointsRegeneration: int = None
    ManaRegeneration: int = None
    StaminaRegeneration: int = None
    PhysicalResistance: int = None
    FireResistance: int = None
    ColdResistance: int = None
    PoisonResistance: int = None
    EnergyResistance: int = None
    MaxPhysicResistance: int = None
    MaxFireResistance: int = None
    MaxColdResistance: int = None
    MaxPoisonResistance: int = None
    MaxEnergyResistance: int = None
    DamageMin: int = None
    DamageMax: int = None
    DamageIncrease: int = None
    HitChanceIncrease: int = None
    SwingSpeedIncrease: int = None
    DefenseChanceIncrease: int = None
    MaxDefenseChanceIncrease: int = None
    ReflectPhysicalDamage: int = None
    SpellDamageIncrease: int = None
    FasterCasting: int = None
    FasterCastRecovery: int = None
    LowerManaCost: int = None
    LowerReagentCost: int = None
    IsCasting: bool = None
    IsRecovering: bool = None
    Luck: int = None
    Gold: int = None
    TithingPoints: int = None
    Weight: int = None
    WeightMax: int = None
    StatsCap: int = None
    Followers: int = None
    FollowersMax: int = None
    EnhancePotions: int = None
    MaxHitPointsIncrease: int = None
    MaxManaIncrease: int = None
    MaxStaminaIncrease: int = None
    IsHidden: bool = None
    IsWalking: bool = None
    InWarMode: bool = None
    __class__: str = None

class PyProfile:
   ""
    CharacterName: str = None
    ServerName: str = None
    LootBagSerial: int = None
    FavoriteBagSerial: int = None
    MoveItemDelay: int = None
    AutoLootEnabled: bool = None

class PyRadioButton(PyCheckbox):
   ""
    GroupIndex: int = None

    def GetGroupIndex(self) -> "int":
        """
         Gets the group index of the radio button.
         Radio buttons with the same group index are mutually exclusive.
         Used in python API
        
        """
        pass

    def SetGroupIndex(self, groupIndex: "int") -> None:
        """
         Sets the group index of the radio button.
         Radio buttons with the same group index are mutually exclusive.
         Used in python API
        
        """
        pass

class PyResizableStaticPic(PyBaseControl):
   ""
    Hue: int = None
    Graphic: int = None
    DrawBorder: bool = None

class PyScrollArea(PyBaseControl):
   ""

class PySimpleProgressBar(PyBaseControl):
   ""

    def SetProgress(self, value: "float", max: "float") -> None:
        """
         Sets the progress value for the progress bar.
        
        """
        pass

class PyStatic(PyGameObject):
   ""
    IsImpassible: bool = None
    IsTree: bool = None
    IsVegetation: bool = None
    IsCave: bool = None
    Name: str = None
    __class__: str = None

class PyTextBox(PyBaseControl):
   ""
    Text: str = None
    Hue: int = None
    Font: str = None
    FontSize: float = None
    MultiLine: bool = None

    def SetText(self, text: "str") -> None:
        pass

class PyTiledGumpPic(PyBaseControl):
   ""
    Graphic: int = None
    Hue: int = None

class PyTTFTextInputField(PyBaseControl):
   ""
    Text: str = None
    CaretIndex: int = None
    NumbersOnly: bool = None
    AcceptKeyboardInput: bool = None
    ConvertHtmlColors: bool = None

    def SetText(self, text: "str") -> None:
        pass

    def SetPlaceholder(self, text: "str") -> None:
        pass

    def SetFocus(self) -> None:
        pass

    def UpdateSize(self, width: "int", height: "int") -> None:
        pass

JournalEntries = None
Backpack: int = None
Player: PyPlayer = None
Bank: int = None
Random = None
LastTargetSerial: int = None
LastTargetPos = None
LastTargetGraphic: int = None
Found: int = None
Profile: PyProfile = None
Events: PyEvents = None
Gumps: PyGumps = None
StopRequested: bool = None
CancellationToken = None

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

def Dispose() -> None:
    pass

def OnHotKey(key: "str", callback: "Any" = None) -> None:
    """
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
    
    """
    pass

def SetSharedVar(name: "str", value: "Any") -> None:
    """
     Set a variable that is shared between scripts.
     Example:
     ```py
     API.SetSharedVar("myVar", 10)
     ```
    
    """
    pass

def GetSharedVar(name: "str") -> "Any":
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

def RemoveSharedVar(name: "str") -> None:
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

def Attack(serial: "int") -> None:
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

def SetWarMode(enabled: "bool") -> None:
    """
     Sets the player's war mode state (peace/war toggle).
    
    """
    pass

def BandageSelf() -> "bool":
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

def ClearLeftHand() -> "PyItem":
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

def ClearRightHand() -> "PyItem":
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

def ClickObject(serial: "int") -> None:
    """
     Single click an object
     Example:
     ```py
     API.ClickObject(API.Player)
     ```
    
    """
    pass

def UseObject(serial: "int", skipQueue: "bool" = True) -> None:
    """
     Attempt to use(double click) an object.
     Example:
     ```py
     API.UseObject(API.Backpack)
     ```
    
    """
    pass

def Contents(serial: "int") -> "int":
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

def ContextMenu(serial: "int", entry: "int") -> None:
    """
     Send a context menu(right click menu) response.
     This does not open the menu, you do not need to open the menu first. This handles both in one action.
     Example:
     ```py
     API.ContextMenu(API.Player, 1)
     ```
    
    """
    pass

def EquipItem(serial: "int") -> None:
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

def QueueMoveItem(serial: "int", destination: "int", amt: "int" = 0, x: "int" = 0xFFFF, y: "int" = 0xFFFF) -> None:
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
                 API.QueueMoveItem(item, barrel)
     ```
    
    """
    pass

def MoveItem(serial: "int", destination: "int", amt: "int" = 0, x: "int" = 0xFFFF, y: "int" = 0xFFFF) -> None:
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

def QueueMoveItemOffset(serial: "int", amt: "int" = 0, x: "int" = 0, y: "int" = 0, z: "int" = 0, OSI: "bool" = False) -> None:
    """
     Move an item to the ground near you.
     Example:
     ```py
     items = API.ItemsInContainer(API.Backpack)
     for item in items:
       API.QueueMoveItemOffset(item, 0, 1, 0, 0)
     ```
    
    """
    pass

def MoveItemOffset(serial: "int", amt: "int" = 0, x: "int" = 0, y: "int" = 0, z: "int" = 0, OSI: "bool" = False) -> None:
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

def UseSkill(skillName: "str") -> None:
    """
     Use a skill.
     Example:
     ```py
     API.UseSkill("Hiding")
     API.Pause(11)
     ```
    
    """
    pass

def CastSpell(spellName: "str") -> None:
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

def Dress(name: "str") -> None:
    """
     Dress from a saved dress configuration.
     Example:
     ```py
     API.Dress("PvP Gear")
     ```
    
    """
    pass

def UndressAll(kr: "bool" = False) -> None:
    """
     Undress all your equipment
    
    """
    pass

def GetAvailableDressOutfits() -> "list":
    """
     Get all available dress configurations.
     Example:
     ```py
     outfits = API.GetAvailableDressOutfits()
     if outfits:
       Dress(outfits[0])
     ```
    
    """
    pass

def Organizer(name: "str", source: "int" = 0, destination: "int" = 0) -> None:
    """
     Runs an organizer agent to move items between containers.
     Example:
     ```py
     # Run organizer with default containers
     API.Organizer("MyOrganizer")
    
     # Run organizer with specific source and destination
     API.Organizer("MyOrganizer", 0x40001234, 0x40005678)
     ```
    
    """
    pass

def ClientCommand(command: "str") -> None:
    """
     Executes a client command as if typed in the game console
    
    """
    pass

def BuffExists(buffName: "str") -> "bool":
    """
     Check if a buff is active.
     Example:
     ```py
     if API.BuffExists("Bless"):
       API.SysMsg("You are blessed!")
     ```
    
    """
    pass

def ActiveBuffs() -> "list[Buff]":
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

def SysMsg(message: "str", hue: "int" = 946) -> None:
    """
     Show a system message(Left side of screen).
     Example:
     ```py
     API.SysMsg("Script started!")
     ```
    
    """
    pass

def Msg(message: "str") -> None:
    """
     Say a message outloud.
     Example:
     ```py
     API.Say("Hello friend!")
     ```
    
    """
    pass

def HeadMsg(message: "str", serial: "int", hue: "int" = 1337) -> None:
    """
     Show a message above a mobile or item, this is only visible to you.
     Example:
     ```py
     API.HeadMsg("Only I can see this!", API.Player)
     ```
    
    """
    pass

def PartyMsg(message: "str") -> None:
    """
     Send a message to your party.
     Example:
     ```py
     API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")
     ```
    
    """
    pass

def GuildMsg(message: "str") -> None:
    """
     Send your guild a message.
     Example:
     ```py
     API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
     ```
    
    """
    pass

def AllyMsg(message: "str") -> None:
    """
     Send a message to your alliance.
     Example:
     ```py
     API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
     ```
    
    """
    pass

def WhisperMsg(message: "str") -> None:
    """
     Whisper a message.
     Example:
     ```py
     API.WhisperMsg("Psst, bet you didn't see me here..")
     ```
    
    """
    pass

def YellMsg(message: "str") -> None:
    """
     Yell a message.
     Example:
     ```py
     API.YellMsg("Vendor restocked, get your fresh feathers!")
     ```
    
    """
    pass

def EmoteMsg(message: "str") -> None:
    """
     Emote a message.
     Example:
     ```py
     API.EmoteMsg("laughing")
     ```
    
    """
    pass

def GlobalMsg(message: "str") -> None:
    """
     Send a chat message via the global chat msg system ( ,message here ).
    
    """
    pass

def PromptResponse(message: "str") -> None:
    """
     Send a response to a server prompt(Like renaming a rune for example).
    
    """
    pass

def FindItem(serial: "int") -> "PyItem":
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

def FindType(graphic: "int", container: "int" = 1337, range: "int" = 1337, hue: "int" = 1337, minamount: "int" = 0) -> "PyItem":
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

def FindTypeAll(graphic: "int", container: "int" = 1337, range: "int" = 1337, hue: "int" = 1337, minamount: "int" = 0) -> "list[PyItem]":
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

def FindLayer(layer: "str", serial: "int" = 1337) -> "PyItem":
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

def GetItemsOnGround(distance: "int" = int.MaxValue, graphic: "int" = 1337) -> "list":
    """
     Get all items on the ground within specified range.
     Example:
     ```py
     items = API.GetItemsOnGround(10)  # All items within 10 tiles
     if items:
       API.SysMsg("Found " + str(len(items)) + " items on ground!")
     ```
    
    """
    pass

def ItemsInContainer(container: "int", recursive: "bool" = False) -> "list[PyItem]":
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

def UseType(graphic: "int", hue: "int" = 1337, container: "int" = 1337, skipQueue: "bool" = True) -> None:
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

def CreateCooldownBar(seconds: "float", text: "str", hue: "int") -> None:
    """
     Create a cooldown bar.
     Example:
     ```py
     API.CreateCooldownBar(5, "Healing", 21)
     ```
    
    """
    pass

def IgnoreObject(serial: "int") -> None:
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

def OnIgnoreList(serial: "int") -> "bool":
    """
     Check if a serial is on the ignore list.
     Example:
     ```py
     if API.OnIgnoreList(API.Backpack):
       API.SysMsg("Currently ignoring backpack")
     ```
    
    """
    pass

def Pathfind(x: "int", y: "int", z: "int" = 1337, distance: "int" = 1, wait: "bool" = False, timeout: "int" = 10) -> "bool":
    """
     Attempt to pathfind to a location.  This will fail with large distances.
     Example:
     ```py
     API.Pathfind(1414, 1515)
     ```
    
    """
    pass

def PathfindEntity(entity: "int", distance: "int" = 1, wait: "bool" = False, timeout: "int" = 10) -> "bool":
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

def Pathfinding() -> "bool":
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

def GetPath(x: "int", y: "int", z: "int" = 1337, distance: "int" = 1) -> "list":
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

def AutoFollow(mobile: "int") -> None:
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

def Run(direction: "str") -> None:
    """
     Run in a direction.
     Example:
     ```py
     API.Run("north")
     ```
    
    """
    pass

def Walk(direction: "str") -> None:
    """
     Walk in a direction.
     Example:
     ```py
     API.Walk("north")
     ```
    
    """
    pass

def Turn(direction: "str") -> None:
    """
     Turn your character a specific direction.
     Example:
     ```py
     API.Turn("north")
     ```
    
    """
    pass

def Rename(serial: "int", name: "str") -> None:
    """
     Attempt to rename something like a pet.
     Example:
     ```py
     API.Rename(0x12345678, "My Handsome Pet")
     ```
    
    """
    pass

def Dismount(skipQueue: "bool" = True) -> None:
    """
     Attempt to dismount if mounted.
     Example:
     ```py
     API.Dismount()
     ```
    
    """
    pass

def Mount(serial: "int" = 1337, skipQueue: "bool" = True) -> None:
    """
     Attempt to mount(double click)
     Example:
     ```py
     API.Mount(0x12345678)
     ```
    
    """
    pass

def SetMount(serial: "int") -> None:
    """
     This will set your saved mount for this character.
    
    """
    pass

def WaitForTarget(targetType: "str" = "any", timeout: "float" = 5) -> "bool":
    """
     Wait for a target cursor.
     Example:
     ```py
     API.WaitForTarget()
     ```
    
    """
    pass

def Target(serial: "int") -> None:
    """
     Target an item or mobile.
     Example:
     ```py
     if API.WaitForTarget():
       API.Target(0x12345678)
     ```
    
    """
    pass

def Target(x: "int", y: "int", z: "int", graphic: "int" = 1337) -> None:
    """
     Target a location. Include graphic if targeting a static.
     Example:
     ```py
     if API.WaitForTarget():
       API.Target(1243, 1337, 0)
      ```
    
    """
    pass

def RequestTarget(timeout: "float" = 5) -> "int":
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

def RequestAnyTarget(timeout: "float" = 5) -> "PyGameObject":
    """
     Prompts the player to target any object in the game world, including an <c>Item</c> , <c>Mobile</c> , <c>Land</c> tile, <c>Static</c> , or <c>Multi</c> .
     Waits for the player to select a target within a given timeout period.
    
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

def TargetLandRel(xOffset: "int", yOffset: "int") -> None:
    """
     Target a land tile relative to your position.
     If this doesn't work, try TargetTileRel instead.
     Example:
     ```py
     API.TargetLand(1, 1)
     ```
    
    """
    pass

def TargetTileRel(xOffset: "int", yOffset: "int", graphic: "int" = 1337) -> None:
    """
     Target a tile relative to your location.
     If this doesn't work, try TargetLandRel instead.'
     Example:
     ```py
     API.TargetTileRel(1, 1)
     ```
    
    """
    pass

def TargetResource(itemSerial: "int", resource: "int") -> None:
    """
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

def PreTarget(serial: "int", targetType: "str" = "neutral") -> None:
    """
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
    
    """
    pass

def CancelPreTarget() -> None:
    """
     Cancels any active pre-target.
     Example:
     ```py
     API.PreTarget(enemy.Serial, "harmful")
     # Changed my mind, cancel the pre-target
     API.CancelPreTarget()
     ```
    
    """
    pass

def HasTarget(targetType: "str" = "any") -> "bool":
    """
     Check if the player has a target cursor.
     Example:
     ```py
     if API.HasTarget():
         API.CancelTarget()
     ```
    
    """
    pass

def GetMap() -> "int":
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

def SetSkillLock(skill: "str", up_down_locked: "str") -> None:
    """
     Set a skills lock status.
     Example:
     ```py
     API.SetSkillLock("Hiding", "locked")
     ```
    
    """
    pass

def SetStatLock(stat: "str", up_down_locked: "str") -> None:
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

def ItemNameAndProps(serial: "int", wait: "bool" = False, timeout: "int" = 10) -> "str":
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

def RequestOPLData(serials: "list[int]") -> None:
    """
     Requests Object Property List (OPL) data for the specified serials.
     If the OPL data doesn't already exist, it will be requested from the server.
     OPL consists of item name and tooltip text(properties).
    
    """
    pass

def HasGump(ID: "int" = 1337) -> "int":
    """
     Check if a player has a server gump. Leave blank to check if they have any server gump.
     Example:
     ```py
     if API.HasGump(0x12345678):
       API.SysMsg("Found a gump!")
    ```
    
    """
    pass

def ReplyGump(button: "int", gump: "int" = 1337) -> "bool":
    """
     Reply to a gump.
     Example:
     ```py
     API.ReplyGump(21)
     ```
    
    """
    pass

def CloseGump(ID: "int" = 1337) -> None:
    """
     Close the last gump open, or a specific gump.
     Example:
     ```py
     API.CloseGump()
     ```
    
    """
    pass

def ConfigNextGump(serial: "int | None" = None, x: "int | None" = None, y: "int | None" = None, isVisible: "bool | None" = None, autoClose: "bool | None" = None, autoRespond: "bool | None" = None, autoRespondButton: "int | None" = None) -> None:
    """
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
    
    """
    pass

def GumpContains(text: "str", ID: "int" = 1337) -> "bool":
    """
     Check if a gump contains a specific text.
     Example:
     ```py
     if API.GumpContains("Hello"):
       API.SysMsg("Found the text!")
     ```
    
    """
    pass

def GetGumpContents(ID: "int" = 1337) -> "str":
    """
     This will return a string of all the text in a server-side gump.
    
    """
    pass

def GetGump(ID: "int" = 1337) -> "PyBaseGump":
    """
     Get a gump by ID.
     Example:
     ```py
     gump = API.GetGump()
     if gump:
       API.SysMsg("Found the gump!")
       gump.Dispose() #Close it
     ```
    
    """
    pass

def GetAllGumps() -> "list":
    """
     Gets all currently open server-side gumps.
    
    """
    pass

def WaitForGump(ID: "int" = 1337, delay: "float" = 5) -> "bool":
    """
     Wait for a server-side gump.
     Example:
     ```py
     if API.WaitForGump(1951773915):
       API.HeadMsg("SUCCESS", API.Player, 62)
     else:
      API.HeadMsg("FAILURE", API.Player, 32)
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

def ToggleAbility(ability: "str") -> None:
    """
     Toggle an ability.
     Example:
     ```py
     if not API.PrimaryAbilityActive():
       API.ToggleAbility("primary")
     ```
    
    """
    pass

def PrimaryAbilityActive() -> "bool":
    """
     Check if your primary ability is active.
     Example:
     ```py
     if API.PrimaryAbilityActive():
       API.SysMsg("Primary ability is active!")
     ```
    
    """
    pass

def SecondaryAbilityActive() -> "bool":
    """
     Check if your secondary ability is active.
     Example:
     ```py
     if API.SecondaryAbilityActive():
       API.SysMsg("Secondary ability is active!")
     ```
    
    """
    pass

def InJournal(msg: "str", clearMatches: "bool" = False) -> "bool":
    """
     Check if your journal contains a message.
     Example:
     ```py
     if API.InJournal("You have been slain"):
       API.SysMsg("You have been slain!")
     ```
    
    """
    pass

def InJournalAny(msgs: "list[str]", clearMatches: "bool" = False) -> "bool":
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

def GetJournalEntries(seconds: "float", matchingText: "str" = "") -> "list":
    """
     Get all the journal entires in the last X seconds.
     matchingText supports regex with $ prepended.
     Example:
     ```py
     list = API.GetJournalEntries(30)
     if list:
       for entry in list:
         entry.Text # Do something with this
     ```
    
    """
    pass

def ClearJournal(matchingEntries: "str" = "") -> None:
    """
     Clear your journal(This is specific for each script).
     Supports regex matching if prefixed with $
     Example:
     ```py
     API.ClearJournal()
     ```
    
    """
    pass

def Pause(seconds: "float") -> None:
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

def AutoLootContainer(container: "int") -> None:
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

def Virtue(virtue: "str") -> None:
    """
     Use a virtue.
     Example:
     ```py
     API.Virtue("honor")
     ```
    
    """
    pass

def NearestEntity(scanType: "ScanType", maxDistance: "int" = 10) -> "Any":
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

def NearestMobile(notoriety: "list[Notoriety]", maxDistance: "int" = 10) -> "PyMobile":
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

def NearestCorpse(distance: "int" = 3) -> "PyItem":
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

def NearestMobiles(notoriety: "list[Notoriety]", maxDistance: "int" = 10) -> "list[PyMobile]":
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

def FindMobile(serial: "int") -> "PyMobile":
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

def GetAllMobiles(graphic: "int | None" = None, distance: "int | None" = None, notoriety: "list[Notoriety]" = None) -> "list[PyMobile]":
    """
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
    
    """
    pass

def GetTile(x: "int", y: "int") -> "PyGameObject":
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

def GetStaticsAt(x: "int", y: "int") -> "list[Any]":
    """
     Gets all static objects at a specific position (x, y coordinates).
     This includes trees, vegetation, buildings, and other non-movable scenery.
     Example:
     ```py
     statics = API.GetStaticsAt(1000, 1000)
     for s in statics:
         API.SysMsg(f"Static Graphic: {s.Graphic}, Z: {s.Z}")
     ```
    
    """
    pass

def GetStaticsInArea(x1: "int", y1: "int", x2: "int", y2: "int") -> "list[Any]":
    """
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
    
    """
    pass

def GetMultisAt(x: "int", y: "int") -> "list[Any]":
    """
     Gets all multi objects at a specific position (x, y coordinates).
     This includes server-side house data.
     Example:
     ```py
     multis = API.GetMultisAt(1000, 1000)
     for m in multis:
         API.SysMsg(f"Multi Graphic: {m.Graphic}, Z: {m.Z}")
     ```
    
    """
    pass

def GetMultisInArea(x1: "int", y1: "int", x2: "int", y2: "int") -> "list[Any]":
    """
     Gets all multi objects within a rectangular area defined by coordinates.
     This includes server-side house data.
     Example:
     ```py
     multis = API.GetMultisInArea(1000, 1000, 1010, 1010)
     API.SysMsg(f"Found {len(multis)} multis in area")
     for m in multis:
         API.SysMsg(f"Multi Graphic: {m.Graphic} at {m.X}, {m.Y}")
     ```
    
    """
    pass

def IsFriend(serial: "int") -> "bool":
    """
     Check if a mobile is in the friends list.
     Example:
     ```py
     if API.IsFriend(player.Serial):
         API.SysMsg("This player is your friend!")
     ```
    
    """
    pass

def AddFriend(serial: "int") -> "bool":
    """
     Add a mobile to the friends list by serial number.
     Example:
     ```py
     mobile = API.FindMobile(0x12345)
     if mobile:
         API.AddFriend(mobile.Serial)
     ```
    
    """
    pass

def RemoveFriend(serial: "int") -> "bool":
    """
     Remove a mobile from the friends list by serial number.
     Example:
     ```py
     API.RemoveFriend(0x12345)
     ```
    
    """
    pass

def GetAllFriends() -> "list":
    """
     Get all friends as an array of serials.
     Example:
     ```py
     friends = API.GetAllFriends()
     for friend in friends:
         API.FindMobile(friend)
     ```
    
    """
    pass

def CreateGump(acceptMouseInput: "bool" = True, canMove: "bool" = True, keepOpen: "bool" = False) -> "PyBaseGump":
    """
     Use API.Gumps.CreateGump instead
    
    """
    pass

def AddGump(g: "Any") -> None:
    """
     Use API.Gumps.AddGump instead
    
    """
    pass

def CreateGumpCheckbox(text: "str" = "", hue: "int" = 0, isChecked: "bool" = False) -> "PyCheckbox":
    """
     Use API.Gumps.CreateGumpCheckbox instead.
    
    """
    pass

def CreateGumpLabel(text: "str", hue: "int" = 996) -> "PyLabel":
    """
     Use API.Gumps.CreateGumpLabel instead.
    
    """
    pass

def CreateGumpColorBox(opacity: "float" = 0.7, color: "str" = "#000000") -> "PyAlphaBlendControl":
    """
     Use API.Gumps.CreateGumpColorBox instead.
    
    """
    pass

def CreateGumpItemPic(graphic: "int", width: "int", height: "int") -> "PyResizableStaticPic":
    """
     Use API.Gumps.CreateGumpItemPic instead.
    
    """
    pass

def CreateGumpButton(text: "str" = "", hue: "int" = 996, normal: "int" = 0x00EF, pressed: "int" = 0x00F0, hover: "int" = 0x00EE) -> "PyButton":
    """
     Use API.Gumps.CreateGumpButton instead.
    
    """
    pass

def CreateSimpleButton(text: "str", width: "int", height: "int") -> "PyNiceButton":
    """
     Use API.Gumps.CreateSimpleButton instead.
    
    """
    pass

def CreateGumpRadioButton(text: "str" = "", group: "int" = 0, inactive: "int" = 0x00D0, active: "int" = 0x00D1, hue: "int" = 0xFFFF, isChecked: "bool" = False) -> "PyRadioButton":
    """
     Use API.Gumps.CreateGumpRadioButton instead.
    
    """
    pass

def CreateGumpTextBox(text: "str" = "", width: "int" = 200, height: "int" = 30, multiline: "bool" = False) -> "PyTTFTextInputField":
    """
     Use API.Gumps.CreateGumpTextBox instead.
    
    """
    pass

def CreateGumpTTFLabel(text: "str", size: "float", color: "str" = "#FFFFFF", font: "str" = TrueTypeLoader.EMBEDDED_FONT, aligned: "str" = "let", maxWidth: "int" = 0, applyStroke: "bool" = False) -> "PyTextBox":
    """
     Use API.Gumps.CreateGumpTTFLabel instead.
    
    """
    pass

def CreateGumpSimpleProgressBar(width: "int", height: "int", backgroundColor: "str" = "#616161", foregroundColor: "str" = "#212121", value: "int" = 100, max: "int" = 100) -> "PySimpleProgressBar":
    """
     Use API.Gumps.CreateGumpSimpleProgressBar instead.
    
    """
    pass

def CreateGumpScrollArea(x: "int", y: "int", width: "int", height: "int") -> "PyScrollArea":
    """
     Use API.Gumps.CreateGumpScrollArea instead.
    
    """
    pass

def CreateGumpPic(graphic: "int", x: "int" = 0, y: "int" = 0, hue: "int" = 0) -> "PyGumpPic":
    """
     Use API.Gumps.CreateGumpPic instead.
    
    """
    pass

def CreateDropDown(width: "int", items: "list[str]", selectedIndex: "int" = 0) -> "PyControlDropDown":
    """
     Use API.Gumps.CreateDropDown instead.
    
    """
    pass

def CreateModernGump(x: "int", y: "int", width: "int", height: "int", resizable: "bool" = True, minWidth: "int" = 50, minHeight: "int" = 50, onResized: "Any" = None) -> "PyNineSliceGump":
    """
     Use API.Gumps.CreateModernGump instead.
    
    """
    pass

def AddControlOnClick(control: "Any", onClick: "Any", leftOnly: "bool" = True) -> "Any":
    """
     Use API.Gumps.AddControlOnClick instead.
    
    """
    pass

def AddControlOnDisposed(control: "PyBaseControl", onDispose: "Any") -> "PyBaseControl":
    """
     Use API.Gumps.AddControlOnDisposed instead.
    
    """
    pass

def GetSkill(skill: "str") -> "Skill":
    """
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
    
    """
    pass

def DisplayRange(distance: "int", hue: "int" = 22) -> None:
    """
     Show a radius around the player.
     Example:
     ```py
     API.DisplayRange(7, 32)
     ```
    
    """
    pass

def ToggleScript(scriptName: "str") -> None:
    """
     Toggle another script on or off.
     Example:
     ```py
     API.ToggleScript("MyScript.py")
     ```
    
    """
    pass

def PlayScript(scriptName: "str") -> None:
    """
     Play a legion script.
    
    """
    pass

def StopScript(scriptName: "str") -> None:
    """
     Stop a legion script.
    
    """
    pass

def AddMapMarker(name: "str", x: "int" = int.MaxValue, y: "int" = int.MaxValue, map: "int" = int.MaxValue, color: "str" = "purple") -> None:
    """
     Add a marker to the current World Map (If one is open)
     Example:
     ```py
     API.AddMapMarker("Death")
     ```
    
    """
    pass

def RemoveMapMarker(name: "str") -> None:
    """
     Remove a marker from the world map.
     Example:
     ```py
     API.RemoveMapMarker("Death")
     ```
    
    """
    pass

def IsProcessingMoveQueue() -> "bool":
    """
     Check if the move item queue is being processed. You can use this to prevent actions if the queue is being processed.
     Example:
     ```py
     if API.IsProcessingMoveQueue():
       API.Pause(0.5)
     ```
    
    """
    pass

def IsProcessingUseItemQueue() -> "bool":
    """
     Check if the use item queue is being processed. You can use this to prevent actions if the queue is being processed.
     Example:
     ```py
     if API.IsProcessingUseItemQueue():
       API.Pause(0.5)
     ```
    
    """
    pass

def IsGlobalCooldownActive() -> "bool":
    """
     Check if the global cooldown is currently active. This applies to actions like moving or using items,
     and prevents new actions from executing until the cooldown has expired.
    
     Example:
     ```py
     if API.IsGlobalCooldownActive():
         API.Pause(0.5)
     ```
    
    """
    pass

def SavePersistentVar(name: "str", value: "str", scope: "Any") -> None:
    """
     Save a variable that persists between sessions and scripts.
     Example:
     ```py
     API.SavePersistentVar("TotalKills", "5", API.PersistentVar.Char)
     ```
    
    """
    pass

def RemovePersistentVar(name: "str", scope: "Any") -> None:
    """
     Delete/remove a persistent variable.
     Example:
     ```py
     API.RemovePersistentVar("TotalKills", API.PersistentVar.Char)
     ```
    
    """
    pass

def GetPersistentVar(name: "str", defaultValue: "str", scope: "Any") -> "str":
    """
     Get a persistent variable.
     Example:
     ```py
     API.GetPersistentVar("TotalKills", "0", API.PersistentVar.Char)
     ```
    
    """
    pass

def MarkTile(x: "int", y: "int", hue: "int", map: "int" = -1) -> None:
    """
     Mark a tile with a specific hue.
    
    """
    pass

def RemoveMarkedTile(x: "int", y: "int", map: "int" = -1) -> None:
    """
     Remove a marked tile. See MarkTile for more info.
    
    """
    pass

def TrackingArrow(x: "int", y: "int", identifier: "int" = 1337) -> None:
    """
     Create a tracking arrow pointing towards a location.
     Set x or y to a negative value to close existing tracker arrow.
     ```py
     API.TrackingArrow(400, 400)
     ```
    
    """
    pass

