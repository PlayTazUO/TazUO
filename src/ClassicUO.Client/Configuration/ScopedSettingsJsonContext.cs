using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Configuration
{
    [JsonSerializable(typeof(GlobalSettingsSave), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ServerSettingsSave), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(AccountSettingsSave), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal sealed partial class ScopedSettingsJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name) =>
                string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            };
            return options;
        });

        public static ScopedSettingsJsonContext DefaultToUse { get; } = new ScopedSettingsJsonContext(_jsonOptions.Value);
    }
}
