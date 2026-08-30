using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.UnitTests.Game.LegionScript;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class ControlsTest
{
    [Collection(MainThreadCollection.Name)]
    public class Dispose
    {
        [Fact]
        public void CleanUpDisposedChildren()
        {
            Control main = MainThreadQueue.BubblingInvokeOnMainThread(() =>
            {
                Control m = new Area();

                for (int i = 0; i < 10; i++)
                    m.Add(new Area());

                foreach (Control child in m.Children)
                    child.Dispose();

                m.CleanUpDisposedChildren();

                return m;
            });

            Assert.Empty(main.Children);
        }
    }
}
