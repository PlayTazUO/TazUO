---
title: ApiFadeSpec
description: "How a visual effect arrives and leaves."
---

## Class Description
How a visual effect arrives and leaves.

Onset reads as something happening, so it wants to be noticed; leaving is only the absence of
that, and a fast fade-out draws attention to the effect ending rather than to it being over.
Hence the client's own defaults are quick in, unhurried out.

:::caution[Photosensitive Epilepsy Warning]
Visual effects may contain flashing lights or rapid color changes.

If you have an epileptic condition or have had seizures of any kind, please consult your physician before
enabling the system or using the VFX APIs.

Note that while the in-game UI has some safeguards meant to reduce PSE risks, certain API usage patterns can
bypass those.

Near-zero fades make an effect appear and vanish at once, so starting and stopping one in a loop
strobes the whole screen.
Keep both above a tenth of a second unless the effect is started once and left to run.
:::

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
