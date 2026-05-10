---
title: ApiUiControlDropDown
description: ApiUiControlDropDown class documentation
---

## Properties
*No properties found.*

*No fields found.*

## Enums
*No enums found.*

## Methods
### GetSelectedIndex

 Get the selected index of the dropdown. The first entry is 0.


**Return Type:** `int`

---

### OnDropDownOptionSelected
`(onSelectionChanged)`
 Add an onSelectionChanged callback to this dropdown control.
 The callback function will receive the selected index as a parameter.
 Example:
 ```py
 def on_select(index):
   API.SysMsg(f"Selected index: {index}")

 dropdown = API.Gumps.CreateDropDown(100, ["first", "second", "third"], 0)
 dropdown.OnDropDownOptionSelected(on_select)

 while True:
   API.ProcessCallbacks()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `onSelectionChanged` | `object` | ❌ No | The callback function that receives the selected index |

**Return Type:** `ApiUiControlDropDown`

---

