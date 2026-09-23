---
title: ApiEntity
description:  Represents a Python-accessible entity in the game world, such as a mobile or item.  Inherits basic spatial and visual data from <see cref="ApiGameObject"/> . 
---

## Class Description
 Represents a Python-accessible entity in the game world, such as a mobile or item.
 Inherits basic spatial and visual data from <see cref="ApiGameObject"/> .


## Properties
### `Name`

**Type:** `string`

### `__class__`

**Type:** `string`

 The Python-visible class name of this object.
 Accessible in Python as `obj.__class__` .



### `Serial`

**Type:** `uint`

 The unique serial identifier of the entity.



## Enums
*No enums found.*

## Methods
### ToString

 Returns a readable string representation of the entity.
 Used when printing or converting the object to a string in Python scripts.


**Return Type:** `string`

---

### Destroy

 This will remove the item from the client, it will reappear if you leave the area and come back.
 This object will also no longer be available and may cause issues if you try to interact with it further.


**Return Type:** `void` *(Does not return anything)*

---

### Target

 Attempts to target this entity. Only has any effect while the client is waiting for a target selection.


**Return Type:** `void` *(Does not return anything)*

---

### TargetRel
`(xOffset, yOffset, tilesOnly)`
 Attempts to target the spot at an offset from this entity's position, resolving it the same way a
 click would: the topmost visible object there is targeted, whether that is an entity, a static/multi,
 or land. Only has any effect while the client is waiting for a target selection.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `xOffset` | `int` | ❌ No | X offset from this entity's position, in tiles. |
| `yOffset` | `int` | ❌ No | Y offset from this entity's position, in tiles. |
| `tilesOnly` | `bool` | ✅ Yes | When true (default), entities are ignored and only statics/multi or land are targeted. |

**Return Type:** `void` *(Does not return anything)*

---

