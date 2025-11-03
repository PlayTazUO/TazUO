---
title: PyEvents
description: PyEvents class documentation
---

## Properties
*No properties found.*


## Enums
*No enums found.*

## Methods
### OnPlayerHitsChanged
`(callback)`
 Subscribe to player hits changed event. Callback receives the new hits value as an integer.
 Example:
 ```py
 def on_hits_changed(new_hits):
   API.SysMsg(f"Player hits changed to: {new_hits}")
 API.Events.OnPlayerHitsChanged(on_hits_changed)
 while not API.StopRequested:
   API.ProcessCallbacks()
   API.Pause(0.25)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No | Python function to call when player hits change |

**Return Type:** `void` *(Does not return anything)*

---

### OnBuffAdded
`(callback)`
 Called when a buff is added to your char. Callback receives a Buff object.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnBuffRemoved
`(callback)`
 Called when a buff is removed from your char. Callback receives a Buff object.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPlayerDeath
`(callback)`
 Called when the player dies. Callback receives your characters serial.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnOpenContainer
`(callback)`
 Called when a container is opened. Callback receives the container serial.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPlayerMoved
`(callback)`
 Called when the player moves. Callback receives a PositionChangedArgs object with .NewLocation available in the object.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnItemCreated
`(callback)`
 Called when a new item is created. Callback receives the item serial.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

