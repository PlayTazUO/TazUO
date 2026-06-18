using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Utility.Logging;
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
        public bool IsMcpBridgeEnabled => _server?.IsMcpBridgeEnabled ?? false;

        public void SetMcpBridgeAuthToken(string authToken)
        {
            _server?.SetMcpBridgeAuthToken(authToken);
        }

        public void DisableMcpBridge()
        {
            _server?.SetMcpBridgeAuthToken(null);
        }

        public async Task<bool> Start(int? port = null, string mcpAuthToken = null)
        {
            if (!string.IsNullOrWhiteSpace(mcpAuthToken))
                _server?.SetMcpBridgeAuthToken(mcpAuthToken);

            // Check if map texture exists first
            int mapIndex = World.Instance?.MapIndex ?? 0;
            Texture2D mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap();

            if (mapTexture == null || mapTexture.IsDisposed)
            {
                await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap();

                if (mapTexture == null || mapTexture.IsDisposed)
                {
                    Log.Error("Map texture not available - please open world map first");
                    GameActions.Print(World.Instance, "Please open world map first", 0x21);

                    if (!string.IsNullOrWhiteSpace(mcpAuthToken))
                        _server?.SetMcpBridgeAuthToken(null);

                    return false;
                }
            }

            // Use profile setting if no port specified
            int serverPort = port ?? Configuration.ProfileManager.CurrentProfile?.WebMapServerPort ?? 8088;

            // Start server first
            bool started = _server?.Start(serverPort) ?? false;
            bool running = started || IsRunning;
            if (!running && !string.IsNullOrWhiteSpace(mcpAuthToken))
                _server?.SetMcpBridgeAuthToken(null);

            if (running)
            {
                // Generate PNG asynchronously to avoid blocking server startup.
                _ = GenerateMapPngAsync();
            }

            return running;
        }

        private async Task GenerateMapPngAsync()
        {
            try
            {
                int mapIndex = World.Instance?.MapIndex ?? 0;

                Log.Info($"Generating PNG for map {mapIndex}...");

                // Ensure the map PNG file is generated on disk
                await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                string pngFilePath = UI.Gumps.WorldMapGump.GetMapPngPath();

                // Retry if the PNG file isn't ready yet (may happen during map change)
                int retries = 0;
                while (string.IsNullOrEmpty(pngFilePath) || !File.Exists(pngFilePath))
                {
                    if (retries >= 100)
                    {
                        Log.Warn($"Map PNG not available for map {mapIndex}. Open the world map gump to generate it.");
                        GameActions.Print(World.Instance, "Please open world map gump first", 0x21);
                        return;
                    }
                    Log.Warn($"Map PNG not ready yet, retrying in 1000ms... (attempt {retries + 1})");
                    await Task.Delay(1000);
                    await UI.Gumps.WorldMapGump.LoadMapTextureForMap(mapIndex);
                    pngFilePath = UI.Gumps.WorldMapGump.GetMapPngPath();
                    retries++;
                }

                Log.Info($"Reading map PNG for map {mapIndex} from {pngFilePath}...");
                var startTime = System.Diagnostics.Stopwatch.StartNew();

                // Read the already-generated PNG file directly — no GPU operations needed
                byte[] pngData = await Task.Run(() => File.ReadAllBytes(pngFilePath));

                _server?.SetCachedMapPng(pngData, mapIndex);

                startTime.Stop();
                Log.Info($"PNG read took {startTime.ElapsedMilliseconds}ms, size: {pngData.Length / 1024}KB");
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
