using System;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.UI.Gumps;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    /// <summary>
    /// Guards the serial check on <see cref="ClassicUO.Game.Managers.TargetManager.LastAttack" />.
    /// <para>
    /// Without it, <see cref="World.Clear" /> - which zeroes the last attack on logout and character
    /// switch - opened a health bar for serial 0, a mobile that cannot exist. That also dragged the
    /// process-wide <c>UIManager</c> gump registry into any test that cleared a world, and its
    /// dictionary is not thread-safe, so the suite corrupted it whenever two such tests overlapped.
    /// </para>
    /// </summary>
    public class LastAttackHealthBarTest : IDisposable
    {
        private readonly Profile _previousProfile = ProfileManager.CurrentProfile;
        private readonly BaseHealthBarGump _previousBar = BaseHealthBarGump.LastAttackBar;

        public LastAttackHealthBarTest()
        {
            Client.UnitTestingActive = true;

            SetCurrentProfile(new Profile { OpenHealthBarForLastAttack = true, UseOneHPBarForLastAttack = true });
            BaseHealthBarGump.LastAttackBar = null;
        }

        [Fact]
        public void ClearingTheLastAttackOpensNoHealthBar()
        {
            var world = new World();

            world.TargetManager.LastAttack = 0;

            BaseHealthBarGump.LastAttackBar.Should().BeNull("serial 0 names no mobile to show a bar for");
        }

        [Fact]
        public void ClearingTheWorldOpensNoHealthBar()
        {
            var world = new World();

            world.Clear();

            BaseHealthBarGump.LastAttackBar.Should().BeNull();
        }

        public void Dispose()
        {
            BaseHealthBarGump.LastAttackBar = _previousBar;
            SetCurrentProfile(_previousProfile);

            GC.SuppressFinalize(this);
        }

        /// <summary>The setter is private, so the profile is planted the same way
        /// <see cref="ToolTipOverrideTest" /> plants one.</summary>
        private static void SetCurrentProfile(Profile profile)
        {
            PropertyInfo prop = typeof(ProfileManager).GetProperty(
                nameof(ProfileManager.CurrentProfile),
                BindingFlags.Public | BindingFlags.Static);

            prop.SetValue(null, profile);
        }
    }
}
