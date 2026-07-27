---
title: ApiUiBaseControl
description: ApiUiBaseControl class documentation
---

## Properties
### `CanMove`

**Type:** `bool`

 Weather this control/gump can be moved by dragging this control


### `IsVisible`

**Type:** `bool`

### `IsDisposed`

**Type:** `bool`

 Check if this control has been disposed(delete/removed/etc)



*No fields found.*

## Enums
*No enums found.*

## Methods
### Add
`(childControl)`
 Adds a child control to this control. Works with gumps too (gump.Add(control)).
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `childControl` | `object` | ❌ No | The control to add as a child |

**Return Type:** `void` *(Does not return anything)*

---

### GetX

 Returns the control's X position.
 Used in python API


**Return Type:** `int`

---

### GetY

 Returns the control's Y position.
 Used in python API


**Return Type:** `int`

---

### SetX
`(x)`
 Sets the control's X position.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | The new X coordinate |

**Return Type:** `ApiUiBaseControl`

---

### SetY
`(y)`
 Sets the control's Y position.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `y` | `int` | ❌ No | The new Y coordinate |

**Return Type:** `ApiUiBaseControl`

---

### SetPos
`(x, y)`
 Sets the control's X and Y positions.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | The new X coordinate |
| `y` | `int` | ❌ No | The new Y coordinate |

**Return Type:** `ApiUiBaseControl`

---

### GetWidth

**Return Type:** `int`

---

### GetHeight

**Return Type:** `int`

---

### SetWidth
`(width)`
 Sets the control's width.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The new width in pixels |

**Return Type:** `ApiUiBaseControl`

---

### SetHeight
`(height)`
 Sets the control's height.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `height` | `int` | ❌ No | The new height in pixels |

**Return Type:** `ApiUiBaseControl`

---

### SetRect
`(x, y, width, height)`
 Sets the control's position and size in one operation.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | The new X coordinate |
| `y` | `int` | ❌ No | The new Y coordinate |
| `width` | `int` | ❌ No | The new width in pixels |
| `height` | `int` | ❌ No | The new height in pixels |

**Return Type:** `ApiUiBaseControl`

---

### CenterXInViewPort

 Centers a GUMP horizontally in the viewport. Only works on Gump instances.
 Used in python API


**Return Type:** `ApiUiBaseControl`

---

### CenterYInViewPort

 Centers a GUMP vertically in the viewport. Only works on Gump instances.
 Used in python API


**Return Type:** `ApiUiBaseControl`

---

### GetAlpha

 Returns the control's Alpha value.
 Used in python API


**Return Type:** `float`

---

### SetAlpha
`(alpha)`
 Sets the control's Alpha value.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `alpha` | `float` | ❌ No | The new Alpha value |

**Return Type:** `ApiUiBaseControl`

---

### SetTooltip
`(text)`
 Sets a plain text tooltip that is shown when hovering this control.
 Automatically enables mouse input so the tooltip can be triggered by hovering.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No | The tooltip text to display. Pass an empty string to clear the tooltip. |

**Return Type:** `ApiUiBaseControl`

---

### SetEntityTooltip
`(serial)`
 Sets the tooltip of this control to display the properties of an item/entity, as if hovering that item.
 Automatically enables mouse input so the tooltip can be triggered by hovering.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `serial` | `uint` | ❌ No | The serial of the item/entity whose tooltip should be shown. |

**Return Type:** `ApiUiBaseControl`

---

### SetAcceptMouseInput
`(enabled)`
 Sets whether this control accepts mouse input. Mouse input must be enabled for
 hover-based features such as tooltips to work.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `enabled` | `bool` | ❌ No | True to accept mouse input, false to ignore it. |

**Return Type:** `ApiUiBaseControl`

---

### ClearTooltip

 Clears the tooltip from this control.
 Used in python API


**Return Type:** `ApiUiBaseControl`

---

### Clear

 Clears all child controls from this control.
 Used in python API


**Return Type:** `ApiUiBaseControl`

---

### Dispose

 Close/Destroy the control


**Return Type:** `void` *(Does not return anything)*

---

