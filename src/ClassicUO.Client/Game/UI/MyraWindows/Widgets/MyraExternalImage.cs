#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A Myra Image widget that asynchronously downloads an image from a URL and displays it once available.
/// The download happens off the UI thread; the GPU texture is created on the main thread (GraphicsDevice
/// access is not thread-safe). Decoded textures are cached by URL for the process lifetime so the same
/// image is only ever fetched and uploaded once — matching the behaviour of <c>ExternalUrlImage</c>.
/// </summary>
public class MyraExternalImage : Image
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly Dictionary<string, Texture2D?> _cache = new();

    private readonly int _maxSize;

    public MyraExternalImage(string url, int maxSize = 320)
    {
        _maxSize = maxSize;
        MaxWidth = maxSize;
        MaxHeight = maxSize;

        LoadAsync(url);
    }

    private void LoadAsync(string url)
    {
        Task.Run(async () =>
        {
            Texture2D? texture = await GetOrDownloadAsync(url);

            if (texture == null)
                return;

            MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (!IsPlaced)
                    return;

                // Scale the image down to fit within maxSize while preserving its aspect ratio; the Image
                // widget stretches its renderable to the arranged bounds, so we size the widget ourselves.
                float scale = Math.Min(1f, _maxSize / (float)Math.Max(texture.Width, texture.Height));
                Width = Math.Max(1, (int)(texture.Width * scale));
                Height = Math.Max(1, (int)(texture.Height * scale));

                Renderable = new TextureRegion(texture);
            });
        });
    }

    private static async Task<Texture2D?> GetOrDownloadAsync(string url)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(url, out Texture2D? cached))
                return cached;
        }

        byte[]? data = null;
        try
        {
            data = await _httpClient.GetByteArrayAsync(url);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to download poll image '{url}': {e}");
        }

        // Creating the GPU texture must happen on the main thread; cache the result (including a null
        // failure) so we don't repeatedly retry a broken URL.
        Texture2D? texture = data == null
            ? null
            : MainThreadQueue.InvokeOnMainThread(() =>
            {
                try
                {
                    using var ms = new MemoryStream(data);
                    return Texture2D.FromStream(Client.Game.GraphicsDevice, ms);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to decode poll image '{url}': {e}");
                    return null;
                }
            });

        lock (_cache)
        {
            _cache[url] = texture;
        }

        return texture;
    }
}
