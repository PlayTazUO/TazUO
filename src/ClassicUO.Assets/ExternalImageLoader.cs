using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class ExternalImageLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private string exePath;

        private Dictionary<string, Texture2D> EmbeddedArt = new Dictionary<string, Texture2D>();
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

        public Texture2D GetImageTexture(string fullImagePath)
        {
            Texture2D texture = null;

            if (GraphicsDevice != null && File.Exists(fullImagePath))
            {
                FileStream titleStream = File.OpenRead(fullImagePath);
                texture = Texture2D.FromStream(GraphicsDevice, titleStream);
                titleStream.Close();
                Color[] buffer = new Color[texture.Width * texture.Height];
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

            if (gump_textureCache.TryGetValue(graphic, out var cached))
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
                        FileStream titleStream = File.OpenRead(fullImagePath);
                        tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                        titleStream.Close();
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
                    Console.WriteLine($"Failed to load gump image '{fullImagePath}': {ex.Message}");
                }
            }

            return new GumpInfo();
        }

        public ArtInfo LoadArtTexture(uint graphic)
        {
            if (!art_availableFilePaths.TryGetValue(graphic, out string fullImagePath))
                return new ArtInfo();

            if (art_textureCache.TryGetValue(graphic, out var cached))
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
                        using (FileStream titleStream = File.OpenRead(fullImagePath))
                        {
                            tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                        }
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
                    Console.WriteLine($"Failed to load art image '{fullImagePath}': {ex.Message}");
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
                Console.WriteLine($"Unsupported BMP bit depth {bpp} in '{path}'");
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

            Color[] pixelColors = new Color[texture.Width * texture.Height];
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

        private static bool TryParseId(string value, out uint result)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out result);
            return uint.TryParse(value, out result);
        }

        public void Load()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            exePath = Path.GetDirectoryName(strExeFilePath);

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

        public Task LoadResourceAssets()
        {
            return Task.Run(
                () =>
                {
                    var assembly = GetType().Assembly;

                    //Load the custom gump art included with TUO
                    for (uint i = 40303; i <= 40312; i++)
                    {
                        //Check if the art already exists
                        var gumpInfo = LoadGumpTexture(i);

                        if (gumpInfo.Pixels == null || gumpInfo.Pixels.IsEmpty)
                        {
                            gumpInfo = GumpsLoader.Instance.GetGump(i);
                            if (gumpInfo.Pixels != null && !gumpInfo.Pixels.IsEmpty)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        var resourceName = assembly.GetName().Name + $".gumpartassets.{i}.png";
                        try
                        {
                            Stream stream = assembly.GetManifestResourceStream(resourceName);
                            if (stream != null)
                            {
                                Texture2D tempTexture = Texture2D.FromStream(GraphicsDevice, stream);
                                FixPNGAlpha(ref tempTexture);

                                uint[] pixels = GetPixels(tempTexture);
                                int width = tempTexture.Width;
                                int height = tempTexture.Height;
                                gump_textureCache.Add(i, (pixels, width, height));
                                tempTexture.Dispose();


                                //Increase available gump id's
                                if (!gump_availableFilePaths.ContainsKey(i))
                                    gump_availableFilePaths[i] = $"0x{i:X}";

                                stream.Dispose();
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                    }

                    //Load all embedded art in gumpartassets folder
                    var resourceNames = assembly.GetManifestResourceNames();
                    foreach (var resourceName in resourceNames)
                    {
                        string path = assembly.GetName().Name + ".gumpartassets.";
                        if (resourceName.IndexOf(path) == 0 && resourceName.EndsWith(".png"))
                        {
                            var fName = resourceName.Substring(path.Length);
                            try
                            {
                                Stream stream = assembly.GetManifestResourceStream(resourceName);
                                if (stream != null)
                                {
                                    Texture2D texture = Texture2D.FromStream(GraphicsDevice, stream);
                                    FixPNGAlpha(ref texture);
                                    EmbeddedArt.Add(fName, texture);
                                    stream.Dispose();
                                }
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); }
                        }
                    }
                });
        }

        private static void FixPNGAlpha(ref Texture2D texture)
        {
            Color[] buffer = new Color[texture.Width * texture.Height];
            texture.GetData(buffer);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
            texture.SetData(buffer);
        }

        public void ClearArtPixelCache(uint graphic)
        {
            art_textureCache.Remove(graphic);
        }

        public void ClearGumpPixelCache(uint graphic)
        {
            gump_textureCache.Remove(graphic);
        }

        public void ClearAllPixelCaches()
        {
            art_textureCache.Clear();
            gump_textureCache.Clear();
        }
    }
}
