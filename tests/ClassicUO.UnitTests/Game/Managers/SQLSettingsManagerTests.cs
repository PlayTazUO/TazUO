using System.Globalization;
using System.Threading;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    // Exercises the typed round-trip of SQLSettingsManager, covering the integer widths and enum support
    // relied upon by the Profile [SqlSetting] migration. Uses Global scope so no logged-in profile is needed.
    public class SQLSettingsManagerTests
    {
        private enum SampleEnum
        {
            First = 0,
            Second = 1,
            Third = 7
        }

        [Fact]
        public void Scalar_And_Enum_Values_RoundTrip()
        {
            using var settings = new SQLSettingsManager();

            settings.Set(SettingsScope.Global, "t_bool", true);
            settings.Set(SettingsScope.Global, "t_byte", (byte)200);
            settings.Set(SettingsScope.Global, "t_sbyte", (sbyte)-120);
            settings.Set(SettingsScope.Global, "t_short", (short)-30000);
            settings.Set(SettingsScope.Global, "t_ushort", (ushort)60000);
            settings.Set(SettingsScope.Global, "t_int", -1234567);
            settings.Set(SettingsScope.Global, "t_uint", 4000000000u);
            settings.Set(SettingsScope.Global, "t_long", -9000000000L);
            settings.Set(SettingsScope.Global, "t_ulong", 18000000000000000000UL);
            settings.Set(SettingsScope.Global, "t_float", 1.5f);
            settings.Set(SettingsScope.Global, "t_double", 3.14159d);
            settings.Set(SettingsScope.Global, "t_string", "hello world");
            settings.Set(SettingsScope.Global, "t_enum", SampleEnum.Third);

            settings.Get<bool>(SettingsScope.Global, "t_bool").Should().BeTrue();
            settings.Get<byte>(SettingsScope.Global, "t_byte").Should().Be(200);
            settings.Get<sbyte>(SettingsScope.Global, "t_sbyte").Should().Be(-120);
            settings.Get<short>(SettingsScope.Global, "t_short").Should().Be(-30000);
            settings.Get<ushort>(SettingsScope.Global, "t_ushort").Should().Be(60000);
            settings.Get<int>(SettingsScope.Global, "t_int").Should().Be(-1234567);
            settings.Get<uint>(SettingsScope.Global, "t_uint").Should().Be(4000000000u);
            settings.Get<long>(SettingsScope.Global, "t_long").Should().Be(-9000000000L);
            settings.Get<ulong>(SettingsScope.Global, "t_ulong").Should().Be(18000000000000000000UL);
            settings.Get<float>(SettingsScope.Global, "t_float").Should().Be(1.5f);
            settings.Get<double>(SettingsScope.Global, "t_double").Should().Be(3.14159d);
            settings.Get<string>(SettingsScope.Global, "t_string").Should().Be("hello world");
            settings.Get<SampleEnum>(SettingsScope.Global, "t_enum").Should().Be(SampleEnum.Third);
        }

        [Fact]
        public void Float_And_Double_Are_Locale_Independent()
        {
            // A culture whose decimal separator is ',' (e.g. de-DE). Values must still store/parse with '.'
            // so they round-trip and so JSON-imported values ("0.9") parse correctly.
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                using var settings = new SQLSettingsManager();

                settings.Set(SettingsScope.Global, "loc_float", 0.9f);
                settings.Set(SettingsScope.Global, "loc_double", 3.14159d);

                // Stored form must be invariant ('.'), not the culture's ','.
                settings.Get(SettingsScope.Global, "loc_float").Should().Be("0.9");

                settings.Get<float>(SettingsScope.Global, "loc_float").Should().Be(0.9f);
                settings.Get<double>(SettingsScope.Global, "loc_double").Should().Be(3.14159d);

                // Simulate a value imported from profile.json (always '.'-decimal) read under this culture.
                settings.Set(SettingsScope.Global, "loc_json", "0.5");
                settings.Get<double>(SettingsScope.Global, "loc_json").Should().Be(0.5d);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void Missing_Value_Returns_Default()
        {
            using var settings = new SQLSettingsManager();

            settings.Get(SettingsScope.Global, "does_not_exist_key", 42).Should().Be(42);
            settings.Get(SettingsScope.Global, "does_not_exist_enum", SampleEnum.Second).Should().Be(SampleEnum.Second);
        }
    }
}
