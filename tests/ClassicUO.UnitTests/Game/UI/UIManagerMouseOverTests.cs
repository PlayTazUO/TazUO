using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.UnitTests.Fixtures;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

[Collection(MainThreadCollection.Name)]
public class UIManagerMouseOverTests
{
    private sealed class HoverCounterControl : Area
    {
        public int MouseOverCount;

        public override void OnMouseOver(int x, int y)
        {
            MouseOverCount++;
            base.OnMouseOver(x, y);
        }
    }

    private static HoverCounterControl CreateHoverable()
    {
        HoverCounterControl c = new()
        {
            IsEnabled = true,
            X = 0,
            Y = 0,
            Width = 100,
            Height = 100
        };
        return c;
    }

    [Fact]
    public void StationaryCursor_StillDispatchesOnMouseOver()
    {
        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            UIManager.Clear();
            HoverCounterControl control = CreateHoverable();
            UIManager.Add(control);
            Mouse.Position = new Point(50, 50);

            UIManager.HandleMouseInput();
            Assert.Same(control, UIManager.MouseOverControl);
            Assert.Equal(1, control.MouseOverCount);

            // The cursor does not move: the expensive hit-test is skipped but the per-frame
            // hover dispatch must still reach the control under the cursor.
            UIManager.HandleMouseInput();
            Assert.Same(control, UIManager.MouseOverControl);
            Assert.Equal(2, control.MouseOverCount);
        });
    }

    [Fact]
    public void DisposedControl_IsNotKeptCached()
    {
        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            UIManager.Clear();
            HoverCounterControl control = CreateHoverable();
            UIManager.Add(control);
            Mouse.Position = new Point(50, 50);

            UIManager.HandleMouseInput();
            Assert.Same(control, UIManager.MouseOverControl);

            // Dispose while the cursor stays still and no structural change is signalled: the
            // cached hit-test result must not point at the removed control.
            control.Dispose();
            UIManager.HandleMouseInput();
            Assert.Null(UIManager.MouseOverControl);
        });
    }

    [Fact]
    public void Clear_DoesNotLeaveControlCached()
    {
        MainThreadQueue.BubblingInvokeOnMainThread(() =>
        {
            UIManager.Clear();
            HoverCounterControl control = CreateHoverable();
            UIManager.Add(control);
            Mouse.Position = new Point(50, 50);

            UIManager.HandleMouseInput();
            Assert.Same(control, UIManager.MouseOverControl);

            UIManager.Clear();
            UIManager.HandleMouseInput();
            Assert.Null(UIManager.MouseOverControl);
        });
    }
}
