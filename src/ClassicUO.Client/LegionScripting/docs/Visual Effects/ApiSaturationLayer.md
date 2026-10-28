---
title: ApiSaturationLayer
description: "Pulls the frame's colour toward or away from its own brightness - drained for sickness and near-death, pushed for fever and hallucination. One sample per pixel, so it is the cheapest technique that reads the frame at all. Build one with `API.VisualEffects.NewSaturationLayer`."
---

## Class Description
Pulls the frame's colour toward or away from its own brightness - drained for sickness and
near-death, pushed for fever and hallucination. One sample per pixel, so it is the cheapest
technique that reads the frame at all. Build one with `API.VisualEffects.NewSaturationLayer`.

## Properties
### `Saturation`

**Type:** `double`

Where the colour is pulled to, relative to the frame's own brightness: 1 leaves it untouched,
0 drains it to grey, above 1 pushes it past its real colour, and below 0 lands on the
complement. Capped at -1 and 3 when the layer is baked.


*No fields found.*

## Enums
*No enums found.*

## Methods
