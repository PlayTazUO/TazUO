---
title: ApiUiNineSliceGump
description: "ApiUiNineSliceGump class documentation"
---

## Properties
### `Gump`

**Type:** `Gump`



## Enums
*No enums found.*

## Methods
### GetHue

Gets the current hue of the nine-slice gump

**Return Type:** `ushort` - The current hue value

---

### SetHue
`(hue)`
Sets the hue of the nine-slice gump

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `hue` | `ushort` | ❌ No | The hue value to set |

**Return Type:** `void` *(Does not return anything)*

---

### GetResizable

Gets whether the gump is resizable

**Return Type:** `bool` - True if resizable, false otherwise

---

### SetResizable
`(resizable)`
Sets whether the gump is resizable

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `resizable` | `bool` | ❌ No | True to make resizable, false otherwise |

**Return Type:** `void` *(Does not return anything)*

---

### GetMinWidth

Gets the minimum width the gump can be resized to.

**Return Type:** `int` - The minimum width in pixels

---

### SetMinWidth
`(minWidth)`
Sets the minimum width the gump can be resized to. The gump is not resized immediately; the
new floor applies to subsequent resize drags.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `minWidth` | `int` | ❌ No | The minimum width in pixels |

**Return Type:** `void` *(Does not return anything)*

---

### GetMinHeight

Gets the minimum height the gump can be resized to.

**Return Type:** `int` - The minimum height in pixels

---

### SetMinHeight
`(minHeight)`
Sets the minimum height the gump can be resized to. The gump is not resized immediately; the
new floor applies to subsequent resize drags.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `minHeight` | `int` | ❌ No | The minimum height in pixels |

**Return Type:** `void` *(Does not return anything)*

---

### GetBorderSize

Gets the border size of the nine-slice

**Return Type:** `int` - The border size in pixels

---

### SetBorderSize
`(borderSize)`
Sets the border size of the nine-slice

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `borderSize` | `int` | ❌ No | The border size in pixels |

**Return Type:** `void` *(Does not return anything)*

---

### SetLegionTexture
`(texture, borderSize)`
Set the modern gump texture and border size

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `texture` | `string` | ❌ No |  |
| `borderSize` | `int` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

