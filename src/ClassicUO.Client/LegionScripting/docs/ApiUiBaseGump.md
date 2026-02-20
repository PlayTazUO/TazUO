---
title: ApiUiBaseGump
description:  Python API wrapper for Gump (game window) objects in TazUO.  Provides safe, thread-marshaled access to gump properties and methods from Python scripts.  Inherits all control manipulation methods from ApiUiBaseControl.  Used in python API 
---

## Class Description
 Python API wrapper for Gump (game window) objects in TazUO.
 Provides safe, thread-marshaled access to gump properties and methods from Python scripts.
 Inherits all control manipulation methods from ApiUiBaseControl.
 Used in python API


## Properties
### `IsDisposed`

**Type:** `bool`

 Gets whether the gump has been disposed and is no longer valid.
 Returns true if the gump is disposed or no longer exists.
 Used in python API


### `PacketGumpText`

**Type:** `string`

 Gets the original packet text that was used to create this gump.
 This contains the gump layout and content data sent from the server.
 Used in python API


### `CanCloseWithRightClick`

**Type:** `bool`

 Gets or Sets the ability to close the gump with a right click


### `LayerOrder`

**Type:** `UILayer`

### `Gump`

**Type:** `Gump`

 Gets the underlying Gump instance that this wrapper represents.
 Used internally by the scripting system to access the actual game object.



*No fields found.*

## Enums
*No enums found.*

## Methods
### SetInScreen

 Ensures the gump is fully visible within the screen boundaries.
 Adjusts the gump's position if it extends beyond the screen edges.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

### CenterYInScreen

 Centers the gump vertically within the entire screen.
 This accounts for the full screen dimensions, including all UI elements.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

### CenterXInScreen

 Centers the gump horizontally within the entire screen.
 This accounts for the full screen dimensions, including all UI elements.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

