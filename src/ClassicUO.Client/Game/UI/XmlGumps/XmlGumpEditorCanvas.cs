#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI;

/// <summary>
/// The visual canvas of the XML gump editor: renders one preview widget per element, lets the user
/// select and drag them, and reports selection and geometry changes back to the owning window.
/// </summary>
/// <remarks>
/// Elements are drawn with Myra primitives rather than the game's own controls, because a Myra widget
/// has no access to the game batcher a classic control draws through. Each preview is positioned
/// absolutely through <see cref="Widget.Left"/>/<see cref="Widget.Top"/>, which Myra applies on top of
/// the panel-relative layout position.
/// </remarks>
public sealed class XmlGumpEditorCanvas : Panel
{
    #region Public constants

    public const int CANVAS_WIDTH = 720;
    public const int CANVAS_HEIGHT = 520;

    #endregion

    #region Public events

    /// <summary>Raised when the selected element changes, or is cleared.</summary>
    public event Action<XmlGumpElement?>? SelectionChanged;

    /// <summary>
    /// Raised after the selected element has moved, from a drag or an arrow-key nudge. The preview is
    /// already at its new position when this fires.
    /// </summary>
    public event Action? ElementMoved;

    #endregion

    #region Private members

    private const int MIN_SIZE = 4;

    private readonly World _world;
    private readonly List<ElementPreview> _previews = new();
    private readonly Dictionary<uint, Texture2D> _hueTextures = new();

    /// <summary>
    /// Re-applies the live text/progress values, rebuilt alongside the previews. Running these instead
    /// of rebuilding keeps a refresh from re-creating every widget several times a second.
    /// </summary>
    private readonly List<Action> _liveUpdaters = new();

    private XmlGumpElement? _selected;
    private ElementPreview? _dragPreview;
    private Point _dragStartMouse;
    private int _dragStartX;
    private int _dragStartY;

    #endregion

    #region Ctor

    public XmlGumpEditorCanvas(World world)
    {
        _world = world;

        Width = CANVAS_WIDTH;
        Height = CANVAS_HEIGHT;
        ClipToBounds = true;

        // Transparent, but not null: a null background makes the canvas fall through Myra's input pass
        // (Container.InputFallsThrough keys off it), so clicks would reach the game instead of
        // selecting. A transparent brush draws nothing yet still claims the click, letting the preview
        // sit over the game.
        Background = new SolidBrush(Color.Transparent);

        // Required to receive arrow keys: Myra only routes key input to the focused widget, and only a
        // widget that accepts keyboard focus can be focused.
        AcceptsKeyboardFocus = true;

        TouchDown += OnCanvasTouchDown;
    }

    #endregion

    #region Public accessors

    /// <summary>The element the property panel is editing, or null.</summary>
    public XmlGumpElement? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value))
                return;

            _selected = value;

            foreach (ElementPreview preview in _previews)
                preview.IsSelected = ReferenceEquals(preview.Element, value);

            // Focus the canvas so the arrow keys nudge the selection, when this desktop is the one
            // processing input.
            if (value != null && Desktop != null)
                SetKeyboardFocus();

            SelectionChanged?.Invoke(value);
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Rebuilds every preview widget from the given top-level elements.
    /// </summary>
    /// <remarks>
    /// Takes the elements rather than holding a document reference: the editor swaps its document on
    /// New/Open, and a cached reference would leave the canvas rendering the previous one.
    /// </remarks>
    public void Rebuild(List<XmlGumpElement> elements)
    {
        Widgets.Clear();
        _previews.Clear();
        _liveUpdaters.Clear();

        foreach (XmlGumpElement element in elements)
        {
            try
            {
                BuildPreview(element, this);
            }
            catch (Exception e)
            {
                ClassicUO.Utility.Logging.Log.Error($"Xml gump editor failed to preview a '{element.Tag}' element: {e}");
            }
        }

        if (_selected != null && !ContainsElement(elements, _selected))
            Selected = null;
    }

    /// <summary>Re-resolves the values shown by live text and progress elements.</summary>
    public void RefreshLive()
    {
        foreach (Action update in _liveUpdaters)
            update();
    }

    private static bool ContainsElement(List<XmlGumpElement> elements, XmlGumpElement target)
    {
        foreach (XmlGumpElement element in elements)
        {
            if (ReferenceEquals(element, target) || ContainsElement(element.Children, target))
                return true;
        }

        return false;
    }

    /// <summary>Releases the baked hue textures held for this canvas. Call when the editor closes.</summary>
    public void Release()
    {
        if (Desktop != null)
        {
            Desktop.MouseMoved -= OnDesktopMouseMoved;
            Desktop.TouchUp -= OnDesktopTouchUp;
        }

        _dragPreview = null;

        foreach (Texture2D? texture in _hueTextures.Values)
            texture?.Dispose();

        _hueTextures.Clear();
    }

    #endregion

    #region Event handlers

    private void OnCanvasTouchDown(object? sender, TouchEventArgs e)
    {
        if (Desktop == null || e.Button != TouchButton.Left)
            return;

        Point global = Desktop.MousePosition;
        ElementPreview? hit = null;

        for (int i = _previews.Count - 1; i >= 0; i--)
        {
            if (_previews[i].ContainsGlobalPoint(global))
            {
                hit = _previews[i];

                break;
            }
        }

        Selected = hit?.Element;

        if (hit == null)
            return;

        _dragPreview = hit;
        _dragStartMouse = global;
        _dragStartX = hit.Element.GetInt("x");
        _dragStartY = hit.Element.GetInt("y");

        Desktop.MouseMoved += OnDesktopMouseMoved;
        Desktop.TouchUp += OnDesktopTouchUp;
    }

    private void OnDesktopMouseMoved(object? sender, EventArgs e)
    {
        if (_dragPreview == null || Desktop == null)
            return;

        Point current = Desktop.MousePosition;
        int x = _dragStartX + (current.X - _dragStartMouse.X);
        int y = _dragStartY + (current.Y - _dragStartMouse.Y);

        _dragPreview.Element.SetAttribute("x", x.ToString());
        _dragPreview.Element.SetAttribute("y", y.ToString());
        _dragPreview.Left = x;
        _dragPreview.Top = y;
    }

    private void OnDesktopTouchUp(object? sender, EventArgs e)
    {
        if (_dragPreview == null)
            return;

        Desktop.MouseMoved -= OnDesktopMouseMoved;
        Desktop.TouchUp -= OnDesktopTouchUp;
        _dragPreview = null;
        ElementMoved?.Invoke();
    }

    /// <summary>
    /// Moves the selected element one pixel for each arrow key press, for fine placement.
    /// </summary>
    public override void OnKeyDown(Keys key)
    {
        if (_selected == null)
        {
            base.OnKeyDown(key);

            return;
        }

        int dx = 0;
        int dy = 0;

        switch (key)
        {
            case Keys.Left:
                dx = -1;

                break;
            case Keys.Right:
                dx = 1;

                break;
            case Keys.Up:
                dy = -1;

                break;
            case Keys.Down:
                dy = 1;

                break;
            default:
                base.OnKeyDown(key);

                return;
        }

        NudgeSelected(dx, dy);
    }

    private void NudgeSelected(int dx, int dy)
    {
        XmlGumpElement selected = _selected!;
        int x = selected.GetInt("x") + dx;
        int y = selected.GetInt("y") + dy;

        selected.SetAttribute("x", x.ToString());
        selected.SetAttribute("y", y.ToString());

        foreach (ElementPreview preview in _previews)
        {
            if (ReferenceEquals(preview.Element, selected))
            {
                preview.Left = x;
                preview.Top = y;
            }
        }

        ElementMoved?.Invoke();
    }

    #endregion

    #region Preview building

    private void BuildPreview(XmlGumpElement element, Panel parent)
    {
        var preview = new ElementPreview(element)
        {
            Left = element.GetInt("x"),
            Top = element.GetInt("y"),
            IsSelected = ReferenceEquals(_selected, element)
        };

        // Registered before its content, so a container lands earlier in the list than its children and
        // the reverse scan in OnCanvasTouchDown reaches the innermost preview first.
        _previews.Add(preview);

        BuildContent(element, preview, preview);

        if (preview.Width == null)
            preview.Width = MIN_SIZE;

        if (preview.Height == null)
            preview.Height = MIN_SIZE;

        parent.Widgets.Add(preview);
    }

    /// <summary>Builds the visual content of an element into its preview wrapper.</summary>
    private void BuildContent(XmlGumpElement element, Panel preview, Panel parent)
    {
        switch (element.Tag.ToLowerInvariant())
        {
            case "text":
                BuildText(element, preview);
                break;
            case "colorbox":
                BuildColorBox(element, preview);
                break;
            case "image":
                BuildImage(element, preview);
                break;
            case "simple_border":
                BuildBorder(element, preview);
                break;
            case "macro_button_color":
                BuildMacroColor(element, preview);
                break;
            case "macro_button_graphic":
                BuildImage(element, preview, "id");
                break;
            case "color_progress_bar":
                BuildColorProgressBar(element, preview);
                break;
            case "image_progress_bar":
                BuildImageProgressBar(element, preview);
                break;
            case "hp_bar_color":
                BuildHealthBarColor(element, preview);
                break;
            case "hp_bar_image":
                BuildHealthBarImage(element, preview);
                break;
            case "control":
                BuildContainer(element, preview, parent);
                break;
            default:
                BuildUnknown(element, preview);
                break;
        }
    }

    private void BuildUnknown(XmlGumpElement element, Panel preview)
    {
        preview.Width = Math.Max(MIN_SIZE, element.GetInt("width", 32));
        preview.Height = Math.Max(MIN_SIZE, element.GetInt("height", 32));
        preview.Widgets.Add(new Panel { Background = new SolidBrush(new Color(180, 60, 60, 90)) });
    }

    private void BuildText(XmlGumpElement element, Panel preview)
    {
        string fontName = element.GetAttribute("font") is { Length: > 0 } configured ? configured : TrueTypeLoader.EMBEDDED_FONT;
        int size = Math.Max(1, element.GetInt("size", 16));
        int hue = element.GetInt("hue", 997);
        int width = element.GetInt("width", 0);
        string raw = element.InnerText;
        string text = string.IsNullOrEmpty(raw) ? "Text" : XmlGumpHandler.FormatText(_world, raw);

        SpriteFontBase spriteFont;

        try
        {
            spriteFont = TrueTypeLoader.Instance.GetFont(fontName, size);
        }
        catch (Exception)
        {
            // An unknown font name must not stop the preview; fall back to the embedded one.
            spriteFont = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, size);
        }

        Vector2 measured = spriteFont.MeasureString(text);

        var label = new MyraWindows.Widgets.MyraLabel(text, 0)
        {
            Font = spriteFont,
            TextColor = HueColor((ushort)Math.Clamp(hue, 0, ushort.MaxValue), 31),
            Wrap = width > 0
        };

        if (width > 0)
            label.Width = width;

        string align = element.GetAttribute("align") ?? "left";

        switch (align.ToLowerInvariant())
        {
            case "center":
                label.HorizontalAlignment = HorizontalAlignment.Center;

                break;
            case "right":
                label.HorizontalAlignment = HorizontalAlignment.Right;

                break;
        }

        preview.Widgets.Add(label);
        preview.Width = width > 0 ? width : Math.Max(MIN_SIZE, (int)measured.X);
        preview.Height = Math.Max(MIN_SIZE, (int)Math.Ceiling(measured.Y));

        if (element.GetBool("updates"))
            _liveUpdaters.Add(() => label.Text = XmlGumpHandler.FormatText(_world, raw));
    }

    private void BuildColorBox(XmlGumpElement element, Panel preview)
    {
        int width = Math.Max(MIN_SIZE, element.GetInt("width", 20));
        int height = Math.Max(MIN_SIZE, element.GetInt("height", 20));
        float alpha = element.GetFloat("alpha", 1f);

        preview.Width = width;
        preview.Height = height;
        preview.Widgets.Add(new Panel
        {
            Width = width,
            Height = height,
            Background = new SolidBrush(HueColor(element.GetUShort("hue"), 31, alpha))
        });
    }

    private void BuildBorder(XmlGumpElement element, Panel preview)
    {
        int width = Math.Max(MIN_SIZE, element.GetInt("width"));
        int height = Math.Max(MIN_SIZE, element.GetInt("height"));

        preview.Width = width;
        preview.Height = height;
        preview.Widgets.Add(new Panel
        {
            Width = width,
            Height = height,
            Border = new SolidBrush(HueColor(element.GetUShort("hue"), 31)),
            BorderThickness = new Thickness(1)
        });
    }

    private void BuildMacroColor(XmlGumpElement element, Panel preview)
    {
        int width = Math.Max(MIN_SIZE, element.GetInt("width", 32));
        int height = Math.Max(MIN_SIZE, element.GetInt("height", 22));
        float alpha = element.GetFloat("alpha", 1f);

        preview.Width = width;
        preview.Height = height;
        preview.Widgets.Add(new Panel
        {
            Width = width,
            Height = height,
            Background = new SolidBrush(HueColor(element.GetUShort("hue"), 31, alpha))
        });
    }

    private void BuildImage(XmlGumpElement element, Panel preview, string graphicAttribute = "id")
    {
        ushort graphic = element.GetUShort(graphicAttribute);
        ushort hue = element.GetUShort("hue");
        Rectangle? source = ParseRect(element.GetAttribute("rect"));

        var image = new Image { Width = MIN_SIZE, Height = MIN_SIZE };
        TextureRegion? region = GetRegion(graphic, hue, source);

        if (region != null)
        {
            image.Renderable = region;
            image.Width = region.Bounds.Width;
            image.Height = region.Bounds.Height;
        }
        else
        {
            image.Renderable = null;
            image.Background = new SolidBrush(new Color(180, 60, 60, 90));
            image.Width = element.GetInt("width") > 0 ? element.GetInt("width") : 32;
            image.Height = element.GetInt("height") > 0 ? element.GetInt("height") : 32;
        }

        preview.Width = image.Width ?? MIN_SIZE;
        preview.Height = image.Height ?? MIN_SIZE;

        // An explicit size on the element wins, so stretched graphics can be laid out too.
        if (element.GetInt("width") > 0)
        {
            image.Width = element.GetInt("width");
            preview.Width = image.Width;
        }

        if (element.GetInt("height") > 0)
        {
            image.Height = element.GetInt("height");
            preview.Height = image.Height;
        }

        preview.Widgets.Add(image);
    }

    private void BuildColorProgressBar(XmlGumpElement element, Panel preview)
    {
        int width = Math.Max(MIN_SIZE, element.GetInt("width", 100));
        int height = Math.Max(MIN_SIZE, element.GetInt("height", 12));
        bool vertical = string.Equals(element.GetAttribute("direction"), "up", StringComparison.OrdinalIgnoreCase);

        preview.Width = width;
        preview.Height = height;
        preview.Widgets.Add(new Panel { Width = width, Height = height, Background = new SolidBrush(HueColor(element.GetUShort("background_hue"), 20)) });

        var foreground = new Panel { Background = new SolidBrush(HueColor(element.GetUShort("foreground_hue"), 31)) };
        ApplyColorFill(foreground, width, height, vertical, ProgressPercent(element));
        preview.Widgets.Add(foreground);

        if (element.GetBool("updates"))
            _liveUpdaters.Add(() => ApplyColorFill(foreground, width, height, vertical, ProgressPercent(element)));
    }

    private static void ApplyColorFill(Panel fill, int width, int height, bool vertical, float percent)
    {
        int filledWidth = vertical ? width : (int)(width * percent);
        int filledHeight = vertical ? (int)(height * percent) : height;

        fill.Left = 0;
        fill.Top = vertical ? height - filledHeight : 0;
        fill.Width = filledWidth;
        fill.Height = filledHeight;
    }

    private void BuildImageProgressBar(XmlGumpElement element, Panel preview)
    {
        ushort bgGraphic = element.GetUShort("image_background");
        ushort fgGraphic = element.GetUShort("image_foreground");
        bool vertical = string.Equals(element.GetAttribute("direction"), "up", StringComparison.OrdinalIgnoreCase);

        TextureRegion? bg = GetRegion(bgGraphic, element.GetUShort("background_hue"), null);
        TextureRegion? fg = GetRegion(fgGraphic, element.GetUShort("foreground_hue"), null);

        int width = bg?.Bounds.Width ?? Math.Max(MIN_SIZE, element.GetInt("width", 100));
        int height = bg?.Bounds.Height ?? Math.Max(MIN_SIZE, element.GetInt("height", 12));

        preview.Width = width;
        preview.Height = height;

        preview.Widgets.Add(new Image { Width = width, Height = height, Renderable = bg, Background = bg == null ? new SolidBrush(new Color(180, 60, 60, 90)) : null });

        if (fg == null)
            return;

        var foreground = new Image();
        ApplyImageFill(foreground, fg, width, height, vertical, ProgressPercent(element));
        preview.Widgets.Add(foreground);

        if (element.GetBool("updates"))
            _liveUpdaters.Add(() => ApplyImageFill(foreground, fg, width, height, vertical, ProgressPercent(element)));
    }

    private static void ApplyImageFill(Image fill, TextureRegion source, int width, int height, bool vertical, float percent)
    {
        if (vertical)
        {
            int span = source.Bounds.Height;
            int filled = Math.Clamp((int)(span * percent), 1, span);
            var crop = new Rectangle(source.Bounds.X, source.Bounds.Y + (span - filled), source.Bounds.Width, filled);

            fill.Renderable = new TextureRegion(source.Texture, crop);
            fill.Left = 0;
            fill.Top = height - filled;
            fill.Width = width;
            fill.Height = filled;
        }
        else
        {
            int span = source.Bounds.Width;
            int filled = Math.Clamp((int)(span * percent), 1, span);
            var crop = new Rectangle(source.Bounds.X, source.Bounds.Y, filled, source.Bounds.Height);

            fill.Renderable = new TextureRegion(source.Texture, crop);
            fill.Left = 0;
            fill.Top = 0;
            fill.Width = filled;
            fill.Height = height;
        }
    }

    private void BuildHealthBarColor(XmlGumpElement element, Panel preview)
    {
        int width = Math.Max(MIN_SIZE, element.GetInt("width", 80));
        int height = Math.Max(MIN_SIZE, element.GetInt("height", 12));
        bool vertical = string.Equals(element.GetAttribute("direction"), "up", StringComparison.OrdinalIgnoreCase);

        preview.Width = width;
        preview.Height = height;
        preview.Widgets.Add(new Panel { Width = width, Height = height, Background = new SolidBrush(HueColor(element.GetUShort("hue_background"), 20)) });

        var foreground = new Panel { Background = new SolidBrush(HueColor(element.GetUShort("hue_normal", 97), 31)) };
        ApplyColorFill(foreground, width, height, vertical, PlayerHealthPercent());
        preview.Widgets.Add(foreground);

        // Health bars always track the player, so they refresh regardless of an updates flag.
        _liveUpdaters.Add(() => ApplyColorFill(foreground, width, height, vertical, PlayerHealthPercent()));
    }

    private void BuildHealthBarImage(XmlGumpElement element, Panel preview)
    {
        TextureRegion? bg = GetRegion(element.GetUShort("image_background"), element.GetUShort("hue_background"), null);
        TextureRegion? fg = GetRegion(element.GetUShort("image_foreground"), element.GetUShort("hue_normal", 97), null);
        bool vertical = string.Equals(element.GetAttribute("direction"), "up", StringComparison.OrdinalIgnoreCase);

        int width = bg?.Bounds.Width ?? Math.Max(MIN_SIZE, element.GetInt("width", 80));
        int height = bg?.Bounds.Height ?? Math.Max(MIN_SIZE, element.GetInt("height", 12));

        preview.Width = width;
        preview.Height = height;

        preview.Widgets.Add(new Image { Width = width, Height = height, Renderable = bg, Background = bg == null ? new SolidBrush(new Color(180, 60, 60, 90)) : null });

        if (fg == null)
            return;

        var foreground = new Image();
        ApplyImageFill(foreground, fg, width, height, vertical, PlayerHealthPercent());
        preview.Widgets.Add(foreground);

        // Health bars always track the player, so they refresh regardless of an updates flag.
        _liveUpdaters.Add(() => ApplyImageFill(foreground, fg, width, height, vertical, PlayerHealthPercent()));
    }

    private void BuildContainer(XmlGumpElement element, Panel preview, Panel parent)
    {
        int width = element.GetInt("width");
        int height = element.GetInt("height");

        foreach (XmlGumpElement child in element.Children)
            BuildPreview(child, preview);

        if (width > 0)
            preview.Width = width;

        if (height > 0)
            preview.Height = height;

        if (preview.Width == null || preview.Height == null)
        {
            int maxW = MIN_SIZE;
            int maxH = MIN_SIZE;

            foreach (ElementPreview child in _previews)
            {
                if (child.Parent != preview)
                    continue;

                maxW = Math.Max(maxW, child.Left + (child.Width ?? 0));
                maxH = Math.Max(maxH, child.Top + (child.Height ?? 0));
            }

            preview.Width ??= maxW;
            preview.Height ??= maxH;
        }

        preview.Widgets.Add(new Panel { Width = preview.Width, Height = preview.Height, Border = new SolidBrush(new Color(90, 90, 110, 90)), BorderThickness = new Thickness(1) });
    }

    #endregion

    #region Image helpers

    private TextureRegion? GetRegion(ushort graphic, ushort hue, Rectangle? source)
    {
        ref readonly SpriteInfo info = ref Client.Game.UO.Gumps.GetGump(graphic);

        if (info.Texture == null)
            return null;

        if (hue == 0)
        {
            Rectangle rect = source ?? info.UV;

            return new TextureRegion(info.Texture, rect);
        }

        Texture2D? baked = GetHueTexture(graphic, hue);

        if (baked == null)
            return new TextureRegion(info.Texture, source ?? info.UV);

        Rectangle bakedRect = source ?? baked.Bounds;

        return new TextureRegion(baked, bakedRect);
    }

    private Texture2D? GetHueTexture(ushort graphic, ushort hue)
    {
        uint key = ((uint)graphic << 16) | hue;

        if (_hueTextures.TryGetValue(key, out Texture2D? cached))
            return cached;

        uint[] pixels = Client.Game.UO.Gumps.GetHuedGumpPixels(graphic, hue, IsPartialHue(graphic), out Rectangle bounds);

        if (pixels.Length == 0)
        {
            _hueTextures[key] = null!;

            return null;
        }

        var texture = new Texture2D(Client.Game.GraphicsDevice, bounds.Width, bounds.Height, false, SurfaceFormat.Color);
        texture.SetData(pixels);
        _hueTextures[key] = texture;

        return texture;
    }

    private static bool IsPartialHue(ushort graphic)
    {
        StaticTiles[] staticData = Client.Game.UO.FileManager.TileData.StaticData;

        return graphic < staticData.Length && staticData[graphic].IsPartialHue;
    }

    private static Rectangle? ParseRect(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        string[] parts = value.Split(';');

        if (parts.Length != 4)
            return null;

        if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) &&
            int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h))
            return new Rectangle(x, y, w, h);

        return null;
    }

    #endregion

    #region Progress helpers

    private float ProgressPercent(XmlGumpElement element)
    {
        (int value, int max) = ResolveProgress(element);

        return Percentage(value, max);
    }

    private (int Value, int Max) ResolveProgress(XmlGumpElement element)
    {
        int max = ResolveNumber(element.GetAttribute("max_value"), 0);

        if (max <= 0)
            return (1, 1);

        return (Math.Clamp(ResolveNumber(element.GetAttribute("value"), 0), 0, max), max);
    }

    /// <summary>Reads a progress value, resolving a placeholder such as <c>{hp}</c> against the player.</summary>
    private int ResolveNumber(string? raw, int fallback)
    {
        if (string.IsNullOrEmpty(raw))
            return fallback;

        if (int.TryParse(raw, out int direct))
            return direct;

        return int.TryParse(XmlGumpHandler.FormatText(_world, raw), out int resolved) ? resolved : fallback;
    }

    private float PlayerHealthPercent()
    {
        Mobile? player = _world.Player;

        if (player == null || player.HitsMax <= 0)
            return 1f;

        return Math.Clamp((float)player.Hits / player.HitsMax, 0f, 1f);
    }

    private static float Percentage(int value, int max) => max <= 0 ? 1f : Math.Clamp((float)value / max, 0f, 1f);

    private static Color HueColor(ushort hue, ushort shade, float alpha = 1f)
    {
        float clamped = Math.Clamp(alpha, 0f, 1f);

        // Myra draws through premultiplied alpha, so the channels scale with opacity.
        if (hue == 0)
            return Color.FromNonPremultiplied(255, 255, 255, (int)(255 * clamped));

        var color = new Color { PackedValue = Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(shade, hue) };

        return Color.FromNonPremultiplied(color.R, color.G, color.B, (int)(255 * clamped));
    }

    #endregion
}

/// <summary>
/// The wrapper around one element's rendered preview. Owns its own selection outline and is the unit the
/// canvas hit-tests, selects and drags.
/// </summary>
internal sealed class ElementPreview : Panel
{
    public XmlGumpElement Element { get; }

    private bool _isSelected;

    public ElementPreview(XmlGumpElement element)
    {
        Element = element;

        // Anchored to the panel's top-left so the explicit size and the Left/Top offset are what
        // position the preview; the container default of Stretch would otherwise size it to the canvas.
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);

        if (_isSelected)
        {
            context.DrawRectangle(Bounds, new Color(80, 190, 255), 2);
            context.DrawRectangle(new Rectangle(Bounds.X - 3, Bounds.Y - 3, 8, 8), new Color(80, 190, 255));
        }
    }
}
