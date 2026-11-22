using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    public class ExternalUrlImage : Control
    {
        private Texture2D _texture;
        private bool _loading;
        private Vector3 _hue;
        private static Dictionary<string, Texture2D> _cache = new();

        /// <summary>
        /// Creates an image control that loads its content in the background.
        /// </summary>
        /// <param name="url">
        /// Absolute URL of the image to download, or an inline data URI (<c>data:image/png;base64,...</c>).
        /// </param>
        /// <param name="width">Displayed width; the control keeps this size while loading.</param>
        /// <param name="height">Displayed height; the control keeps this size while loading.</param>
        public ExternalUrlImage(string url, int width = 100, int height = 100)
        {
            Width = width;
            Height = height;
            _hue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            LoadImageFromUrl(url);
        }

        /// <summary>
        /// Replaces the displayed image, loading it in the background.
        /// </summary>
        /// <param name="url">
        /// Absolute URL of the image to download, or an inline data URI (<c>data:image/png;base64,...</c>).
        /// </param>
        public void LoadImageFromUrl(string url)
        {
            _loading = true;

            Task.Run(() => LoadImage(url));
        }

        private void LoadImage(string url)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(url, out Texture2D cached) && cached != null)
                {
                    MainThreadQueue.InvokeOnMainThread(() =>
                        {
                            _texture = cached;
                            _loading = false;
                        }
                    );

                    return;
                }
            }

            byte[] data;

            try
            {
                // Data URIs carry their own bytes, so there is nothing to download.
                if (!DataUri.TryDecode(url, out data))
                {
                    using HttpClient client = new();
                    data = client.GetByteArrayAsync(new Uri(url)).Result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from URL: {ex}");

                MainThreadQueue.InvokeOnMainThread(() => _loading = false);

                return;
            }

            // The download stays on this worker thread so a slow URL never stalls the game loop, but the GPU
            // texture has to be created on the main thread.
            MainThreadQueue.InvokeOnMainThread(() =>
                {
                    try
                    {
                        using var ms = new MemoryStream(data);
                        var texture = Texture2D.FromStream(Client.Game.GraphicsDevice, ms);

                        lock (_cache)
                        {
                            _cache[url] = texture;
                        }

                        _texture = texture;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to decode image: {ex}");
                    }
                    finally
                    {
                        _loading = false;
                    }
                }
            );
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_texture != null && !_loading)
            {
                batcher.Draw(_texture, new Rectangle(x, y, Width, Height), _texture.Bounds, _hue);
            }

            return true;
        }
    }
}
