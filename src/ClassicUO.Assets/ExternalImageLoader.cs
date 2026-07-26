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
    public class ExternalImageLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private string exePath;
        private string _uoDirectory;

        private Dictionary<string, Texture2D> EmbeddedArt = new Dictionary<string, Texture2D>();
        private Dictionary<string, Texture2D> _zipNamedTextures = new Dictionary<string, Texture2D>();
        private Texture2D _emptyTexture;

        private Dictionary<uint, string> gump_availableFilePaths = new Dictionary<uint, string>();
        private Dictionary<uint, (uint[] pixels, int width, int height)> gump_textureCache = new Dictionary<uint, (uint[], int, int)>();

        private Dictionary<uint, string> art_availableFilePaths = new Dictionary<uint, string>();
        private Dictionary<uint, (uint[] pixels, int width, int height)> art_textureCache = new Dictionary<uint, (uint[], int, int)>();

        public GraphicsDevice GraphicsDevice { set; get; }

        public static ExternalImageLoader _instance;
        public static ExternalImageLoader Instance => _instance ?? (_instance = new ExternalImageLoader());

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
            if (!gump_availableFilePaths.TryGetValue(graphic, out string fullImagePath))
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

            if (GraphicsDevice != null && File.Exists(fullImagePath))
            {
                try
                {
                    Texture2D tempTexture;
                    if (fullImagePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    {
                        tempTexture = LoadBmp(GraphicsDevice, fullImagePath);
                    }
                    else
                    {
                        using FileStream titleStream = File.OpenRead(fullImagePath);
                        tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    }

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
                catch (Exception ex)
                {
                    Log.Error($"Failed to load gump image '{fullImagePath}': {ex.Message}");
                }
            }

            return new GumpInfo();
        }

        public ArtInfo LoadArtTexture(uint graphic)
        {
            if (!art_availableFilePaths.TryGetValue(graphic, out string fullImagePath))
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

            if (GraphicsDevice != null && File.Exists(fullImagePath))
            {
                try
                {
                    Texture2D tempTexture;
                    if (fullImagePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    {
                        tempTexture = LoadBmp(GraphicsDevice, fullImagePath);
                    }
                    else
                    {
                        using FileStream titleStream = File.OpenRead(fullImagePath);
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
                catch (Exception ex)
                {
                    Log.Error($"Failed to load art image '{fullImagePath}': {ex.Message}");
                }
            }

            return new ArtInfo();
        }

        private static Texture2D LoadBmp(GraphicsDevice gd, string path)
        {
            byte[] file = File.ReadAllBytes(path);
            if (file.Length < 54 || file[0] != 'B' || file[1] != 'M')
                return null;

            int dataOffset = BitConverter.ToInt32(file, 10);
            int headerSize = BitConverter.ToInt32(file, 14);
            if (headerSize < 40)
                return null;

            int width = BitConverter.ToInt32(file, 18);
            int rawHeight = BitConverter.ToInt32(file, 22);
            bool topDown = rawHeight < 0;
            int height = Math.Abs(rawHeight);
            short bpp = BitConverter.ToInt16(file, 28);

            if (bpp != 24 && bpp != 32)
            {
                Log.Error($"Unsupported BMP bit depth {bpp} in '{path}'");
                return null;
            }

            int bytesPerPixel = bpp / 8;
            int rowStride = width * bytesPerPixel;
            int rowPadding = (4 - (rowStride % 4)) % 4;
            int rowBytes = rowStride + rowPadding;

            var texture = new Texture2D(gd, width, height);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                int srcRow = topDown ? y : (height - 1 - y);
                int srcOffset = dataOffset + srcRow * rowBytes;

                for (int x = 0; x < width; x++)
                {
                    int srcIdx = srcOffset + x * bytesPerPixel;
                    byte b = file[srcIdx];
                    byte g = file[srcIdx + 1];
                    byte r = file[srcIdx + 2];
                    byte a = (bytesPerPixel == 4) ? file[srcIdx + 3] : (byte)255;

                    pixels[y * width + x] = new Color(r, g, b, a);
                }
            }

            texture.SetData(pixels);
            return texture;
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

        private static string[] FindImageFiles(string directory)
        {
            var results = new List<string>();
            results.AddRange(Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories));
            results.AddRange(Directory.GetFiles(directory, "*.bmp", SearchOption.AllDirectories));
            return results.ToArray();
        }

        public void Load(string uoDirectory = null)
        {
            exePath = AppContext.BaseDirectory;
            _uoDirectory = uoDirectory;

            string gumpPath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER);

            if (Directory.Exists(gumpPath))
            {
                string[] files = FindImageFiles(gumpPath);

                for (int i = 0; i < files.Length; i++)
                {
                    string fname = Path.GetFileName(files[i]);
                    string baseName = Path.GetFileNameWithoutExtension(fname);
                    if (TryParseId(baseName, out uint id))
                        gump_availableFilePaths[id] = files[i];
                }
            }
            else
            {
                Directory.CreateDirectory(gumpPath);
            }

            string artPath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER);

            if (Directory.Exists(artPath))
            {
                string[] files = FindImageFiles(artPath);

                for (int i = 0; i < files.Length; i++)
                {
                    string fname = Path.GetFileName(files[i]);
                    string baseName = Path.GetFileNameWithoutExtension(fname);

                    if (TryParseId(baseName, out uint gfx))
                    {
                        art_availableFilePaths[gfx + 0x4000] = files[i];
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
                if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                && !entry.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)) continue;

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
                    string baseName = Path.GetFileNameWithoutExtension(entry.Name);

                    if (folder.Equals(GUMP_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseId(baseName, out uint id) && !gump_textureCache.ContainsKey(id))
                            RegisterGumpFromBytes(id, bytes);
                    }
                    else if (folder.Equals(ART_EXTERNAL_FOLDER, StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseId(baseName, out uint fileId))
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
                    if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    && !entry.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)) continue;
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

                gump_availableFilePaths.TryAdd(id, $"0x{id:X}");
            }
            catch (Exception ex) { Log.Error($"Error registering zip gump image {id}: {ex.Message}"); }
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

                art_availableFilePaths.TryAdd(id, $"0x{id:X}");
            }
            catch (Exception ex) { Log.Error($"Error registering zip art PNG {id}: {ex.Message}"); }
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
