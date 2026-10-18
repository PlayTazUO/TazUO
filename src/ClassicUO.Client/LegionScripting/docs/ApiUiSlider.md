---
title: ApiUiSlider
description:  Inherits from ApiUiBaseControl  Wraps a <see cref="HSliderBar"/> so scripts can read and change its value and react to changes. 
---

## Class Description
 Inherits from ApiUiBaseControl
 Wraps a <see cref="HSliderBar"/> so scripts can read and change its value and react to changes.


## Properties
### `Value`

**Type:** `int`

 Gets or sets the slider's current value. Values outside MinValue/MaxValue are clamped.


### `MinValue`

**Type:** `int`

 Gets or sets the slider's minimum value.


### `MaxValue`

**Type:** `int`

 Gets or sets the slider's maximum value.



*No fields found.*

## Enums
*No enums found.*

## Methods
### GetValue

 Gets the slider's current value.
 Used in python API


**Return Type:** `int`

---

### SetValue
`(value)`
 Sets the slider's current value. Values outside MinValue/MaxValue are clamped.
 Used in python API


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `value` | `int` | ❌ No | The new value |

**Return Type:** `ApiUiSlider`

---

### OnValueChanged
`(onValueChanged)`
 Add an onValueChanged callback to this slider, invoked whenever the value changes.
 The callback function receives the new value as its only argument.
 Example:
 ```py
 def on_change(value):
   API.SysMsg(f"Slider is now {value}")

 slider = API.Gumps.CreateGumpSlider(200, 0, 100, 50)
 slider.OnValueChanged(on_change)

 while True:
   API.ProcessCallbacks()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `onValueChanged` | `object` | ❌ No | The callback function that receives the new value |

**Return Type:** `ApiUiSlider`

---

