using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal class MapWebServer : IDisposable
    {
        private class ClientState
        {
            public HttpListenerResponse Response { get; set; }
            public int LastJournalCount { get; set; }
        }

        private HttpListener _httpListener;
        private Thread _listenerThread;
        private volatile bool _isRunning;
        private int _port = 8088;
        private int _lastMapIndex = -1;
        private readonly object _clientsLock = new object();
        private readonly List<ClientState> _activeClients = new List<ClientState>();
        private byte[] _cachedMapPng = null;
        private readonly object _cacheLock = new object();

        public bool IsRunning => _isRunning;
        public int Port => _port;

        public void SetCachedMapPng(byte[] pngData, int mapIndex)
        {
            lock (_cacheLock)
            {
                _cachedMapPng = pngData;
                _lastMapIndex = mapIndex;
            }
            Log.Info($"Map PNG cached: {pngData?.Length ?? 0} bytes for map {mapIndex}");
        }

        public bool Start(int port = 8088)
        {
            if (_isRunning)
                return false;

            _port = port;

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "MapWebServer"
                };
                _listenerThread.Start();

                Log.Info($"Map Web Server started on http://localhost:{_port}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start Map Web Server: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error stopping Map Web Server: {ex.Message}");
            }

            Log.Info("Map Web Server stopped");
        }

        private void ListenerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    HttpListenerContext context = _httpListener.GetContext();
                    Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping the listener
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error($"Map Web Server error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;

                switch (path)
                {
                    case "/":
                        ServeHtmlPage(context.Response);
                        break;
                    case "/api/mapdata":
                        ServeMapData(context.Response);
                        break;
                    case "/api/maptexture":
                        ServeMapTexture(context.Response);
                        break;
                    case "/api/events":
                        ServeEventStream(context.Response);
                        break;
                    case "/api/command":
                        HandleCommand(context.Request, context.Response);
                        break;
                    case "/api/journalsize":
                        if (context.Request.HttpMethod == "GET")
                            GetJournalSize(context.Response);
                        else if (context.Request.HttpMethod == "POST")
                            SetJournalSize(context.Request, context.Response);
                        else
                        {
                            context.Response.StatusCode = 405;
                            context.Response.Close();
                        }
                        break;
                    case "/api/minimizestates":
                        if (context.Request.HttpMethod == "GET")
                            GetMinimizeStates(context.Response);
                        else if (context.Request.HttpMethod == "POST")
                            SetMinimizeStates(context.Request, context.Response);
                        else
                        {
                            context.Response.StatusCode = 405;
                            context.Response.Close();
                        }
                        break;
                    default:
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling request: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private void ServeHtmlPage(HttpListenerResponse response)
        {
            string html = GetHtmlPage();
            byte[] buffer = Encoding.UTF8.GetBytes(html);

            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void ServeMapData(HttpListenerResponse response)
        {
            if (World.Instance == null || !World.Instance.InGame)
            {
                response.StatusCode = 503;
                response.Close();
                return;
            }

            Texture2D mapTexture = UI.Gumps.WorldMapGump.GetMapTextureForMap(World.Instance.MapIndex);

            var data = new
            {
                mapIndex = World.Instance.MapIndex,
                mapWidth = mapTexture?.Width ?? 0,
                mapHeight = mapTexture?.Height ?? 0,
                player = new
                {
                    x = World.Instance.Player?.X ?? 0,
                    y = World.Instance.Player?.Y ?? 0,
                    name = World.Instance.Player?.Name ?? ""
                },
                party = GetPartyData(),
                guild = GetGuildData(),
                markers = GetMarkersData(),
                mobiles = GetMobilesData()
            };

            string json = JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void ServeMapTexture(HttpListenerResponse response)
        {
            try
            {
                byte[] imageData = null;
                int currentMapIndex = World.Instance?.MapIndex ?? 0;

                lock (_cacheLock)
                {
                    // Check if cached map is for the current map index
                    if (_lastMapIndex != currentMapIndex)
                    {
                        Log.Warn($"Cached map index ({_lastMapIndex}) doesn't match current map index ({currentMapIndex}). Clearing cache.");
                        _cachedMapPng = null;
                    }

                    imageData = _cachedMapPng;
                }

                if (imageData == null)
                {
                    Log.Warn("Map texture not cached");
                    response.StatusCode = 404;
                    byte[] errorMsg = Encoding.UTF8.GetBytes("Map texture not loaded. Please close and reopen the web map.");
                    response.ContentType = "text/plain";
                    response.ContentLength64 = errorMsg.Length;
                    response.OutputStream.Write(errorMsg, 0, errorMsg.Length);
                    response.Close();
                    return;
                }

                response.ContentType = "image/png";
                response.ContentLength64 = imageData.Length;
                response.OutputStream.Write(imageData, 0, imageData.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error serving map texture: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.Close();
                }
                catch { }
            }
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cachedMapPng = null;
                _lastMapIndex = -1;
            }
        }

        private void ServeEventStream(HttpListenerResponse response)
        {
            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var clientState = new ClientState
            {
                Response = response,
                LastJournalCount = JournalManager.Entries.Count
            };

            lock (_clientsLock)
            {
                _activeClients.Add(clientState);
            }

            try
            {
                // Keep connection alive and send updates
                while (_isRunning && World.Instance != null && World.Instance.InGame)
                {
                    var data = new
                    {
                        mapIndex = World.Instance.MapIndex,
                        player = new
                        {
                            x = World.Instance.Player?.X ?? 0,
                            y = World.Instance.Player?.Y ?? 0,
                            name = World.Instance.Player?.Name ?? ""
                        },
                        party = GetPartyData(),
                        guild = GetGuildData(),
                        markers = GetMarkersData(),
                        mobiles = GetMobilesData(),
                        journal = MainThreadQueue.InvokeOnMainThread(() => GetNewJournalEntries(clientState))
                    };

                    string json = JsonSerializer.Serialize(data);

                    string message = $"data: {json}\n\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(message);

                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Flush();

                    Thread.Sleep(500); // Check for updates twice per second
                }
            }
            catch
            {
                // Client disconnected
            }
            finally
            {
                lock (_clientsLock)
                {
                    _activeClients.Remove(clientState);
                }
                try { response.Close(); } catch { }
            }
        }

        private object GetPartyData()
        {
            var partyMembers = new List<object>();

            if (World.Instance == null) return partyMembers;

            for (int i = 0; i < 10; i++)
            {
                PartyMember partyMember = World.Instance.Party.Members[i];

                if (partyMember != null && SerialHelper.IsValid(partyMember.Serial))
                {
                    Mobile mob = World.Instance.Mobiles.Get(partyMember.Serial);

                    if (mob != null && mob.Distance <= World.Instance.ClientViewRange)
                    {
                        WMapEntity wme = World.Instance.WMapManager.GetEntity(mob);

                        if(wme == null) continue;

                        if (string.IsNullOrEmpty(wme.Name) && !string.IsNullOrEmpty(partyMember.Name)) wme.Name = partyMember.Name;

                        partyMembers.Add(new
                        {
                            x = wme.X,
                            y = wme.Y,
                            name = wme.Name,
                            isGuild = wme.IsGuild,
                            map = World.Instance.MapIndex
                        });
                    }
                    else
                    {
                        WMapEntity wme = World.Instance.WMapManager.GetEntity(partyMember.Serial);

                        if (wme != null && !wme.IsGuild)
                        {
                            partyMembers.Add(new
                            {
                                x = wme.X,
                                y = wme.Y,
                                name = wme.Name,
                                isGuild = wme.IsGuild,
                                map = World.Instance.MapIndex
                            });
                        }
                    }
                }
            }

            return partyMembers;
        }

        private List<object> GetGuildData()
        {
            var guildMembers = new List<object>();

            if (World.Instance?.WMapManager != null && World.Instance.WMapManager.Entities != null)
            {
                foreach (WMapEntity wme in World.Instance.WMapManager.Entities.Values)
                {
                    if (wme.IsGuild && !World.Instance.Party.Contains(wme.Serial))
                    {
                        guildMembers.Add(new
                        {
                            x = wme.X,
                            y = wme.Y,
                            name = wme.Name ?? "<out of range>",
                            map = wme.Map
                        });
                    }
                }
            }

            return guildMembers;
        }

        private List<object> GetMarkersData()
        {
            var markers = new List<object>();

            if (UI.Gumps.WorldMapGump._markerFiles != null)
            {
                foreach (UI.Gumps.WorldMapGump.WMapMarkerFile markerFile in UI.Gumps.WorldMapGump._markerFiles)
                {
                    if (markerFile.Hidden || markerFile.Markers == null)
                        continue;

                    foreach (UI.Gumps.WorldMapGump.WMapMarker marker in markerFile.Markers)
                    {
                        if (marker.MapId == World.Instance.MapIndex)
                        {
                            markers.Add(new
                            {
                                x = marker.X,
                                y = marker.Y,
                                name = marker.Name,
                                color = marker.Color == Color.Transparent ? new { r = (byte)255, g = (byte)255, b = (byte)255, a = (byte)255 } : new
                                {
                                    r = marker.Color.R,
                                    g = marker.Color.G,
                                    b = marker.Color.B,
                                    a = marker.Color.A
                                },
                                iconName = marker.MarkerIconName
                            });
                        }
                    }
                }
            }

            return markers;
        }

        private object GetMobilesData()
        {
            var enemyMobiles = new List<object>();
            var otherMobiles = new List<object>();
            var allyMobiles = new List<object>();

            return MainThreadQueue.InvokeOnMainThread(() => {

                if (World.Instance?.Mobiles == null)
                {
                    return new { enemies = enemyMobiles, others = otherMobiles, allies = allyMobiles };
                }

                foreach (Mobile mob in World.Instance.Mobiles.Values)
                {
                    // Skip the player
                    if (mob == World.Instance.Player)
                        continue;

                    // Skip hidden mobiles
                    if (mob.IsHidden)
                        continue;

                    // Skip party members (shown separately)
                    if (World.Instance.Party.Contains(mob.Serial))
                        continue;

                    // Skip guild members (shown separately)
                    WMapEntity wme = World.Instance.WMapManager.GetEntity(mob.Serial);
                    if (wme != null && wme.IsGuild)
                        continue;

                    // Classify by notoriety
                    if (mob.NotorietyFlag == NotorietyFlag.Ally)
                    {
                        // Ally mobile (lime green) - only within view range
                        if (mob.Distance <= World.Instance.ClientViewRange)
                        {
                            allyMobiles.Add(new
                            {
                                serial = mob.Serial,
                                x = mob.X,
                                y = mob.Y,
                                name = mob.Name ?? ""
                            });
                        }
                    }
                    else if (mob.NotorietyFlag == NotorietyFlag.Enemy ||
                             mob.NotorietyFlag == NotorietyFlag.Murderer ||
                             mob.NotorietyFlag == NotorietyFlag.Criminal)
                    {
                        // Enemy/hostile mobile (red)
                        enemyMobiles.Add(new
                        {
                            serial = mob.Serial,
                            x = mob.X,
                            y = mob.Y,
                            name = mob.Name ?? "",
                            notoriety = (byte)mob.NotorietyFlag
                        });
                    }
                    else
                    {
                        // Other mobile (gray) - Unknown, Innocent, Gray, Invulnerable
                        otherMobiles.Add(new
                        {
                            serial = mob.Serial,
                            x = mob.X,
                            y = mob.Y,
                            name = mob.Name ?? "",
                            notoriety = (byte)mob.NotorietyFlag
                        });
                    }
                }

                return new { enemies = enemyMobiles, others = otherMobiles, allies = allyMobiles };
            });
        }

        private List<object> GetNewJournalEntries(ClientState clientState)
        {
            var newEntries = new List<object>();

            int currentCount = JournalManager.Entries.Count;

            if (currentCount > clientState.LastJournalCount)
            {
                int startIndex = clientState.LastJournalCount;
                int entriesToSend = currentCount - clientState.LastJournalCount;

                for (int i = 0; i < entriesToSend && i < 100; i++)
                {
                    int index = startIndex + i;
                    if (index < currentCount)
                    {
                        JournalEntry entry = JournalManager.Entries[index];
                        newEntries.Add(new
                        {
                            text = entry.Text,
                            hue = entry.Hue,
                            name = entry.Name ?? "",
                            time = entry.Time.ToString("HH:mm:ss"),
                            textType = entry.TextType.ToString()
                        });
                    }
                }

                clientState.LastJournalCount = currentCount;
            }

            return newEntries;
        }

        private void HandleCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                if (request.HttpMethod != "POST")
                {
                    response.StatusCode = 405;
                    response.Close();
                    return;
                }

                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    Dictionary<string, string> commandData = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                    if (commandData != null && commandData.TryGetValue("command", out string command))
                    {
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            GameActions.Say(command, 0xFFFF, MessageType.Regular, 3);
                        }

                        response.StatusCode = 200;
                        byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 400;
                    }
                }

                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling command: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private void GetJournalSize(HttpListenerResponse response)
        {
            try
            {
                int width = Client.Settings.Get(SettingsScope.Global, "webmap_journal_width", 400);
                int height = Client.Settings.Get(SettingsScope.Global, "webmap_journal_height", 300);

                var data = new
                {
                    width = width,
                    height = height
                };

                string json = JsonSerializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting journal size: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private void SetJournalSize(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    Dictionary<string, int> sizeData = JsonSerializer.Deserialize<Dictionary<string, int>>(body);

                    if (sizeData != null && sizeData.TryGetValue("width", out int width) && sizeData.TryGetValue("height", out int height))
                    {
                        _ = Client.Settings.SetAsync(SettingsScope.Global, "webmap_journal_width", width);
                        _ = Client.Settings.SetAsync(SettingsScope.Global, "webmap_journal_height", height);

                        response.StatusCode = 200;
                        byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 400;
                    }
                }

                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting journal size: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private void GetMinimizeStates(HttpListenerResponse response)
        {
            try
            {
                bool journalMinimized = Client.Settings.Get(SettingsScope.Global, "webmap_journal_minimized", false);
                bool controlsMinimized = Client.Settings.Get(SettingsScope.Global, "webmap_controls_minimized", false);

                var data = new
                {
                    journalMinimized = journalMinimized,
                    controlsMinimized = controlsMinimized
                };

                string json = JsonSerializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting minimize states: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private void SetMinimizeStates(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    Dictionary<string, bool> stateData = JsonSerializer.Deserialize<Dictionary<string, bool>>(body);

                    if (stateData != null &&
                        stateData.TryGetValue("journalMinimized", out bool journalMinimized) &&
                        stateData.TryGetValue("controlsMinimized", out bool controlsMinimized))
                    {
                        _ = Client.Settings.SetAsync(SettingsScope.Global, "webmap_journal_minimized", journalMinimized);
                        _ = Client.Settings.SetAsync(SettingsScope.Global, "webmap_controls_minimized", controlsMinimized);

                        response.StatusCode = 200;
                        byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 400;
                    }
                }

                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting minimize states: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private string GetHtmlPage() => @"<!DOCTYPE html>
<html>
<head>
    <title>TazUO World Map</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: Arial, sans-serif;
            background: #1a1a1a;
            color: #fff;
            overflow: hidden;
        }
        #controls {
            position: fixed;
            top: 10px;
            left: 10px;
            background: rgba(0,0,0,0.8);
            padding: 15px;
            border-radius: 8px;
            z-index: 1000;
            box-shadow: 0 4px 6px rgba(0,0,0,0.5);
        }
        #controls.minimized {
            padding: 10px 15px;
        }
        #controls.minimized .control-content {
            display: none;
        }
        #controls h2 {
            margin-bottom: 10px;
            font-size: 16px;
            color: #4CAF50;
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            user-select: none;
        }
        #controls.minimized h2 {
            margin-bottom: 0;
        }
        #controlsMinimizeBtn {
            background: none;
            border: none;
            color: #4CAF50;
            font-size: 16px;
            cursor: pointer;
            padding: 0 5px;
            line-height: 1;
            margin-left: 10px;
        }
        #controlsMinimizeBtn:hover {
            color: #45a049;
        }
        #controls label {
            display: block;
            margin: 8px 0;
            cursor: pointer;
        }
        #controls input[type=""checkbox""] {
            margin-right: 8px;
        }
        #controls button {
            margin: 5px 5px 5px 0;
            padding: 8px 15px;
            background: #4CAF50;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }
        #controls button:hover {
            background: #45a049;
        }
        #info {
            position: fixed;
            bottom: 10px;
            right: 10px;
            background: rgba(0,0,0,0.8);
            padding: 10px 15px;
            border-radius: 8px;
            font-size: 12px;
            z-index: 1000;
        }
        #journal {
            position: fixed;
            bottom: 10px;
            left: 10px;
            width: 400px;
            height: 300px;
            min-width: 250px;
            min-height: 150px;
            max-width: 800px;
            max-height: 80vh;
            background: rgba(0,0,0,0.9);
            border-radius: 8px;
            z-index: 1000;
            display: flex;
            flex-direction: column;
            box-shadow: 0 4px 6px rgba(0,0,0,0.5);
        }
        #journal.minimized {
            height: 40px;
        }
        #journal.minimized #journalContent,
        #journal.minimized #journalInputContainer {
            display: none;
        }
        #journalHeader {
            padding: 10px 15px;
            background: rgba(50,50,50,0.9);
            border-radius: 8px 8px 0 0;
            font-size: 14px;
            font-weight: bold;
            color: #4CAF50;
            border-bottom: 1px solid #333;
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            user-select: none;
        }
        #journal.minimized #journalHeader {
            border-bottom: none;
            border-radius: 8px;
        }
        #journalMinimizeBtn {
            background: none;
            border: none;
            color: #4CAF50;
            font-size: 16px;
            cursor: pointer;
            padding: 0 5px;
            line-height: 1;
        }
        #journalMinimizeBtn:hover {
            color: #45a049;
        }
        #journalResizeHandle {
            position: absolute;
            top: 0;
            right: 0;
            width: 15px;
            height: 15px;
            cursor: nwse-resize;
            background: linear-gradient(135deg, transparent 0%, transparent 50%, #4CAF50 50%, #4CAF50 100%);
            border-radius: 0 8px 0 0;
            opacity: 0.5;
            z-index: 10;
        }
        #journalResizeHandle:hover {
            opacity: 1;
        }
        #journal.minimized #journalResizeHandle {
            display: none;
        }
        #journalContent {
            flex: 1;
            overflow-y: auto;
            padding: 10px;
            font-size: 12px;
            font-family: 'Courier New', monospace;
        }
        #journalContent::-webkit-scrollbar {
            width: 8px;
        }
        #journalContent::-webkit-scrollbar-track {
            background: rgba(0,0,0,0.3);
        }
        #journalContent::-webkit-scrollbar-thumb {
            background: rgba(255,255,255,0.3);
            border-radius: 4px;
        }
        #journalContent::-webkit-scrollbar-thumb:hover {
            background: rgba(255,255,255,0.5);
        }
        .journal-entry {
            margin-bottom: 4px;
            line-height: 1.4;
        }
        #journalInputContainer {
            padding: 10px;
            background: rgba(30,30,30,0.9);
            border-radius: 0 0 8px 8px;
            border-top: 1px solid #333;
        }
        #journalInput {
            width: 100%;
            padding: 8px;
            background: rgba(0,0,0,0.5);
            border: 1px solid #555;
            border-radius: 4px;
            color: #fff;
            font-size: 12px;
            outline: none;
        }
        #journalInput:focus {
            border-color: #4CAF50;
        }
        #mapCanvas {
            display: block;
            cursor: grab;
            image-rendering: pixelated;
        }
        #mapCanvas:active {
            cursor: grabbing;
        }
        #status {
            position: fixed;
            top: 10px;
            right: 10px;
            background: rgba(0,0,0,0.8);
            padding: 10px 15px;
            border-radius: 8px;
            font-size: 12px;
        }
        .status-indicator {
            display: inline-block;
            width: 10px;
            height: 10px;
            border-radius: 50%;
            margin-right: 5px;
        }
        .status-connected { background: #4CAF50; }
        .status-disconnected { background: #f44336; }
    </style>
</head>
<body>
    <div id=""controls"">
        <h2 id=""mapTitle"">
            <span id=""mapTitleText"">TazUO Web Map</span>
            <button id=""controlsMinimizeBtn"" title=""Minimize/Maximize"">−</button>
        </h2>
        <div class=""control-content"">
            <button onclick=""zoomIn()"">Zoom In (+)</button>
            <button onclick=""zoomOut()"">Zoom Out (-)</button>
            <button onclick=""centerOnPlayer()"">Center</button>
            <br>
            <label><input type=""checkbox"" id=""followPlayer"" checked> Follow Player</label>
            <label><input type=""checkbox"" id=""rotateMap"" checked> Rotate Map 45°</label>
            <label><input type=""checkbox"" id=""showParty"" checked> Show Party</label>
            <label><input type=""checkbox"" id=""showGuild"" checked> Show Guild</label>
            <label><input type=""checkbox"" id=""showMarkers"" checked> Show Markers</label>
            <label><input type=""checkbox"" id=""showMobiles"" checked> Show Mobiles</label>
            <label style=""margin-left: 20px;""><input type=""checkbox"" id=""showEnemies"" checked> Enemies</label>
            <label style=""margin-left: 20px;""><input type=""checkbox"" id=""showOthers"" checked> Other</label>
            <label style=""margin-left: 20px;""><input type=""checkbox"" id=""showAllies"" checked> Allies</label>
            <label><input type=""checkbox"" id=""showNames"" checked> Show Names</label>
            <label><input type=""checkbox"" id=""showGrid"" checked> Show Grid</label>
        </div>
    </div>
    <div id=""status"">
        <span class=""status-indicator status-disconnected"" id=""statusIndicator""></span>
        <span id=""statusText"">Connecting...</span>
    </div>
    <div id=""info"">
        <div>Zoom: <span id=""zoomLevel"">1.0x</span></div>
        <div>Player: <span id=""playerPos"">0, 0</span></div>
        <div>Mouse: <span id=""mousePos"">-</span></div>
    </div>
    <div id=""journal"">
        <div id=""journalResizeHandle"" title=""Drag to resize""></div>
        <div id=""journalHeader"">
            <span>Journal</span>
            <button id=""journalMinimizeBtn"" title=""Minimize/Maximize"">−</button>
        </div>
        <div id=""journalContent""></div>
        <div id=""journalInputContainer"">
            <input type=""text"" id=""journalInput"" placeholder=""Send a message..."" />
        </div>
    </div>
    <canvas id=""mapCanvas""></canvas>

    <script>
        const canvas = document.getElementById('mapCanvas');
        const ctx = canvas.getContext('2d');
        const journalContent = document.getElementById('journalContent');
        const journalInput = document.getElementById('journalInput');
        const journalBox = document.getElementById('journal');
        const journalMinimizeBtn = document.getElementById('journalMinimizeBtn');
        const journalResizeHandle = document.getElementById('journalResizeHandle');
        const controlsBox = document.getElementById('controls');
        const controlsMinimizeBtn = document.getElementById('controlsMinimizeBtn');

        let mapImage = null;
        let mapData = null;
        let zoom = 1.0;
        let targetZoom = 1.0;
        let offsetX = 0;
        let offsetY = 0;
        let targetOffsetX = 0;
        let targetOffsetY = 0;
        let isDragging = false;
        let lastMouseX = 0;
        let lastMouseY = 0;
        let eventSource = null;
        let animationFrameId = null;
        let mouseZoomPoint = null; // Track the world position to keep under cursor during zoom
        let autoScrollJournal = true;
        let journalMinimized = false;
        let controlsMinimized = false;
        let isResizingJournal = false;
        let resizeStartX = 0;
        let resizeStartY = 0;
        let resizeStartWidth = 0;
        let resizeStartHeight = 0;

        const zoomLevels = [0.125, 0.25, 0.5, 0.75, 1, 1.5, 2, 4, 6, 8];
        let zoomIndex = 4;
        const ZOOM_SPEED = 0.15;
        const POSITION_SPEED = 0.2; // Speed for player position tweening

        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        window.addEventListener('resize', () => {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
            draw();
        });

        function animate() {
            let needsRedraw = false;

            // Smooth zoom interpolation
            if (Math.abs(zoom - targetZoom) > 0.001) {
                const oldZoom = zoom;
                zoom += (targetZoom - zoom) * ZOOM_SPEED;

                // If we're zooming to a mouse point, recalculate offset to keep that point stable
                if (mouseZoomPoint) {
                    offsetX = mouseZoomPoint.canvasX - canvas.width / 2 - mouseZoomPoint.worldX * zoom;
                    offsetY = mouseZoomPoint.canvasY - canvas.height / 2 - mouseZoomPoint.worldY * zoom;
                    targetOffsetX = offsetX;
                    targetOffsetY = offsetY;
                } else {
                    // For button zoom, maintain the center point
                    const zoomRatio = zoom / oldZoom;
                    offsetX *= zoomRatio;
                    offsetY *= zoomRatio;
                    targetOffsetX *= zoomRatio;
                    targetOffsetY *= zoomRatio;
                }

                needsRedraw = true;
            } else if (zoom !== targetZoom) {
                zoom = targetZoom;
                mouseZoomPoint = null; // Clear mouse zoom point when animation completes
                needsRedraw = true;
            }

            // Smooth position interpolation (when following player)
            if (Math.abs(offsetX - targetOffsetX) > 0.1 || Math.abs(offsetY - targetOffsetY) > 0.1) {
                offsetX += (targetOffsetX - offsetX) * POSITION_SPEED;
                offsetY += (targetOffsetY - offsetY) * POSITION_SPEED;
                needsRedraw = true;
            } else if (offsetX !== targetOffsetX || offsetY !== targetOffsetY) {
                offsetX = targetOffsetX;
                offsetY = targetOffsetY;
                needsRedraw = true;
            }

            if (needsRedraw) {
                draw();
            }

            animationFrameId = requestAnimationFrame(animate);
        }

        animate();

        async function loadMapTexture(retryCount = 0, centerAfterLoad = false) {
            try {
                updateStatus(false, 'Loading map...');
                const response = await fetch('/api/maptexture');

                if (!response.ok) {
                    console.log(`Map texture not ready, retrying... (attempt ${retryCount + 1})`);
                    updateStatus(false, `Loading map... (attempt ${retryCount + 1})`);
                    setTimeout(() => loadMapTexture(retryCount + 1, centerAfterLoad), 1000);
                    return;
                }

                const blob = await response.blob();
                const img = new Image();
                img.onload = () => {
                    mapImage = img;
                    updateStatus(true, 'Connected');
                    console.log('Map texture loaded successfully');

                    if (centerAfterLoad) {
                        console.log('Centering on player after map change');
                        document.getElementById('followPlayer').checked = true;
                        centerOnPlayer();
                    } else {
                        draw();
                    }
                };
                img.onerror = (err) => {
                    console.error('Image load error:', err);
                    updateStatus(false, 'Image failed to load');
                };
                img.src = URL.createObjectURL(blob);
            } catch (err) {
                console.error('Failed to load map texture:', err);
                updateStatus(false, 'Failed to load map');
            }
        }

        async function loadMapData() {
            try {
                const response = await fetch('/api/mapdata');
                if (!response.ok) throw new Error('Not in game');

                mapData = await response.json();
                console.log(`[INITIAL LOAD] Map index: ${mapData.mapIndex}, Player: ${mapData.player.name}`);
                updateStatus(true);
                updateTitle();

                if (document.getElementById('followPlayer').checked) {
                    centerOnPlayer();
                }

                draw();
            } catch (err) {
                console.error('Failed to load map data:', err);
                updateStatus(false);
            }
        }

        function updateTitle() {
            if (mapData && mapData.player && mapData.player.name) {
                document.getElementById('mapTitleText').textContent = `TazUO Web Map - ${mapData.player.name}`;
            }
        }

        function hueToRgb(hue) {
            // UO hue to RGB conversion - simplified
            // In reality, UO uses a complex hue system, but this provides basic color coding
            if (hue === 0) return 'rgb(200, 200, 200)'; // Gray for system messages
            if (hue < 100) return 'rgb(255, 255, 100)'; // Yellow
            if (hue < 200) return 'rgb(100, 255, 100)'; // Green
            if (hue < 500) return 'rgb(100, 200, 255)'; // Blue
            if (hue < 1000) return 'rgb(255, 150, 100)'; // Orange
            return 'rgb(255, 100, 255)'; // Magenta
        }

        function addJournalEntries(entries) {
            if (!entries || entries.length === 0) return;

            entries.forEach(entry => {
                const div = document.createElement('div');
                div.className = 'journal-entry';
                const color = hueToRgb(entry.hue);

                let text = '';
                if (entry.name) {
                    text = `[${entry.time}] ${entry.name}: ${entry.text}`;
                } else {
                    text = `[${entry.time}] ${entry.text}`;
                }

                div.style.color = color;
                div.textContent = text;
                journalContent.appendChild(div);
            });

            // Auto-scroll to bottom if enabled
            if (autoScrollJournal) {
                journalContent.scrollTop = journalContent.scrollHeight;
            }

            // Limit journal entries in DOM to prevent memory issues
            while (journalContent.children.length > 500) {
                journalContent.removeChild(journalContent.firstChild);
            }
        }

        async function sendCommand(command) {
            try {
                const response = await fetch('/api/command', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ command: command })
                });

                if (!response.ok) {
                    console.error('Failed to send command');
                }
            } catch (err) {
                console.error('Error sending command:', err);
            }
        }

        async function loadJournalSize() {
            try {
                const response = await fetch('/api/journalsize');
                if (response.ok) {
                    const data = await response.json();
                    journalBox.style.width = data.width + 'px';
                    journalBox.style.height = data.height + 'px';
                    console.log(`Loaded journal size: ${data.width}x${data.height}`);
                }
            } catch (err) {
                console.error('Error loading journal size:', err);
            }
        }

        async function saveJournalSize(width, height) {
            try {
                const response = await fetch('/api/journalsize', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ width: width, height: height })
                });

                if (response.ok) {
                    console.log(`Saved journal size: ${width}x${height}`);
                }
            } catch (err) {
                console.error('Error saving journal size:', err);
            }
        }

        async function loadMinimizeStates() {
            try {
                const response = await fetch('/api/minimizestates');
                if (response.ok) {
                    const data = await response.json();
                    journalMinimized = data.journalMinimized || false;
                    controlsMinimized = data.controlsMinimized || false;

                    // Apply loaded states
                    if (journalMinimized) {
                        // Save the current height (from loadJournalSize) before minimizing
                        savedJournalHeight = journalBox.offsetHeight;
                        journalBox.classList.add('minimized');
                        journalBox.style.height = '40px';
                        journalBox.style.minHeight = '40px';
                        journalMinimizeBtn.textContent = '+';
                    }
                    if (controlsMinimized) {
                        controlsBox.classList.add('minimized');
                        controlsMinimizeBtn.textContent = '+';
                    }

                    console.log(`Loaded minimize states: journal=${journalMinimized}, controls=${controlsMinimized}`);
                }
            } catch (err) {
                console.error('Error loading minimize states:', err);
            }
        }

        async function saveMinimizeStates() {
            try {
                const response = await fetch('/api/minimizestates', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        journalMinimized: journalMinimized,
                        controlsMinimized: controlsMinimized
                    })
                });

                if (response.ok) {
                    console.log(`Saved minimize states: journal=${journalMinimized}, controls=${controlsMinimized}`);
                }
            } catch (err) {
                console.error('Error saving minimize states:', err);
            }
        }

        // Handle journal minimize/maximize
        let savedJournalHeight = null;

        function toggleJournalMinimize() {
            journalMinimized = !journalMinimized;
            if (journalMinimized) {
                // Save current height before minimizing
                savedJournalHeight = journalBox.offsetHeight;
                journalBox.classList.add('minimized');
                journalBox.style.height = '40px';
                journalBox.style.minHeight = '40px';
                journalMinimizeBtn.textContent = '+';
            } else {
                journalBox.classList.remove('minimized');
                // Restore previous height and min-height
                if (savedJournalHeight) {
                    journalBox.style.height = savedJournalHeight + 'px';
                }
                journalBox.style.minHeight = '150px';
                journalMinimizeBtn.textContent = '−';
            }
            saveMinimizeStates();
        }

        function toggleControlsMinimize() {
            controlsMinimized = !controlsMinimized;
            if (controlsMinimized) {
                controlsBox.classList.add('minimized');
                controlsMinimizeBtn.textContent = '+';
            } else {
                controlsBox.classList.remove('minimized');
                controlsMinimizeBtn.textContent = '−';
            }
            saveMinimizeStates();
        }

        journalMinimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleJournalMinimize();
        });

        controlsMinimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleControlsMinimize();
        });

        // Handle journal resizing
        journalResizeHandle.addEventListener('mousedown', (e) => {
            e.preventDefault();
            e.stopPropagation();
            isResizingJournal = true;
            resizeStartX = e.clientX;
            resizeStartY = e.clientY;
            resizeStartWidth = journalBox.offsetWidth;
            resizeStartHeight = journalBox.offsetHeight;
        });

        document.addEventListener('mousemove', (e) => {
            if (isResizingJournal) {
                const deltaX = e.clientX - resizeStartX;
                const deltaY = resizeStartY - e.clientY; // Inverted because journal is bottom-aligned

                const newWidth = Math.max(250, Math.min(800, resizeStartWidth + deltaX));
                const newHeight = Math.max(150, Math.min(window.innerHeight * 0.8, resizeStartHeight + deltaY));

                journalBox.style.width = newWidth + 'px';
                journalBox.style.height = newHeight + 'px';
            }
        });

        document.addEventListener('mouseup', () => {
            if (isResizingJournal) {
                isResizingJournal = false;
                // Save the new size to settings
                saveJournalSize(journalBox.offsetWidth, journalBox.offsetHeight);
            }
        });

        // Handle journal input
        journalInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                const command = journalInput.value.trim();
                if (command) {
                    sendCommand(command);
                    journalInput.value = '';
                }
            }
        });

        // Detect manual scrolling to disable auto-scroll
        journalContent.addEventListener('scroll', () => {
            const isAtBottom = journalContent.scrollHeight - journalContent.scrollTop <= journalContent.clientHeight + 50;
            autoScrollJournal = isAtBottom;
        });

        function connectEventStream() {
            if (eventSource) {
                eventSource.close();
            }

            eventSource = new EventSource('/api/events');

            eventSource.onmessage = (event) => {
                const data = JSON.parse(event.data);
                if (mapData) {
                    // Check if map changed
                    if (data.mapIndex !== undefined && data.mapIndex !== mapData.mapIndex) {
                        console.log(`[MAP CHANGE DETECTED] Changing from map ${mapData.mapIndex} to map ${data.mapIndex}`);
                        mapData.mapIndex = data.mapIndex;
                        mapData.player = data.player;
                        mapData.party = data.party;
                        mapData.guild = data.guild;
                        mapData.markers = data.markers;
                        mapData.mobiles = data.mobiles;
                        updateTitle();

                        // Clear the map image immediately to show blank screen
                        mapImage = null;
                        draw(); // Redraw to show blank screen

                        loadMapTexture(0, true); // Reload the map texture for the new map and center on player
                        return; // loadMapTexture will trigger a redraw when complete
                    }

                    mapData.player = data.player;
                    mapData.party = data.party;
                    mapData.guild = data.guild;
                    mapData.markers = data.markers;
                    mapData.mobiles = data.mobiles;

                    // Handle journal entries
                    if (data.journal && data.journal.length > 0) {
                        addJournalEntries(data.journal);
                    }

                    updateTitle(); // Update title if player name changes

                    if (document.getElementById('followPlayer').checked) {
                        centerOnPlayer();
                    } else {
                        draw();
                    }
                }
            };

            eventSource.onerror = () => {
                updateStatus(false);
                setTimeout(connectEventStream, 5000);
            };
        }

        function updateStatus(connected, message) {
            const indicator = document.getElementById('statusIndicator');
            const text = document.getElementById('statusText');

            if (connected) {
                indicator.className = 'status-indicator status-connected';
                text.textContent = message || 'Connected';
            } else {
                indicator.className = 'status-indicator status-disconnected';
                text.textContent = message || 'Disconnected';
            }
        }

        // Rotate a point around origin by given angle
        function rotatePoint(x, y, angle) {
            if (angle === 0) return { x: x, y: y };

            const cos = Math.cos(angle);
            const sin = Math.sin(angle);

            return {
                x: cos * x - sin * y,
                y: sin * x + cos * y
            };
        }

        function drawLabel(ctx, text, x, y, color, zoom) {
            // Constant font size on screen regardless of zoom level
            const fontSize = 16 / zoom;
            ctx.font = `${fontSize}px Arial`;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'bottom';

            const metrics = ctx.measureText(text);
            const textWidth = metrics.width;
            const textHeight = fontSize;
            const padding = 2 / zoom;

            const labelX = x;
            const labelY = y - (6 / zoom);

            // Draw background
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(
                labelX - textWidth / 2 - padding,
                labelY - textHeight,
                textWidth + (padding * 2),
                textHeight + padding
            );

            // Draw text
            ctx.fillStyle = color;
            ctx.fillText(text, labelX, labelY);
        }

        function draw() {
            ctx.fillStyle = '#000';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            if (!mapImage || !mapData) return;

            const centerX = canvas.width / 2;
            const centerY = canvas.height / 2;

            const drawWidth = mapImage.width * zoom;
            const drawHeight = mapImage.height * zoom;

            const isRotated = document.getElementById('rotateMap').checked;
            const rotationAngle = isRotated ? Math.PI / 4 : 0; // 45 degrees in radians

            ctx.save();
            ctx.translate(centerX + offsetX, centerY + offsetY);

            // Apply rotation to the map image only
            if (isRotated) {
                ctx.rotate(rotationAngle);
            }

            ctx.scale(zoom, zoom);
            ctx.translate(-mapImage.width / 2, -mapImage.height / 2);

            ctx.drawImage(mapImage, 0, 0);

            // Draw grid
            if (document.getElementById('showGrid').checked && zoom >= 2) {
                size = 8;
                if (zoom >= 4)
                    size = 4;
                if (zoom >= 6)
                    size = 2;
                if (zoom >= 8)
                    size = 1;
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.1)';
                ctx.lineWidth = 1 / zoom;
                for (let x = 0; x < mapImage.width; x += size) {
                    ctx.beginPath();
                    ctx.moveTo(x, 0);
                    ctx.lineTo(x, mapImage.height);
                    ctx.stroke();
                }
                for (let y = 0; y < mapImage.height; y += size) {
                    ctx.beginPath();
                    ctx.moveTo(0, y);
                    ctx.lineTo(mapImage.width, y);
                    ctx.stroke();
                }
            }

            // Draw markers
            if (document.getElementById('showMarkers').checked && mapData.markers) {
                mapData.markers.forEach(marker => {
                    const markerColor = `rgba(${marker.color.r}, ${marker.color.g}, ${marker.color.b}, ${marker.color.a / 255})`;

                    // Save state before drawing marker
                    ctx.save();

                    // Position the marker (this point is already in rotated space)
                    ctx.translate(marker.x, marker.y);

                    // Counter-rotate to keep marker and label upright
                    if (isRotated) {
                        ctx.rotate(-rotationAngle);
                    }

                    // Scale for proper sizing
                    ctx.scale(1 / zoom, 1 / zoom);

                    // Draw marker circle
                    ctx.fillStyle = markerColor;
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 3, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    // Draw label
                    if (marker.name) {
                        ctx.font = '16px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(marker.name);
                        const textWidth = metrics.width;
                        const textHeight = 16;
                        const padding = 3;

                        // Draw background
                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding, textWidth + padding * 2, textHeight + padding * 2);

                        // Draw text
                        ctx.fillStyle = markerColor;
                        ctx.fillText(marker.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw enemy mobiles (red)
            if (document.getElementById('showMobiles').checked &&
                document.getElementById('showEnemies').checked &&
                mapData.mobiles &&
                mapData.mobiles.enemies) {
                const showNames = document.getElementById('showNames').checked;
                mapData.mobiles.enemies.forEach(mobile => {
                    ctx.save();
                    ctx.translate(mobile.x, mobile.y);
                    if (isRotated) ctx.rotate(-rotationAngle);
                    ctx.scale(1 / zoom, 1 / zoom);

                    // Draw red circle
                    ctx.fillStyle = '#ff0000';
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 3, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    // Draw name if enabled
                    if (showNames && mobile.name) {
                        ctx.font = '14px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(mobile.name);
                        const textWidth = metrics.width;
                        const textHeight = 14;
                        const padding = 3;

                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding,
                                    textWidth + padding * 2, textHeight + padding * 2);

                        ctx.fillStyle = '#ff0000';
                        ctx.fillText(mobile.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw other mobiles (gray)
            if (document.getElementById('showMobiles').checked &&
                document.getElementById('showOthers').checked &&
                mapData.mobiles &&
                mapData.mobiles.others) {
                const showNames = document.getElementById('showNames').checked;
                mapData.mobiles.others.forEach(mobile => {
                    ctx.save();
                    ctx.translate(mobile.x, mobile.y);
                    if (isRotated) ctx.rotate(-rotationAngle);
                    ctx.scale(1 / zoom, 1 / zoom);

                    // Draw gray circle
                    ctx.fillStyle = '#808080';
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 3, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    // Draw name if enabled
                    if (showNames && mobile.name) {
                        ctx.font = '14px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(mobile.name);
                        const textWidth = metrics.width;
                        const textHeight = 14;
                        const padding = 3;

                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding,
                                    textWidth + padding * 2, textHeight + padding * 2);

                        ctx.fillStyle = '#808080';
                        ctx.fillText(mobile.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw ally mobiles (lime green)
            if (document.getElementById('showMobiles').checked &&
                document.getElementById('showAllies').checked &&
                mapData.mobiles &&
                mapData.mobiles.allies) {
                const showNames = document.getElementById('showNames').checked;
                mapData.mobiles.allies.forEach(mobile => {
                    ctx.save();
                    ctx.translate(mobile.x, mobile.y);
                    if (isRotated) ctx.rotate(-rotationAngle);
                    ctx.scale(1 / zoom, 1 / zoom);

                    // Draw lime circle
                    ctx.fillStyle = '#00ff00';
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 3, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    // Draw name if enabled
                    if (showNames && mobile.name) {
                        ctx.font = '14px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(mobile.name);
                        const textWidth = metrics.width;
                        const textHeight = 14;
                        const padding = 3;

                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding,
                                    textWidth + padding * 2, textHeight + padding * 2);

                        ctx.fillStyle = '#00ff00';
                        ctx.fillText(mobile.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw guild members
            if (document.getElementById('showGuild').checked && mapData.guild) {
                const showNames = document.getElementById('showNames').checked;
                mapData.guild.forEach(member => {
                    ctx.save();
                    ctx.translate(member.x, member.y);
                    if (isRotated) ctx.rotate(-rotationAngle);
                    ctx.scale(1 / zoom, 1 / zoom);

                    ctx.fillStyle = '#00ff00';
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 4, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    if (showNames && member.name) {
                        ctx.font = '16px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(member.name);
                        const textWidth = metrics.width;
                        const textHeight = 16;
                        const padding = 3;

                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding, textWidth + padding * 2, textHeight + padding * 2);

                        ctx.fillStyle = '#00ff00';
                        ctx.fillText(member.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw party members
            if (document.getElementById('showParty').checked && mapData.party) {
                const showNames = document.getElementById('showNames').checked;
                mapData.party.forEach(member => {
                    const color = member.isGuild ? '#00ff00' : '#ffff00';

                    ctx.save();
                    ctx.translate(member.x, member.y);
                    if (isRotated) ctx.rotate(-rotationAngle);
                    ctx.scale(1 / zoom, 1 / zoom);

                    ctx.fillStyle = color;
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1;
                    ctx.beginPath();
                    ctx.arc(0, 0, 4, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.stroke();

                    if (showNames && member.name) {
                        ctx.font = '16px Arial';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        const metrics = ctx.measureText(member.name);
                        const textWidth = metrics.width;
                        const textHeight = 16;
                        const padding = 3;

                        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding, textWidth + padding * 2, textHeight + padding * 2);

                        ctx.fillStyle = color;
                        ctx.fillText(member.name, 0, -6);
                    }

                    ctx.restore();
                });
            }

            // Draw player
            if (mapData.player) {
                ctx.save();
                ctx.translate(mapData.player.x, mapData.player.y);
                if (isRotated) ctx.rotate(-rotationAngle);
                ctx.scale(1 / zoom, 1 / zoom);

                ctx.fillStyle = '#ff0000';
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.arc(0, 0, 5, 0, Math.PI * 2);
                ctx.fill();
                ctx.stroke();

                const showNames = document.getElementById('showNames').checked;
                if (showNames && mapData.player.name) {
                    ctx.font = '16px Arial';
                    ctx.textAlign = 'center';
                    ctx.textBaseline = 'bottom';
                    const metrics = ctx.measureText(mapData.player.name);
                    const textWidth = metrics.width;
                    const textHeight = 16;
                    const padding = 3;

                    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                    ctx.fillRect(-textWidth / 2 - padding, -textHeight - 6 - padding, textWidth + padding * 2, textHeight + padding * 2);

                    ctx.fillStyle = '#ff0000';
                    ctx.fillText(mapData.player.name, 0, -6);
                }

                ctx.restore();

                document.getElementById('playerPos').textContent =
                    `${mapData.player.x}, ${mapData.player.y}`;
            }

            ctx.restore();

            document.getElementById('zoomLevel').textContent = zoom.toFixed(2) + 'x';
        }

        function centerOnPlayer() {
            if (!mapData || !mapData.player || !mapImage) return;

            // Calculate target offset to center player position on screen
            // The map coordinate system has (0,0) at top-left
            // We need to offset so player appears at canvas center

            // Calculate the player's position relative to map center, scaled by zoom
            let scaledX = (mapData.player.x - mapImage.width / 2) * zoom;
            let scaledY = (mapData.player.y - mapImage.height / 2) * zoom;

            // If rotated, we need to rotate these coordinates
            const isRotated = document.getElementById('rotateMap').checked;
            if (isRotated) {
                const rotated = rotatePoint(scaledX, scaledY, Math.PI / 4);
                scaledX = rotated.x;
                scaledY = rotated.y;
            }

            // Negate to get the offset (we want to move the map in opposite direction)
            targetOffsetX = -scaledX;
            targetOffsetY = -scaledY;
            // Animation loop will smoothly interpolate to these target values
        }

        function zoomIn() {
            if (zoomIndex < zoomLevels.length - 1) {
                zoomIndex++;
                targetZoom = zoomLevels[zoomIndex];
                mouseZoomPoint = null; // Button zoom uses center, not mouse position
            }
        }

        function zoomOut() {
            if (zoomIndex > 0) {
                zoomIndex--;
                targetZoom = zoomLevels[zoomIndex];
                mouseZoomPoint = null; // Button zoom uses center, not mouse position
            }
        }

        function zoomToMouse(mouseX, mouseY, zoomDelta) {
            // Apply zoom change
            const newZoomIndex = Math.max(0, Math.min(zoomLevels.length - 1, zoomIndex + zoomDelta));
            if (newZoomIndex === zoomIndex) return;

            zoomIndex = newZoomIndex;
            const newZoom = zoomLevels[zoomIndex];

            // Calculate world position under mouse at current zoom level
            const worldX = (mouseX - canvas.width / 2 - offsetX) / zoom;
            const worldY = (mouseY - canvas.height / 2 - offsetY) / zoom;

            // Store the point to keep stable during zoom animation
            mouseZoomPoint = {
                canvasX: mouseX,
                canvasY: mouseY,
                worldX: worldX,
                worldY: worldY
            };

            targetZoom = newZoom;
            // Let the animate() loop smoothly interpolate to targetZoom
        }

        canvas.addEventListener('mousedown', (e) => {
            // Only allow dragging with left mouse button, and not if resizing journal
            if (e.button === 0 && !isResizingJournal) {
                isDragging = true;
                lastMouseX = e.clientX;
                lastMouseY = e.clientY;
            }
        });

        canvas.addEventListener('mousemove', (e) => {
            if (isDragging) {
                const dx = e.clientX - lastMouseX;
                const dy = e.clientY - lastMouseY;

                // Only disable follow player if user has dragged more than 5 pixels
                // This prevents accidental clicks from disabling it
                if (Math.abs(dx) > 5 || Math.abs(dy) > 5) {
                    document.getElementById('followPlayer').checked = false;
                }

                offsetX += dx;
                offsetY += dy;
                targetOffsetX = offsetX;
                targetOffsetY = offsetY;
                lastMouseX = e.clientX;
                lastMouseY = e.clientY;
                draw();
            }

            // Update mouse world coordinates
            if (mapImage && mapData) {
                const rect = canvas.getBoundingClientRect();
                const mouseCanvasX = e.clientX - rect.left;
                const mouseCanvasY = e.clientY - rect.top;

                const centerX = canvas.width / 2;
                const centerY = canvas.height / 2;

                let screenX = (mouseCanvasX - centerX - offsetX) / zoom;
                let screenY = (mouseCanvasY - centerY - offsetY) / zoom;

                // If rotated, we need to inverse rotate the screen coordinates
                const isRotated = document.getElementById('rotateMap').checked;
                if (isRotated) {
                    const rotated = rotatePoint(screenX, screenY, -Math.PI / 4);
                    screenX = rotated.x;
                    screenY = rotated.y;
                }

                const worldX = screenX + mapImage.width / 2;
                const worldY = screenY + mapImage.height / 2;

                document.getElementById('mousePos').textContent =
                    `${Math.floor(worldX)}, ${Math.floor(worldY)}`;
            }
        });

        canvas.addEventListener('mouseup', () => {
            isDragging = false;
        });

        canvas.addEventListener('wheel', (e) => {
            e.preventDefault();

            const zoomDelta = e.deltaY < 0 ? 1 : -1;

            // When follow player is enabled, zoom towards center
            // When disabled, zoom towards mouse position
            if (document.getElementById('followPlayer').checked) {
                // Zoom towards center (like button zoom)
                const newZoomIndex = Math.max(0, Math.min(zoomLevels.length - 1, zoomIndex + zoomDelta));
                if (newZoomIndex !== zoomIndex) {
                    zoomIndex = newZoomIndex;
                    targetZoom = zoomLevels[zoomIndex];
                    mouseZoomPoint = null; // Center zoom
                }
            } else {
                // Zoom towards mouse position
                const rect = canvas.getBoundingClientRect();
                const mouseX = e.clientX - rect.left;
                const mouseY = e.clientY - rect.top;
                zoomToMouse(mouseX, mouseY, zoomDelta);
            }
        });

        // Keyboard shortcuts
        window.addEventListener('keydown', (e) => {
            switch(e.key) {
                case '+':
                case '=':
                    zoomIn();
                    break;
                case '-':
                case '_':
                    zoomOut();
                    break;
                case 'c':
                case 'C':
                    centerOnPlayer();
                    break;
                case 'f':
                case 'F':
                    const followCheckbox = document.getElementById('followPlayer');
                    followCheckbox.checked = !followCheckbox.checked;
                    if (followCheckbox.checked) centerOnPlayer();
                    break;
            }
        });

        // Add event listeners for checkboxes to trigger redraw
        document.getElementById('rotateMap').addEventListener('change', draw);
        document.getElementById('showParty').addEventListener('change', draw);
        document.getElementById('showGuild').addEventListener('change', draw);
        document.getElementById('showMarkers').addEventListener('change', draw);
        document.getElementById('showMobiles').addEventListener('change', draw);
        document.getElementById('showEnemies').addEventListener('change', draw);
        document.getElementById('showOthers').addEventListener('change', draw);
        document.getElementById('showAllies').addEventListener('change', draw);
        document.getElementById('showNames').addEventListener('change', draw);
        document.getElementById('showGrid').addEventListener('change', draw);

        // Initialize
        loadJournalSize();
        loadMinimizeStates();
        loadMapTexture();
        loadMapData();
        connectEventStream();

        // Reload map texture every 30 seconds in case it changes
        setInterval(loadMapTexture, 30000);
    </script>
</body>
</html>";

        public void Dispose() => Stop();
    }
}
