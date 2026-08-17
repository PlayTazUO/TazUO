#nullable enable

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json;

/// <summary>
///     Reads and writes <see cref="Color" /> as <c>#RRGGBB</c>, or <c>#RRGGBBAA</c> when not fully
///     opaque. Serializing the struct itself instead would emit R, G, B, A and
///     <see cref="Color.PackedValue" />, five members over one backing field, which only round-trips
///     correctly by accident of ordering.
/// </summary>
internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a colour string such as \"#RRGGBB\", got {reader.TokenType}.");

        ReadOnlySpan<char> text = (reader.GetString() ?? string.Empty).AsSpan().Trim();

        if (text.Length > 0 && text[0] == '#')
            text = text[1..];

        if (text.Length != 6 && text.Length != 8)
            throw new JsonException($"Colour \"{text}\" must be 6 or 8 hex digits.");

        if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgba))
            throw new JsonException($"Colour \"{text}\" is not valid hexadecimal.");

        if (text.Length == 6)
            rgba = (rgba << 8) | 0xFF;

        return new Color((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
        writer.WriteStringValue
        (
            value.A == byte.MaxValue
                ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
                : $"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}"
        );
}
