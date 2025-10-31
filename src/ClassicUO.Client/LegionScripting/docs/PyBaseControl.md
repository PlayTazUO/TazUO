---
title: PyBaseControl
description: PyBaseControl class documentation
---

## Properties
### `IsVisible`

**Type:** `bool`


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

**Return Type:** `void` *(Does not return anything)*

---

### SetY
`(y)`
 Sets the control's Y position.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `y` | `int` | ❌ No | The new Y coordinate |

**Return Type:** `void` *(Does not return anything)*

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

**Return Type:** `void` *(Does not return anything)*

---

### SetWidth
`(width)`
 Sets the control's width.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The new width in pixels |

**Return Type:** `void` *(Does not return anything)*

---

### SetHeight
`(height)`
 Sets the control's height.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `height` | `int` | ❌ No | The new height in pixels |

**Return Type:** `void` *(Does not return anything)*

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

**Return Type:** `void` *(Does not return anything)*

---

### CenterXInViewPort

 Centers a GUMP horizontally in the viewport. Only works on Gump instances.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

### CenterYInViewPort

 Centers a GUMP vertically in the viewport. Only works on Gump instances.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

### Clear

 Clears all child controls from this control.
 Used in python API


**Return Type:** `void` *(Does not return anything)*

---

### Dispose

 Close/Destroy the control


**Return Type:** `void` *(Does not return anything)*

---

