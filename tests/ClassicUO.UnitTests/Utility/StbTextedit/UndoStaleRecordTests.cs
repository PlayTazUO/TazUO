using FluentAssertions;
using StbTextEditSharp;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StbTextedit
{
    public class UndoStaleRecordTests
    {
        // Minimal handler backed by a plain string. It mimics a text box that can
        // have its text replaced from outside the undo system.
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

            // Type some text through the edit so undo records are created.
            foreach (char c in "hello world")
            {
                edit.InputChar(c);
            }

            // Replace the buffer with a much shorter string, bypassing the undo
            // system. The stored undo records now reference positions past the end
            // of the current text - the exact condition that used to crash.
            handler.Text = "hi";

            // Should not throw IndexOutOfRangeException.
            edit.Key(ControlKeys.Undo);

            // History is discarded, so a second undo is also safe and a no-op.
            edit.Key(ControlKeys.Undo);

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

            // Create a redo record by undoing once while the buffer is still valid.
            edit.Key(ControlKeys.Undo);

            // Now corrupt the buffer and attempt a redo.
            handler.Text = "hi";

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

            edit.Key(ControlKeys.Undo);

            // The most recent single-character insert should have been undone.
            handler.Text.Should().Be("ab");
        }
    }
}
