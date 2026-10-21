---
title: ApiEffectProfile
description: "A visual effect composition: which layers, in what order, blended how, plus how it arrives and leaves and what it shakes. Get an editable copy of one the client already has with `API.GetVisualEffectProfile`, or start from nothing with `API.NewVisualEffectProfile`. Either way it is yours: nothing a script passes is ever saved, and editing this after `API.StartVisualEffect` cannot reach the running effect."
---

## Class Description
A visual effect composition: which layers, in what order, blended how, plus how it arrives and
 leaves and what it shakes.



 Get an editable copy of one the client already has with `API.GetVisualEffectProfile`, or start
 from nothing with `API.NewVisualEffectProfile`. Either way it is yours: nothing a script passes
 is ever saved, and editing this after `API.StartVisualEffect` cannot reach the running effect.

## Properties
### `Name`

**Type:** `string`

Only ever used in log messages for a script-built composition.

### `FullScreen`

**Type:** `bool`

Whether this covers the whole window rather than the game world alone. Governs both halves of
 scope: which pass draws the layers, and which rectangle `Shake` displaces.



 Leave it off unless the effect really is meant to cover the gumps and the cursor - doing so
 makes the UI hard to use for as long as the effect lasts.

### `Layers`

**Type:** `IList<ApiProfileLayer>`

Back to front: index 0 is drawn first and ends up underneath. At most 4.

### `Fade`

**Type:** `ApiFadeSpec?`

How the effect arrives and leaves. Starts at the client's own timing.

### `Shake`

**Type:** `ApiShakeSpec?`

Screen shake the effect includes, fired once on arrival, or null for a purely
 visual one. Build one with `API.NewShakeSpec`.


*No fields found.*

## Enums
*No enums found.*

## Methods
### AddLayer
`(effect, blend)`
Appends a layer, which is the usual way to build a composition up.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `effect` | `ApiLayerEffect` | ❌ No | The layer to draw. |
| `blend` | `LegionAPI.OverlayBlend` | ✅ Yes | How it composites against what is already beneath it. |

**Return Type:** `ApiProfileLayer`

---

