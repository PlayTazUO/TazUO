using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class PNGLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private string exePath;

        public Dictionary<string, Texture2D> EmbeddedArt = new Dictionary<string, Texture2D>();

        private uint[] gump_availableIDs;
        private Dictionary<uint, (uint[] pixels, int width, int height)> gump_textureCache = new Dictionary<uint, (uint[], int, int)>();

        private uint[] art_availableIDs;
        private Dictionary<uint, (uint[] pixels, int width, int height)> art_textureCache = new Dictionary<uint, (uint[], int, int)>();

        public GraphicsDevice GraphicsDevice { set; get; }

        public static PNGLoader _instance;
        public static PNGLoader Instance => _instance ?? (_instance = new PNGLoader());

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
            if (gump_availableIDs == null)
                return new GumpInfo();
            int index = Array.IndexOf(gump_availableIDs, graphic);
            if (index == -1) return new GumpInfo();

            if (gump_textureCache.TryGetValue(graphic, out var cached))
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
                    Texture2D tempTexture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    titleStream.Close();
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
            if (index == -1) return new ArtInfo();

            if (art_textureCache.TryGetValue(graphic, out var cached))
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

            Color[] pixelColors = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(pixelColors);

            uint[] pixels = new uint[pixelColors.Length];
            for (int i = 0; i < pixelColors.Length; i++)
            {
                pixels[i] = pixelColors[i].PackedValue;
            }

            return pixels;
        }

        public Task Load()
        {
            return Task.Run(
                () =>
                {
                    string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    exePath = Path.GetDirectoryName(strExeFilePath);

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
                });
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
                                if (gump_availableIDs != null)
                                {
                                    uint[] availableIDs = new uint[gump_availableIDs.Length + 1];
                                    gump_availableIDs.CopyTo(availableIDs, 0);
                                    availableIDs[availableIDs.Length - 1] = i;
                                    gump_availableIDs = availableIDs;
                                }
                                else
                                {
                                    gump_availableIDs = [i];
                                }

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
