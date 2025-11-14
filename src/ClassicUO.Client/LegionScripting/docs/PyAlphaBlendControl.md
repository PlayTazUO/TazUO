---
title: PyAlphaBlendControl
description: PyAlphaBlendControl class documentation
---

## Properties
### `Hue`

**Type:** `ushort`

### `Alpha`

**Type:** `float`

### `BaseColorR`

**Type:** `byte`

### `BaseColorG`

**Type:** `byte`

### `BaseColorB`

**Type:** `byte`

### `BaseColorA`

**Type:** `byte`


*No fields found.*

## Enums
*No enums found.*

## Methods
### SetBaseColor
`(r, g, b, a)`
 Sets the base color of the alpha blend control using RGBA values (0-255)


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `r` | `byte` | ❌ No | Red component (0-255) |
| `g` | `byte` | ❌ No | Green component (0-255) |
| `b` | `byte` | ❌ No | Blue component (0-255) |
| `a` | `byte` | ✅ Yes | Alpha component (0-255), defaults to 255 if not specified |

**Return Type:** `void` *(Does not return anything)*

---

