using System;

namespace ClassicUO.Common;

using System.Text.Json;
using System.Text.Json.Serialization;

public class RawStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        // This automatically handles standard \uXXXX escapes and turns them back to chars
        reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        // This bypasses the default escaping for this specific field
        writer.WriteStringValue(value);
}
