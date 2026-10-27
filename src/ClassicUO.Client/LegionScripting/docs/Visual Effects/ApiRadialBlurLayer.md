---
title: ApiRadialBlurLayer
description: "Zoom blur along the ray from the shape's centre - head-spin, speed. Reads as vertigo rather than poor focus because the centre of the screen stays sharp however strong it is. Build one with `API.VisualEffects.NewRadialBlurLayer`."
---

## Class Description
Zoom blur along the ray from the shape's centre - head-spin, speed. Reads as vertigo rather than
poor focus because the centre of the screen stays sharp however strong it is. Build one with
`API.VisualEffects.NewRadialBlurLayer`.

## Properties
### `Zoom`

**Type:** `double`

How far along the centre ray the taps march, as a fraction of the distance from
`ApiShapeSpec.Center`.

### `Taps`

**Type:** `ApiVisualEffects.OverlaySampleTaps`

Samples taken per pixel. Fewer than a disk blur needs: radial taps land on a line
rather than spreading over an area, so the gaps between them are far less visible.


*No fields found.*

## Enums
*No enums found.*

## Methods
