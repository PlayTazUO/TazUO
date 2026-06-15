using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Assets
{
    public class PNGLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private string exePath;
        private string _uoDirectory;

        private Dictionary<string, Texture2D> EmbeddedArt = new Dictionary<string, Texture2D>();
        private Dictionary<string, Texture2D> _zipNamedTextures = new Dictionary<string, Texture2D>();
        private Texture2D _emptyTexture;

        private uint[] gump_availableIDs;
        private Dictionary<uint, (uint[] pixels, int width, int height)> gump_textureCache = new Dictionary<uint, (uint[], int, int)>();

        private uint[] art_availableIDs;
        private Dictionary<uint, (uint[] pixels, int width, int height)> art_textureCache = new Dictionary<uint, (uint[], int, int)>();

        public GraphicsDevice GraphicsDevice { set; get; }

        public static PNGLoader _instance;
        public static PNGLoader Instance => _instance ?? (_instance = new PNGLoader());

        public bool TryGetEmbeddedTexture(string name, out Texture2D texture)
        {
            if (EmbeddedArt.TryGetValue(name, out texture))
            {
                return true;
            }

            if (_emptyTexture == null && GraphicsDevice != null)
            {
                _emptyTexture = new Texture2D(GraphicsDevice, 1, 1);
                _emptyTexture.SetData(new Color[] { Color.Transparent });
            }

            texture = _emptyTexture;
            return false;
        }

        public bool TryGetNamedZipTexture(string name, out Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                texture = null;
                return false;
            }
            return _zipNamedTextures.TryGetValue(name, out texture);
        }

        public Texture2D GetImageTexture(string fullImagePath)
        {
            Texture2D texture = null;

            if (GraphicsDevice != null && File.Exists(fullImagePath))
            {
                FileStream titleStream = File.OpenRead(fullImagePath);
                texture = Texture2D.FromStream(GraphicsDevice, titleStream);
                titleStream.Close();
                var buffer = new Color[texture.Width * texture.Height];
                texture.GetData(buffer);

                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);

                texture.SetData(buffer);
            }

            return texture;
        }

        public GumpInfo LoadGumpTexture(uint graphic)
        {
            if (gump_availableIDs == null)
                return new GumpInfo();

            int index = Array.IndexOf(gump_availableIDs, graphic);

            if (index == -1)
                return new GumpInfo();

            if (gump_textureCache.TryGetValue(graphic, out (uint[] pixels, int width, int height) cached))
            {
                return new GumpInfo()
                {
                    Pixels = cached.pixels,
                    Width = cached.width,
                    Height = cached.height
                };
            }

            if (exePath != null && GraphicsDevice != null)
            {
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER, ((int)graphic).ToString() + ".png");

                if (File.Exists(fullImagePath))
                {
                    FileStream titleStream = File.OpenRead(fullImagePath);
                    var tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    titleStream.Close();

                    if (tempTexture == null)
                        return new GumpInfo();

                    FixPNGAlpha(ref tempTexture);

                    uint[] pixels = GetPixels(tempTexture);
                    int width = tempTexture.Width;
                    int height = tempTexture.Height;
                    gump_textureCache.Add(graphic, (pixels, width, height));
                    tempTexture.Dispose();

                    return new GumpInfo()
                    {
                        Pixels = pixels,
                        Width = width,
                        Height = height
                    };
                }
            }

            return new GumpInfo();
        }

        public ArtInfo LoadArtTexture(uint graphic)
        {
            if (art_availableIDs == null)
                return new ArtInfo();

            int index = Array.IndexOf(art_availableIDs, graphic);

            if (index == -1)
                return new ArtInfo();

            if (art_textureCache.TryGetValue(graphic, out (uint[] pixels, int width, int height) cached))
            {
                return new ArtInfo()
                {
                    Pixels = cached.pixels,
                    Width = cached.width,
                    Height = cached.height
                };
            }

            if (exePath != null && GraphicsDevice != null)
            {
                uint fileGraphic = graphic - 0x4000;
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER, fileGraphic.ToString() + ".png");

                if (File.Exists(fullImagePath))
                {
                    Texture2D tempTexture;
                    using (FileStream titleStream = File.OpenRead(fullImagePath))
                    {
                        tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    }

                    if (tempTexture == null)
                        return new ArtInfo();

                    FixPNGAlpha(ref tempTexture);

                    uint[] pixels = GetPixels(tempTexture);
                    int width = tempTexture.Width;
                    int height = tempTexture.Height;
                    art_textureCache.Add(graphic, (pixels, width, height));
                    tempTexture.Dispose();

                    return new ArtInfo()
                    {
                        Pixels = pixels,
                        Width = width,
                        Height = height
                    };
                }
            }

            return new ArtInfo();
        }

        private uint[] GetPixels(Texture2D texture)
        {
            if (texture == null)
            {
                return new uint[0];
            }

            var pixelColors = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(pixelColors);

            uint[] pixels = new uint[pixelColors.Length];
            for (int i = 0; i < pixelColors.Length; i++)
            {
                pixels[i] = pixelColors[i].PackedValue;
            }

            return pixels;
        }

        public void Load(string uoDirectory = null)
        {
            exePath = AppContext.BaseDirectory;
            _uoDirectory = uoDirectory;

            string gumpPath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER);

            if (Directory.Exists(gumpPath))
            {
                string[] files = Directory.GetFiles(gumpPath, "*.png", SearchOption.TopDirectoryOnly);
                gump_availableIDs = new uint[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    string fname = Path.GetFileName(files[i]);
                    uint.TryParse(fname.Substring(0, fname.Length - 4), out gump_availableIDs[i]);
                }
            }
            else
            {
                Directory.CreateDirectory(gumpPath);
            }

            string artPath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER);

            if (Directory.Exists(artPath))
            {
                string[] files = Directory.GetFiles(artPath, "*.png", SearchOption.TopDirectoryOnly);
                art_availableIDs = new uint[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    string fname = Path.GetFileName(files[i]);

                    if (uint.TryParse(fname.Substring(0, fname.Length - 4), out uint gfx))
                    {
                        art_availableIDs[i] = gfx + 0x4000;
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(artPath);
            }
        }

        public void LoadResourceAssets(GumpsLoader gumps)
        {
            Log.Debug("Loading resource assets");

            System.Reflection.Assembly assembly = GetType().Assembly;

            //Load all embedded art in gumpartassets folder
            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (string resourceName in resourceNames)
            {
                string path = assembly.GetName().Name + ".gumpartassets.";

                if (resourceName.StartsWith(path, StringComparison.Ordinal) && resourceName.EndsWith(".png", StringComparison.Ordinal))
                {
                    string fName = resourceName.Substring(path.Length);
                    Log.Debug("Loading PNG: " + fName);

                    try
                    {
                        Stream stream = assembly.GetManifestResourceStream(resourceName);

                        if (stream != null)
                        {
                            var texture = Texture2D.FromStream(GraphicsDevice, stream);

                            if (texture == null)
                            {
                                stream.Dispose();
                                continue;
                            }

                            FixPNGAlpha(ref texture);
                            EmbeddedArt.Add(fName, texture);
                            stream.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            LoadTuoAssetsZips();
        }

        private static void FixPNGAlpha(ref Texture2D texture)
        {
            var buffer = new Color[texture.Width * texture.Height];
            texture.GetData(buffer);

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);

            texture.SetData(buffer);
        }

        public void RegisterZipPNGs(ZipArchive archive)
        {
            if (GraphicsDevice == null) return;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                byte[] bytes;
                using (var ms = new MemoryStream())
                using (var es = entry.Open())
                {
                    es.CopyTo(ms);
                    bytes = ms.ToArray();
                }

                // Register as a named texture (full path and filename shortcut)
                string entryPath = entry.FullName.Replace('\\', '/');
                RegisterNamedZipTexture(entryPath, bytes);
                if (!_zipNamedTextures.ContainsKey(entry.Name))
                    RegisterNamedZipTexture(entry.Name, bytes);

                // Also handle gumps/ and art/ ID-based overrides
                string[] parts = entryPath.Split('/');
                if (parts.Length >= 2)
                {
                    string folder = parts[parts.Length - 2];
                    string baseName = entry.Name.Substring(0, entry.Name.Length - 4);

                    if (folder.Equals(GUMP_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                    {
                        if (uint.TryParse(baseName, out uint id) && !gump_textureCache.ContainsKey(id))
                            RegisterGumpFromBytes(id, bytes);
                    }
                    else if (folder.Equals(ART_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                    {
                        if (uint.TryParse(baseName, out uint fileId))
                        {
                            uint graphicId = fileId + 0x4000;
                            if (!art_textureCache.ContainsKey(graphicId))
                                RegisterArtFromBytes(graphicId, bytes);
                        }
                    }
                }
            }
        }

        private static bool TryParseId(string value, out uint result)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out result);
            return uint.TryParse(value, out result);
        }

        private static bool ShouldSkipEntry(string fullName)
        {
            string normalized = fullName.Replace('\\', '/');
            foreach (string seg in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (seg[0] == '_' || seg[0] == '.') return true;
            }
            return false;
        }

        private void LoadTuoAssetsZips()
        {
            const string ZIP_NAME = "tuoassets.zip";

            string exeZip = Path.Combine(exePath, ZIP_NAME);
            LoadTuoAssetsZip(exeZip);

            if (!string.IsNullOrEmpty(_uoDirectory))
            {
                string uoZip = Path.Combine(_uoDirectory, ZIP_NAME);
                if (!string.Equals(uoZip, exeZip, StringComparison.OrdinalIgnoreCase))
                    LoadTuoAssetsZip(uoZip);
            }
        }

        private void LoadTuoAssetsZip(string zipPath)
        {
            if (GraphicsDevice == null || !File.Exists(zipPath)) return;

            Log.Info($"Loading tuoassets.zip: {zipPath}");
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                    if (ShouldSkipEntry(entry.FullName)) continue;

                    byte[] bytes;
                    using (var ms = new MemoryStream())
                    using (var es = entry.Open())
                    {
                        es.CopyTo(ms);
                        bytes = ms.ToArray();
                    }

                    if (EmbeddedArt.ContainsKey(entry.Name))
                    {
                        try
                        {
                            using var ms = new MemoryStream(bytes);
                            var tex = Texture2D.FromStream(GraphicsDevice, ms);
                            if (tex == null) continue;
                            FixPNGAlpha(ref tex);
                            if (EmbeddedArt.TryGetValue(entry.Name, out Texture2D old)
                                && old != null && !old.IsDisposed)
                                old.Dispose();
                            EmbeddedArt[entry.Name] = tex;
                            Log.Debug($"tuoassets.zip overrode embedded asset: {entry.Name}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"tuoassets.zip: error overriding embedded asset '{entry.Name}': {ex.Message}");
                        }
                        continue;
                    }

                    string entryPath = entry.FullName.Replace('\\', '/');
                    string[] parts = entryPath.Split('/');
                    if (parts.Length >= 2)
                    {
                        string folder = parts[parts.Length - 2];
                        string baseName = Path.GetFileNameWithoutExtension(entry.Name);

                        if (folder.Equals(GUMP_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                        {
                            if (TryParseId(baseName, out uint id))
                                RegisterGumpFromBytes(id, bytes);
                        }
                        else if (folder.Equals(ART_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                        {
                            if (TryParseId(baseName, out uint fileId))
                                RegisterArtFromBytes(fileId + 0x4000, bytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"tuoassets.zip: error loading '{zipPath}': {ex.Message}");
            }
        }

        private void RegisterNamedZipTexture(string name, byte[] bytes)
        {
            if (GraphicsDevice == null) return;
            try
            {
                using var ms = new MemoryStream(bytes);
                var tex = Texture2D.FromStream(GraphicsDevice, ms);
                if (tex == null) return;
                FixPNGAlpha(ref tex);
                if (_zipNamedTextures.TryGetValue(name, out Texture2D existing) && existing != null && !existing.IsDisposed)
                    existing.Dispose();
                _zipNamedTextures[name] = tex;
            }
            catch (Exception ex) { Log.Error($"Error registering named zip texture '{name}': {ex.Message}"); }
        }

        private void RegisterGumpFromBytes(uint id, byte[] bytes)
        {
            if (GraphicsDevice == null) return;
            try
            {
                using var ms = new MemoryStream(bytes);
                var tex = Texture2D.FromStream(GraphicsDevice, ms);
                if (tex == null) return;
                FixPNGAlpha(ref tex);
                uint[] pixels = GetPixels(tex);
                int width = tex.Width, height = tex.Height;
                gump_textureCache[id] = (pixels, width, height);
                tex.Dispose();

                AppendToAvailableIDs(ref gump_availableIDs, id);
            }
            catch (Exception ex) { Log.Error($"Error registering zip gump PNG {id}: {ex.Message}"); }
        }

        private void RegisterArtFromBytes(uint id, byte[] bytes)
        {
            if (GraphicsDevice == null) return;
            try
            {
                using var ms = new MemoryStream(bytes);
                var tex = Texture2D.FromStream(GraphicsDevice, ms);
                if (tex == null) return;
                FixPNGAlpha(ref tex);
                uint[] pixels = GetPixels(tex);
                int width = tex.Width, height = tex.Height;
                art_textureCache[id] = (pixels, width, height);
                tex.Dispose();

                AppendToAvailableIDs(ref art_availableIDs, id);
            }
            catch (Exception ex) { Log.Error($"Error registering zip art PNG {id}: {ex.Message}"); }
        }

        private static void AppendToAvailableIDs(ref uint[] arr, uint id)
        {
            if (arr == null)
            {
                arr = [id];
                return;
            }
            if (Array.IndexOf(arr, id) >= 0) return;
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = id;
        }

        public void ClearArtPixelCache(uint graphic) => art_textureCache.Remove(graphic);

        public void ClearGumpPixelCache(uint graphic) => gump_textureCache.Remove(graphic);

        public void ClearAllPixelCaches()
        {
            art_textureCache.Clear();
            gump_textureCache.Clear();
        }
    }
}
