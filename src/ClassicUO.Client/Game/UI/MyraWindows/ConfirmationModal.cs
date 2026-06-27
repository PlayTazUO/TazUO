using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// A modal dialog that asks the user to confirm or cancel an action. The result is delivered
/// via a callback: <see langword="true"/> when the user confirms, <see langword="false"/> when
/// they cancel. The window has no close button and cannot be minimized or resized.
/// </summary>
public class ConfirmationModal : MyraControl
{
    private readonly Action<bool> _onClose;
    private readonly string _confirmButtonLabel;
    private readonly string _cancelButtonLabel;

    /// <summary>
    /// Initializes a new <see cref="ConfirmationModal"/> with a plain-text question.
    /// </summary>
    /// <param name="title">Window title bar text.</param>
    /// <param name="question">
    /// Question displayed as the modal body. Defaults to "Are you sure you wish to continue?"
    /// when <see langword="null"/>.
    /// </param>
    /// <param name="onClose">
    /// Callback invoked when the dialog closes. Receives <see langword="true"/> on confirm,
    /// <see langword="false"/> on cancel.
    /// </param>
    /// <param name="confirmButtonLabel">Label for the confirm button; defaults to "Confirm".</param>
    /// <param name="cancelButtonLabel">Label for the cancel button; defaults to "Cancel".</param>
    public ConfirmationModal(
        string title,
        string question,
        Action<bool> onClose,
        string confirmButtonLabel = null,
        string cancelButtonLabel = null
    ) : this(
        title,
        GetContentByText(question),
        onClose,
        confirmButtonLabel,
        cancelButtonLabel
    )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ConfirmationModal"/> with an arbitrary widget as the body.
    /// </summary>
    /// <param name="title">Window title bar text; defaults to "Confirm Action" when <see langword="null"/>.</param>
    /// <param name="modalContent">Widget rendered as the dialog body above the button row.</param>
    /// <param name="onClose">
    /// Callback invoked when the dialog closes. Receives <see langword="true"/> on confirm,
    /// <see langword="false"/> on cancel.
    /// </param>
    /// <param name="confirmButtonLabel">Label for the confirm button; defaults to "Confirm".</param>
    /// <param name="cancelButtonLabel">Label for the cancel button; defaults to "Cancel".</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onClose"/> is <see langword="null"/>.</exception>
    public ConfirmationModal(
        string title,
        Widget modalContent,
        Action<bool> onClose,
        string confirmButtonLabel = null,
        string cancelButtonLabel = null
    ) : base(title ?? "Confirm Action")
    {
        ArgumentNullException.ThrowIfNull(onClose);
        _onClose = onClose;
        _confirmButtonLabel = confirmButtonLabel;
        _cancelButtonLabel = cancelButtonLabel;

        ConfigureRootWindow();

        _rootWindow.Content = GetContent(modalContent);
    }

    /// <summary>
    /// Applies modal-specific window settings: centered title, fixed minimum width, hidden close
    /// button, and disabled minimize/resize.
    /// </summary>
    private void ConfigureRootWindow()
    {
        _rootWindow.TitlePanel.HorizontalAlignment = HorizontalAlignment.Center;
        _rootWindow.TitlePanel.VerticalAlignment = VerticalAlignment.Center;
        _rootWindow.TitlePanel.MinWidth = 300;
        _rootWindow.TitleLabelAlignment = HorizontalAlignment.Center;

        _rootWindow.CloseButton.Visible = false;
        _rootWindow.Props.Minimizable = false;
        _rootWindow.Props.Resize.Enabled = false;
    }

    /// <summary>
    /// Assembles the full dialog content: the caller-supplied body widget stacked above the
    /// confirm/cancel button row.
    /// </summary>
    private VerticalStackPanel GetContent(Widget modalContent)
    {
        var content = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Widgets.Add(modalContent);
        content.Widgets.Add(GetButtonGrid());
        return content;
    }

    /// <summary>
    /// Builds a three-column grid containing the cancel button (left), a flex spacer (center),
    /// and the confirm button (right, styled with a red accent).
    /// </summary>
    private Grid GetButtonGrid()
    {
        var buttonGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };

        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var closeButton = new MyraButton(_cancelButtonLabel ?? "Cancel", () =>
        {
            _rootWindow.Close();
            _onClose(false);
        });

        buttonGrid.Widgets.Add(closeButton);
        Grid.SetColumn(closeButton, 0);

        var spacer = new MyraSpacer(1, 4);
        buttonGrid.Widgets.Add(spacer);
        Grid.SetColumn(spacer, 1);

        var confirmButton = new MyraButton(_confirmButtonLabel ?? "Confirm", () =>
        {
            _rootWindow.Close();
            _onClose(true);
        })
        {
            BorderThickness = new Thickness(0, 0, 0, 3),
            Border = new SolidBrush(new Color(185, 20, 60, 120)),
            Background = new SolidBrush(new Color(220, 20, 60, 150)),
            OverBackground = new SolidBrush(new Color(240, 20, 60, 50))
        };

        buttonGrid.Widgets.Add(confirmButton);
        Grid.SetColumn(confirmButton, 2);

        return buttonGrid;
    }

    /// <summary>
    /// Creates a centered <see cref="MyraLabel"/> from a plain-text question string, used when
    /// the text-only constructor overload is called.
    /// </summary>
    /// <param name="question">Question text; defaults to "Are you sure you wish to continue?".</param>
    private static MyraLabel GetContentByText(string question) =>
        new(question ?? "Are you sure you wish to continue?", MyraLabel.TextStyle.H2)
        {
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(2, 4, 2, 4)
        };
}
