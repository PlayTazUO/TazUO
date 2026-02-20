---
title: EventSinkApiDeclaration
description: EventSinkApiDeclaration class documentation
---

## Properties
*No properties found.*

*No fields found.*

## Enums
*No enums found.*

## Methods
### OnItemCreated
`(callback)`
 Invoked when an item is added to the client. The event's 'sender' is the Item


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### PyOnItemCreated
`(callback)`
 Invoked when an item is added to the client.
 The event's 'sender' is the Item, the event's argument is the item's serial


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnItemUpdated
`(callback)`
 Invoked when an item is already in the client but has been updated. The event's 'sender' is the Item


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnCorpseCreated
`(callback)`
 Invoked when a corpse is added to the client. The event's 'sender' is the corpse Item


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnConnected
`(callback)`
 Invoked when the player is connected to a server


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnDisconnected
`(callback)`
 Invoked when the player is disconnected from the server


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### MessageReceived
`(callback)`
 Invoked when any message is received from the server after client processing


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### RawMessageReceived
`(callback)`
 Invoked when any message is received from the server *before* client processing


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### ClilocMessageReceived
`(callback)`
  Not currently used. May be removed later or put into use, not sure right now


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### JournalEntryAdded
`(callback)`
  Invoked when a message is added to the journal


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### SoundPlayed
`(callback)`
 Invoked when the server requests that a sound be played


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OPLOnReceive
`(callback)`
 Invoked when an object's property list data (Tooltip text for items) is received


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnBuffAdded
`(callback)`
 Invoked when a buff is "added" to a player


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### PyOnBuffAdded
`(callback)`
 Invoked when a buff is "added" to a player


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnBuffRemoved
`(callback)`
 Invoked when a buff is "removed" to a player (Called before removal)


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### PyOnBuffRemoved
`(callback)`
 Invoked when a buff is "removed" to a player (Called before removal)


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPositionChanged
`(callback)`
 Invoked when the player's position is changed


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnEntityDamage
`(callback)`
 Invoked when any entity in the game receives damage, not necessarily the player.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnOpenContainer
`(callback)`
 Invoked when a container is opened.
 The event's 'sender' is the Item, the event's argument is the item's serial


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPlayerDeath
`(callback)`
 Invoked when the player receives a death packet from the server


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPathFinding
`(callback)`
  Invoked when the player or server tells the client to path find
  Vector is X, Y, Z, and Distance


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnSetWeather
`(callback)`
 Invoked when the server asks the client to generate some weather


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### OnPlayerHitsChanged
`(callback)`
 Invoked after the player's hit points have changed.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

### ApiMobileCreated
`(callback)`
 Invoked when a mobile is created.
 The event's sender is null and the argument is an ApiMobile.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `callback` | `object` | ❌ No |  |

**Return Type:** `void` *(Does not return anything)*

---

