---
title: ApiUiExternalUrlImage
description: "Wraps an `ExternalUrlImage` control, which downloads an image from a URL in the background and displays it once available. The download is cached per URL, so the same image is only fetched and uploaded to the GPU once. The control keeps its configured size whether or not the image has finished loading."
---

## Class Description
Wraps an `ExternalUrlImage` control, which downloads an image from a URL in the background and
displays it once available. The download is cached per URL, so the same image is only fetched and uploaded
to the GPU once. The control keeps its configured size whether or not the image has finished loading.

## Properties
*No properties found.*

*No fields found.*

## Enums
*No enums found.*

## Methods
### LoadImageFromUrl
`(url)`
Replaces the displayed image with the image downloaded from `url`.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `url` | `string` | ❌ No | Absolute URL of the image to download. |

**Return Type:** `void` *(Does not return anything)*

---

