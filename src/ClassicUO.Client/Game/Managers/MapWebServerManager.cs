using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Utility.Logging;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    internal class MapWebServerManager
    {
        private static MapWebServerManager _instance;
        private MapWebServer _server;

        public static MapWebServerManager Instance => _instance ??= new MapWebServerManager();

        private MapWebServerManager()
        {
            _server = new MapWebServer();
        }

        public bool IsRunning => _server?.IsRunning ?? false;
        public int Port => _server?.Port ?? 8088;

        public async Task<bool> Start(int? port = null)
        {
            // Check if map texture exists first
            int mapIndex = World.Instance?.MapIndex ?? 0;
            Texture2D mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap(mapIndex);

            if (mapTexture == null || mapTexture.IsDisposed)
            {
                await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap(mapIndex);

                if (mapTexture == null || mapTexture.IsDisposed)
                {
                    Log.Error("Map texture not available - please open the world map first");
                    GameActions.Print(World.Instance, "Please open the world map first", 0x21);
                    return false;
                }
            }

            // Use profile setting if no port specified
            int serverPort = port ?? Configuration.ProfileManager.CurrentProfile?.WebMapServerPort ?? 8088;

            // Start server first
            bool started = _server?.Start(serverPort) ?? false;

            if (started)
            {
                // Generate PNG asynchronously to avoid blocking
                _ = GenerateMapPngAsync();
            }

            return started;
        }

        private async Task GenerateMapPngAsync()
        {
            try
            {
                int mapIndex = World.Instance?.MapIndex ?? 0;

                Log.Info($"Generating PNG for map {mapIndex}...");

                // Try to load the map texture for this map index
                await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                Texture2D mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap(mapIndex);

                Log.Info($"Loaded texture for map {mapIndex}: {(mapTexture != null ? $"{mapTexture.Width}x{mapTexture.Height}" : "null")}");

                // Retry if texture not loaded yet (may happen during map change)
                int retries = 0;
                while ((mapTexture == null || mapTexture.IsDisposed) && retries < 100)
                {
                    Log.Warn($"Map texture not ready yet, retrying in 1000ms... (attempt {retries + 1})");
                    await Task.Delay(1000);
                    await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                    mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap(mapIndex);
                    retries++;
                }

                if (mapTexture == null || mapTexture.IsDisposed)
                {
                    Log.Warn($"Map texture not available for map {mapIndex}. Open the world map gump to generate it.");
                    GameActions.Print(World.Instance, "Please open world map gump first", 0x21);
                    return;
                }

                Log.Info($"Converting map texture to PNG for map {mapIndex} ({mapTexture.Width}x{mapTexture.Height})...");
                var startTime = System.Diagnostics.Stopwatch.StartNew();

                // Extract texture data on the main thread (graphics operations must be on main thread).
                // After awaiting above, we may be on a thread pool thread, so we must dispatch back.
                int width = mapTexture.Width;
                int height = mapTexture.Height;
                byte[] textureData = new byte[width * height * 4]; // RGBA

                MainThreadQueue.BubblingInvokeOnMainThread(() =>
                {
                    lock (Map.Map.GetMapPngLock())
                    {
                        mapTexture.GetData(textureData);
                    }
                });

                // Offload the PNG encoding to a background thread
                byte[] pngData = await Task.Run(() =>
                {
                    using (var image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(textureData, width, height))
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        return ms.ToArray();
                    }
                });

                _server?.SetCachedMapPng(pngData, mapIndex);

                startTime.Stop();
                Log.Info($"PNG conversion took {startTime.ElapsedMilliseconds}ms, size: {pngData.Length / 1024}KB");
                GameActions.Print(World.Instance, "Map loaded in browser", 0x44);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to generate map PNG: {ex.Message}");
                GameActions.Print(World.Instance, "Failed to load map texture", 0x21);
            }
        }

        public void RegenerateMapPng()
        {
            // Clear old cached PNG first
            _server?.ClearCache();
            Log.Info("Map changed - regenerating PNG for web map");

            // Check if there's a WorldMapGump open - if so, it will handle loading the new map texture
            WorldMapGump worldMapGump = UIManager.GetGump<UI.Gumps.WorldMapGump>();
            if (worldMapGump != null)
            {
                Log.Info("WorldMapGump is open - waiting for it to load the new map before regenerating PNG");
                // Wait a bit for the WorldMapGump to finish loading the new map
                _ = Task.Delay(2000).ContinueWith(_ => GenerateMapPngAsync());
            }
            else
            {
                _ = GenerateMapPngAsync();
            }
        }

        public void Stop() => _server?.Stop();

        public void Dispose()
        {
            _server?.Dispose();
            _server = null;
        }
    }
}
