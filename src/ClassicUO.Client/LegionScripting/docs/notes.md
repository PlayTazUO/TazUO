---
title: Additional notes
description: Additional notes and information regarding the Python scripting API.
---

These are additional notes and information regarding the Python scripting API. This is for information not automatically generated in the [API docs](../api/)

## Misc
- Adding `_` to the beggining of python script file names will make them not show in-game. (For example: `_test.py`)

## Gumps/Controls
TazUO includes several helper methods to simplify interaction with gumps and UI controls via Python scripts:  

- `control.Add(control)` - Add a control to another control(Works with gumps too. gump.Add(control))
- `control.GetX()` - Returns the control's X position  
- `control.GetY()`  – Returns the control's Y position  
- `control.SetX(5)` – Sets the control's X position  
- `control.SetY(5)` – Sets the control's Y position  
- `control.SetPos(5, 5)` - Sets the controls x,y positions
- `control.SetWidth(50)` – Sets the control's width  
- `control.SetHeight(50)` – Sets the control's height  
- `control.SetRect(0, 0, 50, 50)` - Sets the controls x, y, width, height in that order
- `gump.CenterXInViewPort()` - Center a GUMP X in the viewport
- `gump.CenterYInViewPort()` - Center a GUMP Y in the viewport

### Buttons
- `button.HasBeenClicked()` -> Will be true if the player clicked the button. When this method is checked, it sets the state to false to avoid registering double clicks.
- `button.IsClicked` -> Is the button currently pressed down?


## Mobiles
These can be used like this:  
```py
mobile = API.Player #Or other things like NearestMobile

if mobile:
  #mobile.ACCESSOR
  mobile.IsParalyzed
  mobile 
```  

- `IsAttackable` -> Not invulnerable check. No other checks are made.
- `IsParalyzed`
- `IsYellowHits` -> Usually indicates they are invulnerable
- `IsPoisoned`
- `IsHuman` -> This is an educated guess, servers do not officially tell the client what race they are. This uses body graphic to determine.

Diff accessors are calculated `Max - Current`, so if yours hits are `80/90`, your hits diff is `10`  
- `Stamina`, `StaminaMax`, `StamDiff`
- `Mana`, `ManaMax`, `ManaDiff`
- `Hits`, `HitsMax`, `HitsDiff`
