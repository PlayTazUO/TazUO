using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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

        public ExternalUrlImage(string url, int width = 100, int height = 100)
        {
            Width = width;
            Height = height;
            _hue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            LoadImageFromUrl(url);
        }

        public void LoadImageFromUrl(string url)
        {
            _loading = true;

            Task.Run(() => LoadImage(url));
        }

        private void LoadImage(string url)
        {
            if (_cache.ContainsKey(url))
            {
                _texture = _cache[url];

                if (_texture != null)
                {
                    _loading = false;

                    return;
                }
            }

            try
            {
                using HttpClient client = new();
                byte[] data = client.GetByteArrayAsync(new Uri(url)).Result;

                using var ms = new MemoryStream(data);
                var texture = Texture2D.FromStream(Client.Game.GraphicsDevice, ms);

                if (!_cache.ContainsKey(url))
                    _cache.Add(url, texture);

                _texture = texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from URL: {ex}");
            }
            finally
            {
                _loading = false;
            }
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
