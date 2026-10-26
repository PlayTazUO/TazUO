---
title: ApiEffectProfileInfo
description: "One entry in the client's visual effect library, as `API.VisualEffects.GetProfiles` lists it. Enough to pick a composition and no more; `API.VisualEffects.GetProfile` fetches the editable copy."
---

## Class Description
One entry in the client's visual effect library, as `API.VisualEffects.GetProfiles` lists it.
Enough to pick a composition and no more; `API.VisualEffects.GetProfile` fetches the editable copy.

## Properties
### `Id`

**Type:** `string`

Stable identity, and what `API.VisualEffects.Start` takes.

### `Name`

**Type:** `string`

Display name. Shipped compositions are localized and suffixed "(built-in)", so this is only
a reliable key into `API.VisualEffects.StartByName` for the user's own.

### `IsBuiltIn`

**Type:** `bool`

Whether this is one of the compositions shipped with the client.


*No fields found.*

## Enums
*No enums found.*

## Methods
### ToString

Both halves, since neither alone identifies a composition to a reader: names repeat
and ids are unreadable.

**Return Type:** `string` - `Name [Id]`.

---

