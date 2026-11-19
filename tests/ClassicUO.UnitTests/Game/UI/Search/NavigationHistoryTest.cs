using System;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class NavigationHistoryTest
    {
        [Fact]
        public void Starts_Empty_With_Nowhere_To_Go()
        {
            var history = new NavigationHistory<string>();

            history.IsEmpty.Should().BeTrue();
            history.Current.Should().BeNull();
            history.CanGoBack.Should().BeFalse();
            history.CanGoForward.Should().BeFalse();
        }

        [Fact]
        public void Ctor_Rejects_A_Non_Positive_Capacity()
        {
            Action create = () => new NavigationHistory<string>(0);

            create.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Push_Then_Back_And_Forward_Walk_The_List()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("b");
            history.Push("c");

            history.Current.Should().Be("c");

            history.TryGoBack(out string back).Should().BeTrue();
            back.Should().Be("b");

            history.TryGoForward(out string forward).Should().BeTrue();
            forward.Should().Be("c");

            history.CanGoForward.Should().BeFalse();
        }

        [Fact]
        public void Push_Ignores_A_Repeat_Of_The_Current_Entry()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("a");

            history.CanGoBack.Should().BeFalse();
        }

        [Fact]
        public void Push_Discards_The_Forward_Tail()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("b");
            history.TryGoBack(out _);

            history.Push("c");

            history.CanGoForward.Should().BeFalse();
            history.Current.Should().Be("c");

            history.TryGoBack(out string back).Should().BeTrue();
            back.Should().Be("a");
        }

        [Fact]
        public void ReplaceCurrent_Swaps_The_Entry_Without_Moving()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("b");

            history.ReplaceCurrent("b2").Should().BeTrue();

            history.Current.Should().Be("b2");
            history.CanGoForward.Should().BeFalse();

            history.TryGoBack(out string back).Should().BeTrue();
            back.Should().Be("a");
        }

        [Fact]
        public void ReplaceCurrent_Fails_On_An_Empty_History()
        {
            new NavigationHistory<string>().ReplaceCurrent("a").Should().BeFalse();
        }

        [Fact]
        public void Push_Past_Capacity_Drops_The_Oldest_Entries()
        {
            var history = new NavigationHistory<int>(3);

            for (int i = 0; i < 5; i++)
                history.Push(i);

            history.Current.Should().Be(4);

            history.TryGoBack(out _);
            history.TryGoBack(out int oldest);

            oldest.Should().Be(2);
            history.CanGoBack.Should().BeFalse();
        }

        [Fact]
        public void Clear_Empties_The_History()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("b");
            history.Clear();

            history.IsEmpty.Should().BeTrue();
            history.CanGoBack.Should().BeFalse();
            history.Current.Should().BeNull();
        }

        [Fact]
        public void Changed_Fires_For_Pushes_Moves_And_Clears_Only()
        {
            var history = new NavigationHistory<string>();
            int raised = 0;
            history.Changed += (_, _) => raised++;

            history.Push("a");
            history.Push("a");       // deduped, no event
            history.Push("b");
            history.TryGoBack(out _);
            history.TryGoForward(out _);
            history.TryGoForward(out _); // nowhere to go, no event
            history.Clear();
            history.Clear();         // already empty, no event

            raised.Should().Be(5);
        }

        [Fact]
        public void TruncateForward_Drops_The_Forward_Tail_Without_Moving_The_Cursor()
        {
            var history = new NavigationHistory<string>();

            history.Push("a");
            history.Push("b");
            history.Push("c");
            history.TryGoBack(out _);

            history.Current.Should().Be("b");
            history.CanGoForward.Should().BeTrue();

            history.TruncateForward();

            history.Current.Should().Be("b");
            history.CanGoForward.Should().BeFalse();
            history.CanGoBack.Should().BeTrue();
        }

        [Fact]
        public void TruncateForward_Is_A_Silent_No_Op_With_Nothing_Ahead()
        {
            var history = new NavigationHistory<string>();
            int raised = 0;

            history.Push("a");
            history.Changed += (_, _) => raised++;

            history.TruncateForward();

            raised.Should().Be(0);
            history.Current.Should().Be("a");
        }

        [Fact]
        public void Custom_Comparer_Drives_Deduplication()
        {
            var history = new NavigationHistory<string>(comparer: StringComparer.OrdinalIgnoreCase);

            history.Push("Video");
            history.Push("VIDEO");

            history.CanGoBack.Should().BeFalse();
        }
    }
}
