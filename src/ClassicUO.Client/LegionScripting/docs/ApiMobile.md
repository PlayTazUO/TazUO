---
title: ApiMobile
description:  Represents a Python-accessible mobile (NPC, creature, or player character).   Inherits entity and positional data from <see cref="ApiEntity"/> .  
---

## Class Description
 Represents a Python-accessible mobile (NPC, creature, or player character).
 Inherits entity and positional data from <see cref="ApiEntity"/> .


## Properties
### `X`

**Type:** `ushort`

### `Y`

**Type:** `ushort`

### `Z`

**Type:** `sbyte`

### `HitsDiff`

**Type:** `int`

### `ManaDiff`

**Type:** `int`

### `StamDiff`

**Type:** `int`

### `IsDead`

**Type:** `bool`

### `IsPoisoned`

**Type:** `bool`

### `HitsMax`

**Type:** `int`

### `Hits`

**Type:** `int`

### `StaminaMax`

**Type:** `int`

### `Stamina`

**Type:** `int`

### `ManaMax`

**Type:** `int`

### `Mana`

**Type:** `int`

### `IsRenamable`

**Type:** `bool`

### `IsHuman`

**Type:** `bool`

### `IsYellowHits`

**Type:** `bool`

### `IsHidden`

**Type:** `bool`

### `IsGargoyle`

**Type:** `bool`

### `IsMounted`

**Type:** `bool`

### `IsDrivingBoat`

**Type:** `bool`

### `IsRunning`

**Type:** `bool`

### `Notoriety`

**Type:** `Notoriety`

### `InWarMode`

**Type:** `bool`

### `Backpack`

**Type:** `ApiItem`

 Get the mobile's Backpack item


### `Mount`

**Type:** `ApiItem`

 Get the mobile's Mount item (if mounted)


### `__class__`

**Type:** `string`

 The Python-visible class name of this object.
 Accessible in Python as <c>obj.__class__</c> .




## Enums
*No enums found.*

## Methods
### NameAndProps
`(wait, timeout)`
 Gets the mobile name and properties (tooltip text).
 This returns the name and properties in a single string. You can split it by newline if you want to separate them.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `wait` | `bool` | ✅ Yes | True or false to wait for name and props |
| `timeout` | `int` | ✅ Yes | Timeout in seconds |

**Return Type:** `string`

---

