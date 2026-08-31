using ClassicUO.Game.UI.Controls;
using ClassicUO.UnitTests.Fixtures;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class ControlsTest
{
    /// <summary>
    /// <see cref="Control.Dispose" /> only does its work on the main thread and defers otherwise, so
    /// these run through the fixture rather than on xUnit's thread - off it, the assert would race the
    /// disposal it is checking for.
    /// </summary>
    [Collection(MainThreadCollection.Name)]
    public class Dispose
    {
        private readonly MainThreadFixture _mainThread;

        public Dispose(MainThreadFixture mainThread) => _mainThread = mainThread;

        [Fact]
        public void CleanUpDisposedChildren()
        {
            _mainThread.Invoke(() =>
            {
                Control main = new Area();

                for (int i = 0; i < 10; i++)
                    main.Add(new Area());

                foreach (Control child in main.Children)
                    child.Dispose();

                main.CleanUpDisposedChildren();

                Assert.Empty(main.Children);
            });
        }
    }
}
