---
title: ApiRect
description: "A mutable rectangle in pixels, carrying a control's position and size. Read one from `ApiUiBaseControl.Bounds`, edit the fields, then assign it back to apply all four in one non-blocking call."
---

## Class Description
A mutable rectangle in pixels, carrying a control's position and size. Read one from
`ApiUiBaseControl.Bounds`, edit the fields, then assign it back to apply all four in
one non-blocking call.

A class rather than XNA's `Rectangle` because IronPython silently discards field writes on a
boxed value type, which would break the edit-then-assign pattern.

## Properties
### `X`

**Type:** `int`

Left edge.

### `Y`

**Type:** `int`

Top edge.

### `Width`

**Type:** `int`

Width in pixels.

### `Height`

**Type:** `int`

Height in pixels.


*No fields found.*

## Enums
*No enums found.*

## Methods
### ToString

Readable text form, so printing one while tuning a layout shows all four fields.

**Return Type:** `string` - `ApiRect(X=.., Y=.., Width=.., Height=..)`.

---

