---
title: ApiVector2
description: "A per-axis pair of values, for wherever an API knob differs between X and Y. Build one with `API.VisualEffects.NewVector2`, or assign to the `X` and `Y` of the one a layer already carries."
---

## Class Description
A per-axis pair of values, for wherever an API knob differs between X and Y. Build one with
`API.VisualEffects.NewVector2`, or assign to the `X` and `Y` of the one a layer already carries.

## Properties
### `X`

**Type:** `double`

Horizontal component. What it means is the knob's, not this class's - a frequency,
a velocity or a position, depending on what carries it.

### `Y`

**Type:** `double`

Vertical component.


*No fields found.*

## Enums
*No enums found.*

## Methods
### ToString

Both axes, so printing one while tuning a layer is readable.

**Return Type:** `string` - `(X, Y)`.

---

