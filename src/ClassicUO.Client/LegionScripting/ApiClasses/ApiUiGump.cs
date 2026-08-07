using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.LegionScripting.ApiClasses;
using FontStashSharp.RichText;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiGump(LegionAPI api)
{
        private readonly CancellationToken _cachedToken = api.CancellationToken.Token;

        private T OnMain<T>(Func<T> func) => MainThreadQueue.InvokeOnMainThread(func, _cachedToken);
        private void OnMain(Action action) => MainThreadQueue.InvokeOnMainThread(action, _cachedToken);

    /// <summary>
    /// Get a blank gump.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// g.Add(API.CreateGumpLabel("Hello World!"))
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="acceptMouseInput">Allow clicking the gump</param>
    /// <param name="canMove">Allow the player to move this gump</param>
    /// <param name="keepOpen">If true, the gump won't be closed if the script stops. Otherwise, it will be closed when the script is stopped. Defaults to false.</param>
    /// <returns>A new, empty gump</returns>
    public ApiUiBaseGump CreateGump(bool acceptMouseInput = true, bool canMove = true, bool keepOpen = false) =>
        OnMain(() =>
        {
            var g = new Gump(api.World, 0, 0)
            {
                AcceptMouseInput = acceptMouseInput,
                CanMove = canMove,
                WantUpdateSize = true
            };

            ApiUiBaseGump apiUiGump = new(g);

            if (!keepOpen)
                api._gumps.Add(g);

            return apiUiGump;
        });

    /// <summary>
    /// Creates a modern nine-slice gump using ModernUIConstants for consistent styling.
    /// The gump uses the standard modern UI panel texture and border size internally.
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Initial width</param>
    /// <param name="height">Initial height</param>
    /// <param name="resizable">Whether the gump can be resized by dragging corners (default: true)</param>
    /// <param name="minWidth">Minimum width (default: 50)</param>
    /// <param name="minHeight">Minimum height (default: 50)</param>
    /// <param name="onResized">Optional callback function called when the gump is resized</param>
    /// <returns>A ApiUiNineSliceGump wrapper containing the nine-slice gump control</returns>
    public ApiUiNineSliceGump CreateModernGump(int x, int y, int width, int height, bool resizable = true, int minWidth = 50, int minHeight = 50, object onResized = null, bool keepOpen = false) => 
        OnMain(() =>
        {
            var g = new ApiUiNineSliceGump(api, x, y, width, height, resizable, minWidth, minHeight, onResized);

            if (!keepOpen)
                api._gumps.Add(g.Gump);

            return g;
        });

    /// <summary>
    /// Add a gump to the players screen.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// g.Add(API.CreateGumpLabel("Hello World!"))
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="g">The gump to add</param>
    public void AddGump(object g) => MainThreadQueue.InvokeOnMainThread(() =>
    {
        switch (g)
        {
            case Gump gump:
                UIManager.Add(gump);
                break;
            case IApiGump { Gump: not null } pyGump:
                UIManager.Add(pyGump.Gump);
                break;
        }
    });

    /// <summary>
    /// Create a checkbox for gumps.
    /// /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// cb = API.CreateGumpCheckbox("Check me?!")
    /// g.Add(cb)
    /// API.AddGump(g)
    ///
    /// API.SysMsg("Checkbox checked: " + str(cb.IsChecked))
    /// ```
    /// </summary>
    /// <param name="text">Optional text label</param>
    /// <param name="hue">Optional hue</param>
    /// <param name="isChecked">Default false, set to true if you want this checkbox checked on creation</param>
    /// <returns>The checkbox</returns>
    public ApiUiCheckbox CreateGumpCheckbox(string text = "", ushort hue = 0, bool isChecked = false) =>
        OnMain(() => new ApiUiCheckbox(new Checkbox(0x00D2, 0x00D3, text, color: hue) { CanMove = true, IsChecked = isChecked }));

    /// <summary>
    /// Create a label for a gump.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// g.Add(API.CreateGumpLabel("Hello World!"))
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="hue">The hue of the text</param>
    /// <returns></returns>
    public ApiUiLabel CreateGumpLabel(string text, ushort hue = 996) => 
        OnMain(() => new ApiUiLabel(new Label(text, true, hue))
            {
                CanMove = true
            });

    /// <summary>
    /// Get a transparent color box for gumps.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// cb = API.CreateGumpColorBox(0.5, "#000000")
    /// cb.SetWidth(200)
    /// cb.SetHeight(200)
    /// g.Add(cb)
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="opacity">0.5 = 50%</param>
    /// <param name="color">Html color code like #000000</param>
    /// <returns></returns>
    public ApiUiAlphaBlendControl CreateGumpColorBox(float opacity = 0.7f, string color = "#000000") => OnMain(() =>
    {
        var bc = new AlphaBlendControl(opacity) { BaseColor = Utility.GetColorFromHex(color) };

        return new ApiUiAlphaBlendControl(bc);
    });

    /// <summary>
    /// Create a picture of an item.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// g.Add(API.CreateGumpItemPic(0x0E78, 50, 50))
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="graphic"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public ApiUiResizableStaticPic CreateGumpItemPic(uint graphic, int width, int height) => OnMain(() =>
    {
        var pic = new ResizableStaticPic(graphic, width, height)
        {
            AcceptMouseInput = false
        };

        return new ApiUiResizableStaticPic(pic);
    });

    /// <summary>
    /// Create an image control that displays a named PNG texture loaded from a ZIP archive.
    /// Place the PNG anywhere inside the ZIP (outside gumps/ and art/ folders) and reference it by its path within the archive.
    /// Example:
    /// ```py
    /// # In your zip: icons/sword.png
    /// img = API.Gumps.LegionTextureControl("icons/sword.png")
    /// img.SetPos(10, 10)
    /// g.Add(img)
    /// ```
    /// </summary>
    /// <param name="textureName">The PNG path within the ZIP, e.g. "icons/sword.png" or just "sword.png" if at the root</param>
    /// <param name="width">Display width in pixels; 0 uses the image's natural width</param>
    /// <param name="height">Display height in pixels; 0 uses the image's natural height</param>
    /// <returns>A LegionTexture control</returns>
    public ApiUiLegionTexture LegionTextureControl(string textureName, int width = 0, int height = 0) =>
        OnMain(() => new ApiUiLegionTexture(new LegionTexturePic(textureName, width, height)));

    /// <summary>
    /// Create a button for gumps.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// button = API.CreateGumpButton("Click Me!")
    /// g.Add(button)
    /// API.AddGump(g)
    ///
    /// while True:
    ///   API.SysMsg("Button currently clicked?: " + str(button.IsClicked))
    ///   API.SysMsg("Button clicked since last check?: " + str(button.HasBeenClicked()))
    ///   API.Pause(0.2)
    /// ```
    /// </summary>
    /// <param name="text"></param>
    /// <param name="hue"></param>
    /// <param name="normal">Graphic when not clicked or hovering</param>
    /// <param name="pressed">Graphic when pressed</param>
    /// <param name="hover">Graphic on hover</param>
    /// <returns></returns>
    public ApiUiButton CreateGumpButton(string text = "", ushort hue = 996, ushort normal = 0x00EF, ushort pressed = 0x00F0, ushort hover = 0x00EE) => OnMain(() =>
    {
        var b = new Button(0, normal, pressed, hover, caption: text, normalHue: hue, hoverHue: hue);

        return new ApiUiButton(b);
    });

    /// <summary>
    /// Create a simple button, does not use graphics.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// button = API.CreateSimpleButton("Click Me!", 100, 20)
    /// g.Add(button)
    /// API.AddGump(g)
    /// ```
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public ApiUiNiceButton CreateSimpleButton(string text, int width, int height) => OnMain(() =>
    {
        NiceButton b = new(0, 0, width, height, ButtonAction.Default, text);
        b.AlwaysShowBackground = true;

        return new ApiUiNiceButton(b);
    });

    /// <summary>
    /// Create a radio button for gumps, use group numbers to only allow one item to be checked at a time.
    /// Example:
    /// ```py
    /// g = API.CreateGump()
    /// g.SetRect(100, 100, 200, 200)
    /// rb = API.CreateGumpRadioButton("Click Me!", 1)
    /// g.Add(rb)
    /// API.AddGump(g)
    /// API.SysMsg("Radio button checked?: " + str(rb.IsChecked))
    /// ```
    /// </summary>
    /// <param name="text">Optional text</param>
    /// <param name="group">Group ID</param>
    /// <param name="inactive">Unchecked graphic</param>
    /// <param name="active">Checked graphic</param>
    /// <param name="hue">Text color</param>
    /// <param name="isChecked">Defaults false, set to true if you want this button checked by default.</param>
    /// <returns></returns>
    public ApiUiRadioButton CreateGumpRadioButton(string text = "", int group = 0, ushort inactive = 0x00D0, ushort active = 0x00D1, ushort hue = 0xFFFF, bool isChecked = false) => OnMain(() =>
    {
        var rb = new RadioButton(group, inactive, active, text, color: hue);
        rb.IsChecked = isChecked;
        return new ApiUiRadioButton(rb);
    });

    /// <summary>
    /// Create a text area control.
    /// Example:
    /// ```py
    /// w = 500
    /// h = 600
    ///
    /// gump = API.CreateGump(True, True)
    /// gump.SetWidth(w)
    /// gump.SetHeight(h)
    /// gump.CenterXInViewPort()
    /// gump.CenterYInViewPort()
    ///
    /// bg = API.CreateGumpColorBox(0.7, "#D4202020")
    /// bg.SetWidth(w)
    /// bg.SetHeight(h)
    ///
    /// gump.Add(bg)
    ///
    /// textbox = API.CreateGumpTextBox("Text example", w, h, True)
    ///
    /// gump.Add(textbox)
    ///
    /// API.AddGump(gump)
    /// ```
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="multiline"></param>
    /// <param name="fontSize">TTF font size, default is 20</param>
    /// <returns></returns>
    public ApiUiTtfTextInputField CreateGumpTextBox(string text = "", int width = 200, int height = 30, bool multiline = false, float fontSize = 20) =>
        OnMain(() =>
            new ApiUiTtfTextInputField(new TTFTextInputField(width, height, text: text, multiline: multiline, convertHtmlColors: false, fontSize: fontSize) { CanMove = true }));

    /// <summary>
    /// Create a TTF label with advanced options.
    /// Example:
    /// ```py
    /// gump = API.CreateGump()
    /// gump.SetRect(100, 100, 200, 200)
    ///
    /// ttflabel = API.CreateGumpTTFLabel("Example label", 25, "#F100DD", "alagard")
    /// ttflabel.SetRect(10, 10, 180, 30)
    /// gump.Add(ttflabel)
    ///
    /// API.AddGump(gump) #Add the gump to the players screen
    /// ```
    /// </summary>
    /// <param name="text"></param>
    /// <param name="size">Font size</param>
    /// <param name="color">Hex color: #FFFFFF. Must begin with #.</param>
    /// <param name="font">Must have the font installed in TazUO</param>
    /// <param name="aligned">left/center/right. Must set a max width for this to work.</param>
    /// <param name="maxWidth">Max width before going to the next line</param>
    /// <param name="applyStroke">Uses players stroke settings, this turns it on or off</param>
    /// <returns></returns>
    public ApiUiTextBox CreateGumpTTFLabel
        (string text, float size, string color = "#FFFFFF", string font = TrueTypeLoader.EMBEDDED_FONT, string aligned = "left", int maxWidth = 0, bool applyStroke = false)
    {
        var opts = TextBox.RTLOptions.Default();

        switch (aligned.ToLower())
        {
            case "left": opts.Align = TextHorizontalAlignment.Left; break;

            case "middle":
            case "center": opts.Align = TextHorizontalAlignment.Center; break;

            case "right": opts.Align = TextHorizontalAlignment.Right; break;
        }

        if (applyStroke)
            opts.StrokeEffect = true;

        if (maxWidth > 0)
            opts.Width = maxWidth;

        // Construct on the main thread: TextBox measures its text through FontStashSharp, whose shared
        // font caches (glyph/kerning) are not thread-safe. Measuring here (a script background thread)
        // while the main thread measures/draws the same fonts corrupts those caches and crashes.
        return OnMain(() =>
            new ApiUiTextBox(TextBox.GetOne(text, font, size, Utility.GetColorFromHex(color), opts)));
    }

    /// <summary>
    /// Create a progress bar. Can be updated as needed with `bar.SetProgress(current, max)`.
    /// Example:
    /// ```py
    /// gump = API.CreateGump()
    /// gump.SetRect(100, 100, 400, 200)
    ///
    /// pb = API.CreateGumpSimpleProgressBar(400, 200)
    /// gump.Add(pb)
    ///
    /// API.AddGump(gump)
    ///
    /// cur = 0
    /// max = 100
    ///
    /// while True:
    ///     pb.SetProgress(cur, max)
    ///     if cur >= max:
    ///         break
    ///     cur += 1
    ///     API.Pause(0.5)
    /// ```
    /// </summary>
    /// <param name="width">The width of the bar</param>
    /// <param name="height">The height of the bar</param>
    /// <param name="backgroundColor">The background color(Hex color like #616161)</param>
    /// <param name="foregroundColor">The foreground color(Hex color like #212121)</param>
    /// <param name="value">The current value, for example 70</param>
    /// <param name="max">The max value(or what would be 100%), for example 100</param>
    /// <returns></returns>
    public ApiUiSimpleProgressBar CreateGumpSimpleProgressBar
        (int width, int height, string backgroundColor = "#616161", string foregroundColor = "#212121", int value = 100, int max = 100) => OnMain(() =>
        {
            SimpleProgressBar bar = new(backgroundColor, foregroundColor, width, height);
            bar.SetProgress(value, max);

            return new ApiUiSimpleProgressBar(bar);
        });

    /// <summary>
    /// Create a scrolling area, add and position controls to it directly.
    /// Example:
    /// ```py
    /// sa = API.CreateGumpScrollArea(0, 60, 200, 140)
    /// gump.Add(sa)
    ///
    /// for i in range(10):
    ///     label = API.CreateGumpTTFLabel(f"Label {i + 1}", 20, "#FFFFFF", "alagard")
    ///     label.SetRect(5, i * 20, 180, 20)
    ///     sa.Add(label)
    /// ```
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public ApiUiScrollArea CreateGumpScrollArea(int x, int y, int width, int height) => OnMain(() =>
        new ApiUiScrollArea(new ScrollArea(x, y, width, height, true)));

    /// <summary>
    /// Create a gump pic(Use this for gump art, not item art)
    /// Example:
    /// ```py
    /// gumpPic = API.CreateGumpPic(0xafb)
    /// gump.Add(gumpPic)
    /// </summary>
    /// <param name="graphic"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="hue"></param>
    /// <returns></returns>
    public ApiUiGumpPic CreateGumpPic(ushort graphic, int x = 0, int y = 0, ushort hue = 0) => OnMain(() =>
    {
        return new ApiUiGumpPic(new GumpPic(x, y, graphic, hue));
    });

    /// <summary>
    /// Create a gump pic that tiles(repeats) (Use this for gump art, not item art)
    /// Example:
    /// ```py
    /// gumpPic = API.CreateTiledGumpPic(0xafb, 100, 100)
    /// gump.Add(gumpPic)
    /// </summary>
    /// <param name="graphic"></param>
    /// <param name="height"></param>
    /// <param name="hue"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public ApiUiTiledGumpPic CreateTiledGumpPic(ushort graphic, int width, int height, ushort hue = 0) => OnMain(() =>
    {
        return new ApiUiTiledGumpPic(new GumpPicTiled(0, 0, width, height, graphic) { Hue = hue });
    });

    /// <summary>
    /// Creates a dropdown control (combobox) with the specified width and items.
    /// </summary>
    /// <param name="width">The width of the dropdown control</param>
    /// <param name="items">Array of strings to display as dropdown options</param>
    /// <param name="selectedIndex">The initially selected item index (default: 0)</param>
    /// <returns>A ApiUiControlDropDown wrapper containing the combobox control</returns>
    public ApiUiControlDropDown CreateDropDown(int width, IList<string> items, int selectedIndex = 0) => OnMain(() =>
    {
        return new ApiUiControlDropDown(new Combobox(0, 0, width, items.ToArray(), selectedIndex), api);
    });

    /// <summary>
    /// Add an onClick callback to a control.
    /// Example:
    /// ```py
    /// def myfunc:
    ///   API.SysMsg("Something clicked!")
    /// bg = API.CreateGumpColorBox(0.7, "#D4202020")
    /// API.AddControlOnClick(bg, myfunc)
    /// while True:
    ///   API.ProcessCallbacks()
    /// ```
    /// </summary>
    /// <param name="control">The control listening for clicks</param>
    /// <param name="onClick">The callback function</param>
    /// <param name="leftOnly">Only accept left mouse clicks?</param>
    /// <returns>Returns the control so methods can be chained.</returns>
    public object AddControlOnClick(object control, object onClick, bool leftOnly = true)
    {
        if (control == null || onClick == null || !api.CallbackChannel.CanInvoke(onClick))
            return control;

        return OnMain(() => {
            Control wControl = null;

            if(control is Control)
                wControl = (Control)control;
            else if (control is ApiUiBaseControl pbc && pbc.Control != null)
                wControl = pbc.Control;

            if (wControl == null)
                return control;

            wControl.AcceptMouseInput = true;

            wControl.MouseUp += (_, e) =>
            {
                if (leftOnly && e.Button != MouseButtonType.Left)
                    return;

                api?.ScheduleCallback(onClick);
            };

            return control;
        });
    }

    /// <summary>
    /// Add onDispose(Closed) callback to a control.
    /// Example:
    /// ```py
    /// def onClose():
    ///     API.Stop()
    ///
    /// gump = API.CreateGump()
    /// gump.SetRect(100, 100, 200, 200)
    ///
    /// bg = API.CreateGumpColorBox(opacity=0.7, color="#000000")
    /// gump.Add(bg.SetRect(0, 0, 200, 200))
    ///
    /// API.AddControlOnDisposed(gump, onClose)
    /// ```
    /// </summary>
    /// <param name="control"></param>
    /// <param name="onDispose"></param>
    /// <returns></returns>
    public ApiUiBaseControl AddControlOnDisposed(ApiUiBaseControl control, object onDispose)
    {
        if (control == null || onDispose == null || control.Control == null || !api.CallbackChannel.CanInvoke(onDispose))
            return control;

        return OnMain(() => {
            control.Control.Disposed += (_, _) => api?.ScheduleCallback(onDispose);
            return control;
        });
    }
}
