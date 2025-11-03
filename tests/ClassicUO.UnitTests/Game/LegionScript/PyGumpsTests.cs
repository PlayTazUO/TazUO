using ClassicUO.Game;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.PyClasses;
using FluentAssertions;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

public class PyGumpsTests
{
    public class CreateGump
    {
        [Fact]
        public void CreateGump_WithDefaultParameters_ReturnsNonNullPyBaseGump()
        {
            Client.UnitTestingActive = true;
            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result = api.Gumps.CreateGump();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<PyBaseGump>();
        }

        [Fact]
        public void CreateGump_WithDefaultParameters_CreatesGumpWithCorrectProperties()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result = api.Gumps.CreateGump();

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            gump.Should().NotBeNull();
            gump.AcceptMouseInput.Should().BeTrue();
            gump.CanMove.Should().BeTrue();
            gump.WantUpdateSize.Should().BeTrue();
        }

        [Fact]
        public void CreateGump_WithAcceptMouseInputFalse_CreatesGumpWithMouseInputDisabled()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result = api.Gumps.CreateGump(acceptMouseInput: false);

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            gump.AcceptMouseInput.Should().BeFalse();
            gump.CanMove.Should().BeTrue();
            gump.WantUpdateSize.Should().BeTrue();
        }

        [Fact]
        public void CreateGump_WithCanMoveFalse_CreatesGumpThatCannotMove()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result = api.Gumps.CreateGump(canMove: false);

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            gump.AcceptMouseInput.Should().BeTrue();
            gump.CanMove.Should().BeFalse();
            gump.WantUpdateSize.Should().BeTrue();
        }

        [Fact]
        public void CreateGump_WithKeepOpenFalse_AddsGumpToTrackedList()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);
            int initialCount = api.gumps.Count;

            // Act
            PyBaseGump result = api.Gumps.CreateGump(keepOpen: false);

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            api.gumps.Should().Contain(gump);
            api.gumps.Count.Should().Be(initialCount + 1);
        }

        [Fact]
        public void CreateGump_WithKeepOpenTrue_DoesNotAddGumpToTrackedList()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);
            int initialCount = api.gumps.Count;

            // Act
            PyBaseGump result = api.Gumps.CreateGump(keepOpen: true);

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            api.gumps.Should().NotContain(gump);
            api.gumps.Count.Should().Be(initialCount);
        }

        [Fact]
        public void CreateGump_WithAllCustomParameters_CreatesGumpWithCorrectConfiguration()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result = api.Gumps.CreateGump(
                acceptMouseInput: false,
                canMove: false,
                keepOpen: true
            );

            // Assert
            Gump gump = ((IPyGump)result).Gump;
            gump.Should().NotBeNull();
            gump.AcceptMouseInput.Should().BeFalse();
            gump.CanMove.Should().BeFalse();
            gump.WantUpdateSize.Should().BeTrue();
            api.gumps.Should().NotContain(gump);
        }

        [Fact]
        public void CreateGump_CalledMultipleTimes_ReturnsDistinctInstances()
        {
            Client.UnitTestingActive = true;

            // Arrange
            ScriptEngine engine = Python.CreateEngine();
            var api = new API(engine);

            // Act
            PyBaseGump result1 = api.Gumps.CreateGump();
            PyBaseGump result2 = api.Gumps.CreateGump();

            // Assert
            result1.Should().NotBeSameAs(result2);
            Gump gump1 = ((IPyGump)result1).Gump;
            Gump gump2 = ((IPyGump)result2).Gump;
            gump1.Should().NotBeSameAs(gump2);
        }
    }
}
