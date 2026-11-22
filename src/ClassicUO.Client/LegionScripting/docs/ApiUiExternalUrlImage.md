---
title: ApiUiExternalUrlImage
description: "Wraps an `ExternalUrlImage` control, which downloads an image from a URL — or decodes an inline data URI — in the background and displays it once available. The content is cached per source string, so the same image is only fetched and uploaded to the GPU once. The control keeps its configured size whether or not the image has finished loading."
---

## Class Description
Wraps an `ExternalUrlImage` control, which downloads an image from a URL — or decodes an inline
data URI — in the background and displays it once available. The content is cached per source string, so the
same image is only fetched and uploaded to the GPU once. The control keeps its configured size whether or not
the image has finished loading.

## Properties
*No properties found.*

*No fields found.*

## Enums
*No enums found.*

## Methods
### LoadImageFromUrl
`(url)`
Replaces the displayed image with the image downloaded from `url`, or decoded from the
inline data URI in `url`.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `url` | `string` | ❌ No | Absolute URL of the image to download, or an inline data URI ( `data:image/png;base64,...`). |

**Return Type:** `void` *(Does not return anything)*

---

