#nullable enable

using System;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Myra.Events;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A label that reads and behaves as a link. Either opens a URL in the user's browser, or runs an
/// arbitrary action for navigation that stays inside the client - a breadcrumb segment, say.
/// <para>
/// Only a URL link marks itself visited. An in-app link goes somewhere that can be returned to, so
/// recolouring it permanently would be a lie.
/// </para>
/// </summary>
public class LinkLabel : MyraLabel
{
    #region Members

    private Action _activate = null!;
    private bool _marksVisited;

    #endregion

    #region Ctor

    /// <param name="text">The link text</param>
    /// <param name="link">The URL to open</param>
    /// <param name="fontSizeOffset">Font size offset from the UI font</param>
    public LinkLabel(string text, string link, int fontSizeOffset) : base(text, fontSizeOffset)
    {
        InitLink(link);
    }

    /// <param name="text">The link text</param>
    /// <param name="link">The URL to open</param>
    /// <param name="style">Text style</param>
    /// <param name="align">Text alignment</param>
    public LinkLabel(
        string text,
        string link,
        TextStyle style = TextStyle.P,
        AlignMode align = AlignMode.Left
    ) : base(text, style, align)
    {
        InitLink(link);
    }

    /// <param name="text">The link text</param>
    /// <param name="onActivate">Invoked when the link is clicked</param>
    /// <param name="style">Text style</param>
    /// <param name="align">Text alignment</param>
    public LinkLabel(
        string text,
        Action onActivate,
        TextStyle style = TextStyle.P,
        AlignMode align = AlignMode.Left
    ) : base(text, style, align)
    {
        ArgumentNullException.ThrowIfNull(onActivate);

        _activate = onActivate;
        _marksVisited = false;

        ApplyLinkColours();
    }

    #endregion

    #region Public Accessors

    /// <summary>Colour used before the link has been followed</summary>
    public Color LinkColor
    {
        get => field;
        set
        {
            field = value;
            TextColor = value;
        }
    } = Color.DodgerBlue;

    /// <summary>Colour used while hovered</summary>
    public Color HoverColor
    {
        get => field;
        set
        {
            field = value;
            OverTextColor = value;
        }
    } = Color.DeepSkyBlue;

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public override void OnTouchDown(TouchEventArgs args)
    {
        base.OnTouchDown(args);

        _activate();

        if (_marksVisited)
            TextColor = Color.MediumPurple;
    }

    #endregion

    #region Private Methods

    private void InitLink(string link)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(link);

        _activate = () => PlatformHelper.LaunchBrowser(link);
        _marksVisited = true;

        ApplyLinkColours();
    }

    private void ApplyLinkColours()
    {
        TextColor = LinkColor;
        OverTextColor = HoverColor;
    }

    #endregion
}
