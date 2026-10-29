---
title: PyApiObject
description: "Base for the script-visible value types. Gives each one a readable text form listing its properties, and routes Python's `repr` to it. Override `ToString` for a tighter form."
---

## Class Description
Base for the script-visible value types. Gives each one a readable text form listing its
properties, and routes Python's `repr` to it. Override `ToString` for a tighter
form.

A class rather than an interface because IronPython resolves members off the concrete type, where
a default interface implementation is not found. Value types therefore cannot use this.

## Properties
*No properties found.*


## Enums
*No enums found.*

## Methods
### ToString

The object's property values. Nested API objects and sequences expand in place.

**Return Type:** `string` - The text form of this object.

---

### __repr__

The object's string representation.

**Return Type:** `string` - The same text as `ToString`.

---

