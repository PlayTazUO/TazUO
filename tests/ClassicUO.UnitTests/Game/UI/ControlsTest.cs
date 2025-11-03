using ClassicUO.Game.UI.Controls;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class ControlsTest
{
    public class Dispose
    {
        [Fact]
        public void CleanUpDisposedChildren()
        {
            Control main = new Area();

            for (int i = 0; i < 10; i++)
                main.Add(new Area());

            foreach(Control child in main.Children)
                child.Dispose();

            main.CleanUpDisposedChildren();

            Assert.Empty(main.Children);
        }
    }
}
