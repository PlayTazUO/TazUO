---
title: ApiFadeSpec
description: "How a visual effect arrives and leaves. Onset reads as something happening, so it wants to be noticed; leaving is only the absence of that, and a fast fade-out draws attention to the effect ending rather than to it being over. Hence the client's own defaults are quick in, unhurried out. **Photosensitivity.** Near-zero fades make an effect appear and vanish at once, so starting and stopping one in a loop strobes the whole screen. Keep both above a tenth of a second unless the effect is raised once."
---

## Class Description
How a visual effect arrives and leaves.



 Onset reads as something happening, so it wants to be noticed; leaving is only the absence of
 that, and a fast fade-out draws attention to the effect ending rather than to it being over.
 Hence the client's own defaults are quick in, unhurried out.





 **Photosensitivity.** Near-zero fades make an effect appear and vanish at once, so starting and
 stopping one in a loop strobes the whole screen. Keep both above a tenth of a second unless the
 effect is raised once.

## Properties
### `InSeconds`

**Type:** `double`

Seconds to reach full strength from nothing. Clamped to 0-86400; the client floors
 the effective fade at 0.01s.

### `OutSeconds`

**Type:** `double`

Seconds to fade away once nothing is asking for the effect any more. Same range as
 `InSeconds`.



## Enums
*No enums found.*

## Methods
