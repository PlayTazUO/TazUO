#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Assets;

namespace ClassicUO.Game.UI;

/// <summary>How the editor presents and parses a single XML gump attribute.</summary>
public enum XmlGumpAttributeKind
{
    Integer,
    UnsignedShort,
    Float,
    Boolean,
    Text,
    Enum,
    Hue,
    Graphic,
    Rect
}

/// <summary>
/// Describes one attribute the runtime parser reads for a tag: its name, how to edit it, and the value
/// a freshly added element starts with.
/// </summary>
/// <param name="Name">The XML attribute name, as the parser matches it.</param>
/// <param name="Kind">How to edit it.</param>
/// <param name="Display">English fallback for the localized caption.</param>
/// <param name="Default">Value a new element of the tag is created with.</param>
/// <param name="Options">The allowed values, for <see cref="XmlGumpAttributeKind.Enum"/>.</param>
public sealed record XmlGumpAttributeDefinition(
    string Name,
    XmlGumpAttributeKind Kind,
    string Display,
    string Default = "",
    string[]? Options = null
);

/// <summary>One XML tag the parser understands: its caption and the attributes it accepts.</summary>
/// <param name="Tag">The XML tag name.</param>
/// <param name="Display">English fallback for the localized name.</param>
/// <param name="Attributes">Attributes the tag accepts, in the order the editor lists them.</param>
public sealed record XmlGumpTagDefinition(string Tag, string Display, XmlGumpAttributeDefinition[] Attributes);

/// <summary>
/// The schema the visual editor is built from: what tags exist, what each accepts, and how to make an
/// empty one. Kept in step with <see cref="XmlGumpHandler"/> by hand - the dispatch there is a string
/// chain, so there is nothing to generate this from.
/// </summary>
public static class XmlGumpSchema
{
    private static readonly XmlGumpAttributeDefinition[] Universal =
    [
        new("x", XmlGumpAttributeKind.Integer, "X", "0"),
        new("y", XmlGumpAttributeKind.Integer, "Y", "0"),
        new("acceptmouseinput", XmlGumpAttributeKind.Boolean, "Accept mouse input", "true"),
        new("canmove", XmlGumpAttributeKind.Boolean, "Can move", "false")
    ];

    /// <summary>
    /// The width/height pair for tags that have no natural size and would otherwise be created at zero
    /// (and so invisible). Tags whose size comes from a graphic, or that size to their children, use
    /// empty defaults instead and are left out of the created element entirely.
    /// </summary>
    private static XmlGumpAttributeDefinition[] Sized(int width, int height) =>
    [
        new("width", XmlGumpAttributeKind.Integer, "Width", width.ToString()),
        new("height", XmlGumpAttributeKind.Integer, "Height", height.ToString())
    ];

    /// <summary>Every tag the editor can add, in palette order.</summary>
    public static readonly XmlGumpTagDefinition[] Tags =
    [
        new("text", "Text", [
            new("x", XmlGumpAttributeKind.Integer, "X", "0"),
            new("y", XmlGumpAttributeKind.Integer, "Y", "0"),
            new("font", XmlGumpAttributeKind.Text, "Font", TrueTypeLoader.EMBEDDED_FONT),
            new("size", XmlGumpAttributeKind.Integer, "Font size", "16"),
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "997"),
            new("updates", XmlGumpAttributeKind.Boolean, "Live updates", "false"),
            new("align", XmlGumpAttributeKind.Enum, "Alignment", "left", ["left", "center", "right"])
        ]),
        new("colorbox", "Color box", [
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "0"),
            new("alpha", XmlGumpAttributeKind.Float, "Alpha", "1"),
            .. Sized(20, 20),
            .. Universal
        ]),
        new("image", "Image", [
            new("id", XmlGumpAttributeKind.Graphic, "Graphic", "0"),
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "0"),
            new("rect", XmlGumpAttributeKind.Rect, "Source rect", ""),
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("height", XmlGumpAttributeKind.Integer, "Height", ""),
            .. Universal
        ]),
        new("image_progress_bar", "Image progress bar", [
            new("image_background", XmlGumpAttributeKind.Graphic, "Background graphic", "0"),
            new("image_foreground", XmlGumpAttributeKind.Graphic, "Foreground graphic", "0"),
            new("background_hue", XmlGumpAttributeKind.Hue, "Background hue", "0"),
            new("foreground_hue", XmlGumpAttributeKind.Hue, "Foreground hue", "0"),
            new("value", XmlGumpAttributeKind.Text, "Value", "0"),
            new("max_value", XmlGumpAttributeKind.Text, "Max value", "0"),
            new("updates", XmlGumpAttributeKind.Boolean, "Live updates", "false"),
            new("direction", XmlGumpAttributeKind.Enum, "Direction", "right", ["right", "up"]),
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("height", XmlGumpAttributeKind.Integer, "Height", ""),
            .. Universal
        ]),
        new("color_progress_bar", "Color progress bar", [
            new("background_hue", XmlGumpAttributeKind.Hue, "Background hue", "0"),
            new("foreground_hue", XmlGumpAttributeKind.Hue, "Foreground hue", "0"),
            new("value", XmlGumpAttributeKind.Text, "Value", "0"),
            new("max_value", XmlGumpAttributeKind.Text, "Max value", "0"),
            new("updates", XmlGumpAttributeKind.Boolean, "Live updates", "false"),
            new("direction", XmlGumpAttributeKind.Enum, "Direction", "right", ["right", "up"]),
            .. Sized(100, 12),
            .. Universal
        ]),
        new("control", "Container", [
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("height", XmlGumpAttributeKind.Integer, "Height", ""),
            .. Universal
        ]),
        new("simple_border", "Border", [
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "0"),
            .. Sized(100, 100),
            new("x", XmlGumpAttributeKind.Integer, "X", "0"),
            new("y", XmlGumpAttributeKind.Integer, "Y", "0"),
            new("acceptmouseinput", XmlGumpAttributeKind.Boolean, "Accept mouse input", "true"),
            new("canmove", XmlGumpAttributeKind.Boolean, "Can move", "false")
        ]),
        new("macro_button_color", "Macro button (color)", [
            new("macro", XmlGumpAttributeKind.Text, "Macro name", ""),
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "0"),
            new("hue_hover", XmlGumpAttributeKind.Hue, "Hover hue", "0"),
            new("alpha", XmlGumpAttributeKind.Float, "Alpha", "1"),
            .. Sized(32, 22),
            .. Universal
        ]),
        new("macro_button_graphic", "Macro button (graphic)", [
            new("macro", XmlGumpAttributeKind.Text, "Macro name", ""),
            new("id", XmlGumpAttributeKind.Graphic, "Graphic", "0"),
            new("id_hover", XmlGumpAttributeKind.Graphic, "Hover graphic", "0"),
            new("hue", XmlGumpAttributeKind.Hue, "Hue", "0"),
            new("hue_hover", XmlGumpAttributeKind.Hue, "Hover hue", "0"),
            new("alpha", XmlGumpAttributeKind.Float, "Alpha", "1"),
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("height", XmlGumpAttributeKind.Integer, "Height", ""),
            .. Universal
        ]),
        new("hp_bar_color", "Health bar (color)", [
            new("hue_normal", XmlGumpAttributeKind.Hue, "Normal hue", "97"),
            new("hue_background", XmlGumpAttributeKind.Hue, "Background hue", "0"),
            new("hue_poisoned", XmlGumpAttributeKind.Hue, "Poisoned hue", "62"),
            new("direction", XmlGumpAttributeKind.Enum, "Direction", "right", ["right", "up"]),
            .. Sized(80, 12),
            .. Universal
        ]),
        new("hp_bar_image", "Health bar (image)", [
            new("image_background", XmlGumpAttributeKind.Graphic, "Background graphic", "0"),
            new("image_foreground", XmlGumpAttributeKind.Graphic, "Foreground graphic", "0"),
            new("image_foreground_poisoned", XmlGumpAttributeKind.Graphic, "Poisoned graphic", "0"),
            new("hue_normal", XmlGumpAttributeKind.Hue, "Normal hue", "97"),
            new("hue_background", XmlGumpAttributeKind.Hue, "Background hue", "0"),
            new("hue_poisoned", XmlGumpAttributeKind.Hue, "Poisoned hue", "62"),
            new("direction", XmlGumpAttributeKind.Enum, "Direction", "right", ["right", "up"]),
            new("width", XmlGumpAttributeKind.Integer, "Width", ""),
            new("height", XmlGumpAttributeKind.Integer, "Height", ""),
            .. Universal
        ])
    ];

    private static readonly Dictionary<string, XmlGumpTagDefinition> ByTag = BuildIndex();

    private static Dictionary<string, XmlGumpTagDefinition> BuildIndex()
    {
        var index = new Dictionary<string, XmlGumpTagDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (XmlGumpTagDefinition tag in Tags)
            index[tag.Tag] = tag;

        return index;
    }

    public static XmlGumpTagDefinition? Find(string tag) => ByTag.GetValueOrDefault(tag);

    /// <summary>
    /// Builds an empty element of a tag, seeded with the schema's defaults.
    /// </summary>
    /// <remarks>
    /// Attributes whose default is empty are left out, so a new element carries only what it actually
    /// needs - an image, for instance, takes its size from its graphic rather than a written zero.
    /// </remarks>
    public static XmlGumpElement CreateElement(string tag)
    {
        var element = new XmlGumpElement(tag);
        XmlGumpTagDefinition? definition = Find(tag);

        if (definition != null)
            foreach (XmlGumpAttributeDefinition attribute in definition.Attributes)
                if (!string.IsNullOrEmpty(attribute.Default))
                    element.SetAttribute(attribute.Name, attribute.Default);

        return element;
    }

    public static string LabelKey(string tag) => $"xmlgumpeditor_tag_{tag}";
    public static string AttributeKey(string tag, string attribute) => $"xmlgumpeditor_attr_{tag}_{attribute}";
    public static string OptionKey(string value) => $"xmlgumpeditor_opt_{value}";
}
