using FluentAssertions;
using StbTextEditSharp;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StbTextedit
{
    public class UndoStaleRecordTests
    {
        // Minimal string-backed handler whose text can be replaced outside the undo system.
        private sealed class TestHandler : ITextEditHandler
        {
            public string Text { get; set; } = string.Empty;

            public int Length => Text?.Length ?? 0;

            public TextEditRow LayoutRow(int startIndex) => new TextEditRow
            {
                num_chars = Length - startIndex
            };

            public float GetWidth(int index) => 1f;
        }

        [Fact]
        public void Undo_Does_Not_Throw_When_Text_Replaced_Externally()
        {
            var handler = new TestHandler();
            var edit = new TextEdit(handler) { SingleLine = true };

            // Type text so undo records are created.
            foreach (char c in "hello world")
            {
                edit.InputChar(c);
            }

            // Replace the buffer with a shorter string, leaving undo records pointing past the end.
            handler.Text = "hi";

            edit.Key(ControlKeys.Undo); // Must not throw IndexOutOfRangeException.
            edit.Key(ControlKeys.Undo); // History discarded, so this is a safe no-op.

            handler.Text.Should().Be("hi");
        }

        [Fact]
        public void Redo_Does_Not_Throw_When_Text_Replaced_Externally()
        {
            var handler = new TestHandler();
            var edit = new TextEdit(handler) { SingleLine = true };

            foreach (char c in "hello world")
            {
                edit.InputChar(c);
            }

            edit.Key(ControlKeys.Undo); // Create a redo record while the buffer is still valid.

            handler.Text = "hi"; // Corrupt the buffer, then attempt a redo.
            edit.Key(ControlKeys.Redo);

            handler.Text.Should().Be("hi");
        }

        [Fact]
        public void Undo_Still_Works_For_Valid_History()
        {
            var handler = new TestHandler();
            var edit = new TextEdit(handler) { SingleLine = true };

            foreach (char c in "abc")
            {
                edit.InputChar(c);
            }

            handler.Text.Should().Be("abc");

            edit.Key(ControlKeys.Undo); // Undoes the most recent single-character insert.

            handler.Text.Should().Be("ab");
        }
    }
}
