using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json
{
    sealed class Point2Converter : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            PointJsonHelper.TryReadPoint(ref reader, out Point point);
            return point;
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            PointJsonHelper.WritePoint(writer, value.X, value.Y);
        }
    }

    sealed class NullablePoint2Converter : JsonConverter<Point?>
    {
        public override Point? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            PointJsonHelper.TryReadPoint(ref reader, out Point point);
            return point;
        }

        public override void Write(Utf8JsonWriter writer, Point? value, JsonSerializerOptions options)
        {
            PointJsonHelper.WritePoint(writer, value.Value.X, value.Value.Y);
        }
    }
}
