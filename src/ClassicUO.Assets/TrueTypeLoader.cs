#region license

// Copyright (c) 2021, jaedan
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class TrueTypeLoader
    {
        public const string EMBEDDED_FONT = "Roboto-Regular";

        private readonly Dictionary<string, FontSystem> _fonts = new();

        private TrueTypeLoader()
        {
        }

        private static TrueTypeLoader _instance;
        public static TrueTypeLoader Instance => _instance ??= new TrueTypeLoader();

        public byte[] ImGuiFont;

        public void Load()
        {
            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 2,
                KernelWidth = 2,
                KernelHeight = 2
            };

            string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts");

            if (!Directory.Exists(fontPath))
                Directory.CreateDirectory(fontPath);

            foreach (string ttf in Directory.GetFiles(fontPath, "*.ttf"))
            {
                var fontSystem = new FontSystem(settings);
                fontSystem.AddFont(File.ReadAllBytes(ttf));

                _fonts[Path.GetFileNameWithoutExtension(ttf)] = fontSystem;
            }

            LoadEmbeddedFonts();
        }

        private void LoadEmbeddedFonts()
        {
            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 2,
                KernelWidth = 2,
                KernelHeight = 2
            };

            System.Reflection.Assembly assembly = this.GetType().Assembly;
            string fontAssetFolder = assembly.GetName().Name + ".fonts";
            // Get all embedded resource names
            string[] resourceNames = assembly.GetManifestResourceNames()
                                        .Where(name => name.StartsWith(fontAssetFolder))
                                        .ToArray();

            foreach (string resourceName in resourceNames)
            {
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                    using (stream)
                    {
                        string[] rnameParts = resourceName.Split('.');
                        string fname = rnameParts[rnameParts.Length - 2];
#if DEBUG
                        Log.Trace($"Loaded embedded font: {fname}");
#endif
                        var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);

                        byte[] filebytes = memoryStream.ToArray();

                        if (fname == EMBEDDED_FONT) //Special case for ImGui
                            ImGuiFont = filebytes;

                        var fontSystem = new FontSystem(settings);
                        fontSystem.AddFont(filebytes);
                        _fonts[fname] = fontSystem;
                    }
            }
        }

        public SpriteFontBase GetFont(string name, float size)
        {
            if (_fonts.TryGetValue(name, out FontSystem font))
            {
                return font.GetFont(size);
            }

            if (_fonts.Count > 0)
                return _fonts.First().Value.GetFont(size);

            return null;
        }

        public SpriteFontBase GetFont(string name) => GetFont(name, 12);

        public string[] Fonts => _fonts.Keys.ToArray();
    }
}
