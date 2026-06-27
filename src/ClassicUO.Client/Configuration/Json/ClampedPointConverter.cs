using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json;

/// <summary>
/// Apply to a <see cref="Point"/> property or field to clamp its X and Y values during JSON serialization.
/// Deserialization is unaffected — clamping only occurs on write.
/// </summary>
/// <param name="minX">Minimum allowed X value. Default: -50.</param>
/// <param name="minY">Minimum allowed Y value. Default: -50.</param>
/// <param name="maxX">Maximum allowed X value. Default: <see cref="int.MaxValue"/>.</param>
/// <param name="maxY">Maximum allowed Y value. Default: <see cref="int.MaxValue"/>.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class ClampedPointConverterAttribute(int minX = -50, int minY = -50, int maxX = int.MaxValue, int maxY = int.MaxValue)
    : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert) => new ClampedPointConverter(minX, minY, maxX, maxY);
}

/// <summary>
/// JSON converter for <see cref="Point"/> that clamps X and Y to a configurable range on write.
/// Reads standard <c>{ "X": n, "Y": n }</c> objects; returns <see cref="Point.Zero"/> on malformed input.
/// </summary>
internal class ClampedPointConverter : JsonConverter<Point>
{
    private readonly int _minX;
    private readonly int _minY;
    private readonly int _maxX = int.MaxValue;
    private readonly int _maxY = int.MaxValue;

    public ClampedPointConverter()
    {
    }

    /// <param name="minX">Minimum X written to JSON. Default: -50.</param>
    /// <param name="minY">Minimum Y written to JSON. Default: -50.</param>
    /// <param name="maxX">Maximum X written to JSON. Default: <see cref="int.MaxValue"/>.</param>
    /// <param name="maxY">Maximum Y written to JSON. Default: <see cref="int.MaxValue"/>.</param>
    public ClampedPointConverter(int minX = -50, int minY = -50, int maxX = int.MaxValue, int maxY = int.MaxValue)
    {
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
    }

    /// <summary>
    /// Reads a <see cref="Point"/> from <c>{ "X": n, "Y": n }</c>.
    /// Returns <see cref="Point.Zero"/> if the token stream is malformed or incomplete.
    /// </summary>
    public sealed override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return Point.Zero;

        var point = new Point { X = reader.GetInt32() };

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return Point.Zero;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return Point.Zero;

        point.Y = reader.GetInt32();

        reader.Read();

        return reader.TokenType != JsonTokenType.EndObject
            ? Point.Zero
            : point;
    }

    /// <summary>
    /// Writes <paramref name="value"/> as <c>{ "X": n, "Y": n }</c>, clamping each component to the configured range.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", Math.Clamp(value.X, _minX, _maxX));
        writer.WriteNumber("Y", Math.Clamp(value.Y, _minY, _maxY));
        writer.WriteEndObject();
    }
}
