---
title: ApiChromaticLayer
description: "Red/blue split along the ray from the shape's centre - lens fringing. Three taps whatever its strength; only `ApiSaturationLayer`, at one, is cheaper. Build one with `API.VisualEffects.NewChromaticLayer`."
---

## Class Description
Red/blue split along the ray from the shape's centre - lens fringing. Three taps whatever its
strength; only `ApiSaturationLayer`, at one, is cheaper. Build one with
`API.VisualEffects.NewChromaticLayer`.

## Properties
### `Aberration`

**Type:** `double`

Channel separation, as a fraction of the distance from `ApiShapeSpec.Center`. Nothing
separates at the centre and the fringing grows toward the corners, which is what makes it
read as a lens rather than as a broken image. Capped when the layer is baked.


*No fields found.*

## Enums
*No enums found.*

## Methods
