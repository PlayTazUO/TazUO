---
title: ApiPulseSpec
description: "Time-varying swell of a layer's strength. Distinct from a noise field, which varies strength across the screen at one moment: this varies it everywhere at once, over time."
---

## Class Description
Time-varying swell of a layer's strength. Distinct from a noise field, which varies strength
across the screen at one moment: this varies it everywhere at once, over time.

## Properties
### `Frequency`

**Type:** `double`

Breathing rate in Hz. Hard-capped at 3 Hz when the layer is baked - flashing above roughly
that is a photosensitive-epilepsy hazard, and nothing may raise it.

### `Amplitude`

**Type:** `double`

Depth of that breathing, as a fraction of `ApiLayerEffect.Strength`.


*No fields found.*

## Enums
*No enums found.*

## Methods
