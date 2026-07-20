using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json;

/// <summary>
/// Shared read/write helpers for the <see cref="Point"/> JSON converters so the
/// <c>{ "X": n, "Y": n }</c> token walk lives in a single place.
/// </summary>
internal static class PointJsonHelper
{
    /// <summary>
    /// Reads a <see cref="Point"/> from <c>{ "X": n, "Y": n }</c>.
    /// Returns <c>false</c> (with <paramref name="point"/> set to <see cref="Point.Zero"/>)
    /// if the token stream is malformed or incomplete.
    /// </summary>
    public static bool TryReadPoint(ref Utf8JsonReader reader, out Point point)
    {
        point = Point.Zero;

        if (reader.TokenType != JsonTokenType.StartObject)
            return false;

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return false;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return false;

        int x = reader.GetInt32();

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            return false;

        reader.Read();

        if (reader.TokenType != JsonTokenType.Number)
            return false;

        int y = reader.GetInt32();

        reader.Read();

        if (reader.TokenType != JsonTokenType.EndObject)
            return false;

        point = new Point(x, y);
        return true;
    }

    /// <summary>
    /// Writes a point as <c>{ "X": n, "Y": n }</c>.
    /// </summary>
    public static void WritePoint(Utf8JsonWriter writer, int x, int y)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", x);
        writer.WriteNumber("Y", y);
        writer.WriteEndObject();
    }
}
