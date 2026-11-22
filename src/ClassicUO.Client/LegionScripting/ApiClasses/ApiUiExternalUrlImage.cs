using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Wraps an <see cref="ExternalUrlImage"/> control, which downloads an image from a URL — or decodes an inline
/// data URI — in the background and displays it once available. The content is cached per source string, so the
/// same image is only fetched and uploaded to the GPU once. The control keeps its configured size whether or not
/// the image has finished loading.
/// </summary>
public class ApiUiExternalUrlImage(ExternalUrlImage control) : ApiUiBaseControl(control)
{
    /// <summary>
    /// Replaces the displayed image with the image downloaded from <paramref name="url"/>, or decoded from the
    /// inline data URI in <paramref name="url"/>.
    /// </summary>
    /// <param name="url">
    /// Absolute URL of the image to download, or an inline data URI (<c>data:image/png;base64,...</c>).
    /// </param>
    public void LoadImageFromUrl(string url) => SetProp(() => control.LoadImageFromUrl(url));
}
