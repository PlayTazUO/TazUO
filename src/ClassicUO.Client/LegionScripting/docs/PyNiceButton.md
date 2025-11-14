---
title: PyNiceButton
description: PyNiceButton class documentation
---

## Properties
### `ButtonParameter`

**Type:** `int`

### `IsSelectable`

**Type:** `bool`

### `IsSelected`

**Type:** `bool`

### `DisplayBorder`

**Type:** `bool`

### `AlwaysShowBackground`

**Type:** `bool`

### `Text`

**Type:** `string`

### `TextHue`

**Type:** `ushort`

### `BackgroundHue`

**Type:** `ushort`


*No fields found.*

## Enums
*No enums found.*

## Methods
### SetText
`(text)`
**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### SetBackgroundHue
`(hue)`
**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `hue` | `ushort` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### SetBackgroundColor
`(r, g, b, a)`
 Sets the background color of the button. Pass null to clear.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `r` | `int?` | ❌ No |  |
| `g` | `int?` | ❌ No |  |
| `b` | `int?` | ❌ No |  |
| `a` | `int?` | ✅ Yes |  |

**Return Type:** `void` *(Does not return anything)*

---

### ClearBackgroundColor

 Clears the background color of the button.


**Return Type:** `void` *(Does not return anything)*

---

