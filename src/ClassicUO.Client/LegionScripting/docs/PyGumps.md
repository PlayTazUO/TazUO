---
title: PyGumps
description: PyGumps class documentation
---

## Properties
*No properties found.*

*No fields found.*

## Enums
*No enums found.*

## Methods
### CreateGump
`(acceptMouseInput, canMove, keepOpen)`
 Get a blank gump.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `acceptMouseInput` | `bool` | ✅ Yes | Allow clicking the gump |
| `canMove` | `bool` | ✅ Yes | Allow the player to move this gump |
| `keepOpen` | `bool` | ✅ Yes | If true, the gump won't be closed if the script stops. Otherwise, it will be closed when the script is stopped. Defaults to false. |

**Return Type:** `PyBaseGump`

---

### CreateModernGump
`(x, y, width, height, resizable, minWidth, minHeight, onResized)`
 Creates a modern nine-slice gump using ModernUIConstants for consistent styling.
 The gump uses the standard modern UI panel texture and border size internally.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No | X position |
| `y` | `int` | ❌ No | Y position |
| `width` | `int` | ❌ No | Initial width |
| `height` | `int` | ❌ No | Initial height |
| `resizable` | `bool` | ✅ Yes | Whether the gump can be resized by dragging corners (default: true) |
| `minWidth` | `int` | ✅ Yes | Minimum width (default: 50) |
| `minHeight` | `int` | ✅ Yes | Minimum height (default: 50) |
| `onResized` | `object` | ✅ Yes | Optional callback function called when the gump is resized |

**Return Type:** `PyNineSliceGump`

---

### AddGump
`(g)`
 Add a gump to the players screen.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `g` | `object` | ❌ No | The gump to add |

**Return Type:** `void` *(Does not return anything)*

---

### CreateGumpCheckbox
`(text, hue, isChecked)`
 Create a checkbox for gumps.
  Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 cb = API.CreateGumpCheckbox("Check me?!")
 g.Add(cb)
 API.AddGump(g)

 API.SysMsg("Checkbox checked: " + str(cb.IsChecked))
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes | Optional text label |
| `hue` | `ushort` | ✅ Yes | Optional hue |
| `isChecked` | `bool` | ✅ Yes | Default false, set to true if you want this checkbox checked on creation |

**Return Type:** `PyCheckbox`

---

### CreateGumpLabel
`(text, hue)`
 Create a label for a gump.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpLabel("Hello World!"))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No | The text |
| `hue` | `ushort` | ✅ Yes | The hue of the text |

**Return Type:** `PyLabel`

---

### CreateGumpColorBox
`(opacity, color)`
 Get a transparent color box for gumps.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 cb = API.CreateGumpColorBox(0.5, "#000000")
 cb.SetWidth(200)
 cb.SetHeight(200)
 g.Add(cb)
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `opacity` | `float` | ✅ Yes | 0.5 = 50% |
| `color` | `string` | ✅ Yes | Html color code like #000000 |

**Return Type:** `PyAlphaBlendControl`

---

### CreateGumpItemPic
`(graphic, width, height)`
 Create a picture of an item.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `uint` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `PyResizableStaticPic`

---

### CreateGumpButton
`(text, hue, normal, pressed, hover)`
 Create a button for gumps.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 button = API.CreateGumpButton("Click Me!")
 g.Add(button)
 API.AddGump(g)

 while True:
   API.SysMsg("Button currently clicked?: " + str(button.IsClicked))
   API.SysMsg("Button clicked since last check?: " + str(button.HasBeenClicked()))
   API.Pause(0.2)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes |  |
| `hue` | `ushort` | ✅ Yes |  |
| `normal` | `ushort` | ✅ Yes | Graphic when not clicked or hovering |
| `pressed` | `ushort` | ✅ Yes | Graphic when pressed |
| `hover` | `ushort` | ✅ Yes | Graphic on hover |

**Return Type:** `PyButton`

---

### CreateSimpleButton
`(text, width, height)`
 Create a simple button, does not use graphics.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 button = API.CreateSimpleButton("Click Me!", 100, 20)
 g.Add(button)
 API.AddGump(g)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `PyNiceButton`

---

### CreateGumpRadioButton
`(text, group, inactive, active, hue, isChecked)`
 Create a radio button for gumps, use group numbers to only allow one item to be checked at a time.
 Example:
 ```py
 g = API.CreateGump()
 g.SetRect(100, 100, 200, 200)
 rb = API.CreateGumpRadioButton("Click Me!", 1)
 g.Add(rb)
 API.AddGump(g)
 API.SysMsg("Radio button checked?: " + str(rb.IsChecked))
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes | Optional text |
| `group` | `int` | ✅ Yes | Group ID |
| `inactive` | `ushort` | ✅ Yes | Unchecked graphic |
| `active` | `ushort` | ✅ Yes | Checked graphic |
| `hue` | `ushort` | ✅ Yes | Text color |
| `isChecked` | `bool` | ✅ Yes | Defaults false, set to true if you want this button checked by default. |

**Return Type:** `PyRadioButton`

---

### CreateGumpTextBox
`(text, width, height, multiline)`
 Create a text area control.
 Example:
 ```py
 w = 500
 h = 600

 gump = API.CreateGump(True, True)
 gump.SetWidth(w)
 gump.SetHeight(h)
 gump.CenterXInViewPort()
 gump.CenterYInViewPort()

 bg = API.CreateGumpColorBox(0.7, "#D4202020")
 bg.SetWidth(w)
 bg.SetHeight(h)

 gump.Add(bg)

 textbox = API.CreateGumpTextBox("Text example", w, h, True)

 gump.Add(textbox)

 API.AddGump(gump)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ✅ Yes |  |
| `width` | `int` | ✅ Yes |  |
| `height` | `int` | ✅ Yes |  |
| `multiline` | `bool` | ✅ Yes |  |

**Return Type:** `PyTTFTextInputField`

---

### CreateGumpTTFLabel
`(text, size, color, font, aligned, maxWidth, applyStroke)`
 Create a TTF label with advanced options.
 Example:
 ```py
 gump = API.CreateGump()
 gump.SetRect(100, 100, 200, 200)

 ttflabel = API.CreateGumpTTFLabel("Example label", 25, "#F100DD", "alagard")
 ttflabel.SetRect(10, 10, 180, 30)
 gump.Add(ttflabel)

 API.AddGump(gump) #Add the gump to the players screen
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `text` | `string` | ❌ No |  |
| `size` | `float` | ❌ No | Font size |
| `color` | `string` | ✅ Yes | Hex color: #FFFFFF. Must begin with #. |
| `font` | `string` | ✅ Yes | Must have the font installed in TazUO |
| `aligned` | `string` | ✅ Yes | left/center/right. Must set a max width for this to work. |
| `maxWidth` | `int` | ✅ Yes | Max width before going to the next line |
| `applyStroke` | `bool` | ✅ Yes | Uses players stroke settings, this turns it on or off |

**Return Type:** `PyTextBox`

---

### CreateGumpSimpleProgressBar
`(width, height, backgroundColor, foregroundColor, value, max)`
 Create a progress bar. Can be updated as needed with `bar.SetProgress(current, max)`.
 Example:
 ```py
 gump = API.CreateGump()
 gump.SetRect(100, 100, 400, 200)

 pb = API.CreateGumpSimpleProgressBar(400, 200)
 gump.Add(pb)

 API.AddGump(gump)

 cur = 0
 max = 100

 while True:
   pb.SetProgress(cur, max)
   if cur >= max:
   break
   cur += 1
   API.Pause(0.5)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The width of the bar |
| `height` | `int` | ❌ No | The height of the bar |
| `backgroundColor` | `string` | ✅ Yes | The background color(Hex color like #616161) |
| `foregroundColor` | `string` | ✅ Yes | The foreground color(Hex color like #212121) |
| `value` | `int` | ✅ Yes | The current value, for example 70 |
| `max` | `int` | ✅ Yes | The max value(or what would be 100%), for example 100 |

**Return Type:** `PySimpleProgressBar`

---

### CreateGumpScrollArea
`(x, y, width, height)`
 Create a scrolling area, add and position controls to it directly.
 Example:
 ```py
 sa = API.CreateGumpScrollArea(0, 60, 200, 140)
 gump.Add(sa)

 for i in range(10):
     label = API.CreateGumpTTFLabel(f"Label {i + 1}", 20, "#FFFFFF", "alagard")
     label.SetRect(5, i * 20, 180, 20)
     sa.Add(label)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `int` | ❌ No |  |
| `y` | `int` | ❌ No |  |
| `width` | `int` | ❌ No |  |
| `height` | `int` | ❌ No |  |

**Return Type:** `PyScrollArea`

---

### CreateGumpPic
`(graphic, x, y, hue)`
 Create a gump pic(Use this for gump art, not item art)
 Example:
 ```py
 gumpPic = API.CreateGumpPic(0xafb)
 gump.Add(gumpPic)


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `graphic` | `ushort` | ❌ No |  |
| `x` | `int` | ✅ Yes |  |
| `y` | `int` | ✅ Yes |  |
| `hue` | `ushort` | ✅ Yes |  |

**Return Type:** `PyGumpPic`

---

### CreateDropDown
`(width, items, selectedIndex)`
 Creates a dropdown control (combobox) with the specified width and items.


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `width` | `int` | ❌ No | The width of the dropdown control |
| `items` | `string[]` | ❌ No | Array of strings to display as dropdown options |
| `selectedIndex` | `int` | ✅ Yes | The initially selected item index (default: 0) |

**Return Type:** `PyControlDropDown`

---

### AddControlOnClick
`(control, onClick, leftOnly)`
 Add an onClick callback to a control.
 Example:
 ```py
 def myfunc:
   API.SysMsg("Something clicked!")
 bg = API.CreateGumpColorBox(0.7, "#D4202020")
 API.AddControlOnClick(bg, myfunc)
 while True:
   API.ProcessCallbacks()
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `control` | `object` | ❌ No | The control listening for clicks |
| `onClick` | `object` | ❌ No | The callback function |
| `leftOnly` | `bool` | ✅ Yes | Only accept left mouse clicks? |

**Return Type:** `object`

---

### AddControlOnDisposed
`(control, onDispose)`
 Add onDispose(Closed) callback to a control.
 Example:
 ```py
 def onClose():
     API.Stop()

 gump = API.CreateGump()
 gump.SetRect(100, 100, 200, 200)

 bg = API.CreateGumpColorBox(opacity=0.7, color="#000000")
 gump.Add(bg.SetRect(0, 0, 200, 200))

 API.AddControlOnDisposed(gump, onClose)
 ```


**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `control` | `PyBaseControl` | ❌ No |  |
| `onDispose` | `object` | ❌ No |  |

**Return Type:** `PyBaseControl`

---

