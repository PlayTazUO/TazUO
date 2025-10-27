---
title: PyGameObject
description:  Base class for all Python-accessible game world objects.   Encapsulates common spatial and visual properties such as position and graphics.  
---

## Class Description
 Base class for all Python-accessible game world objects.
 Encapsulates common spatial and visual properties such as position and graphics.


## Properties
### `Impassible`

**Type:** `bool`

 Check if the object is impassible or not based on item data.


### `X`

**Type:** `ushort`

 The X-coordinate of the object in the game world.


### `Y`

**Type:** `ushort`

 The Y-coordinate of the object in the game world.


### `Z`

**Type:** `sbyte`

 The Z-coordinate (elevation) of the object in the game world.


### `Graphic`

**Type:** `ushort`

 The graphic ID of the object, representing its visual appearance.


### `Hue`

**Type:** `ushort`

 The hue (color tint) applied to the object.


### `Distance`

**Type:** `int`

### `__class__`

**Type:** `string`

 The Python-visible class name of this object.
 Accessible in Python as <c>obj.__class__</c> .




## Enums
*No enums found.*

## Methods
### SetHue
`(hue)`
 Set the hue of a game object.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `hue` | `ushort` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### HasLineOfSightFrom
`(observer)`
 Determines if there is line of sight from the specified observer to this object.
 If no observer is specified, it defaults to the player.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `observer` | `PyGameObject` | ✅ Yes | The observing GameObject (optional). |

**Return Type:** `bool`

---

### ToString

 Returns a readable string representation of the game object.
 Used when printing or converting the object to a string in Python scripts.


**Return Type:** `string`

---

### __repr__

 Returns a detailed string representation of the object.
 This string is used by Python’s built-in <c>repr()</c> function.


**Return Type:** `string`

---

