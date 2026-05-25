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
using System.Buffers.Binary;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Contains a list of embedded fonts available for use in the application.
    /// Note that this list is not exhaustive and may be expanded in the future.
    /// </summary>
    public static class EmbeddedFontNames
    {
        public const string ROBOTO = "Roboto-Regular";
        public const string NOTO_SANS_2_SYMBOLS = "NotoSansSymbols2-Regular";
        public const string ROBOTO_MONO = "Roboto-Mono";
        public const string IBM_PLEX = "ibm-plex";
        public const string WENQUANYI_MICRO_HEI = "wqy-microhei";
    }

    public class TrueTypeLoader
    {
        public const string EMBEDDED_FONT = EmbeddedFontNames.ROBOTO;

        private readonly Dictionary<string, FontSystem> _fonts = new();
        private byte[] _cjkFontBytes;

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

            // Load external fonts, collecting CJK candidate bytes for fallback
            foreach (string ttf in Directory.GetFiles(fontPath, "*.ttf"))
            {
                byte[] fontData = File.ReadAllBytes(ttf);
                var fontSystem = new FontSystem(settings);
                fontSystem.AddFont(fontData);

                string fontName = Path.GetFileNameWithoutExtension(ttf);
                _fonts[fontName] = fontSystem;

                // Use first external TTF as CJK fallback candidate
                if (_cjkFontBytes == null && fontData.Length > 100000)
                    _cjkFontBytes = fontData;
            }

            LoadEmbeddedFonts();

            // Add CJK fallback to ALL font systems
            if (_cjkFontBytes != null)
            {
                foreach (var kvp in _fonts)
                {
                    if (kvp.Key != EmbeddedFontNames.NOTO_SANS_2_SYMBOLS &&
                        kvp.Key != "uo-unicode-1" &&
                        kvp.Key != "wqy-microhei")
                        kvp.Value.AddFont(_cjkFontBytes);
                }
            }
        }

        private void LoadEmbeddedFonts()
        {
            var settings = new FontSystemSettings();

            System.Reflection.Assembly assembly = this.GetType().Assembly;
            string fontAssetFolder = assembly.GetName().Name + ".fonts";
            // Get all embedded resource names
            string[] resourceNames = assembly.GetManifestResourceNames()
                                        .Where(name => name.StartsWith(fontAssetFolder))
                                        .ToArray();

            // First pass: extract CJK font bytes for fallback (only if not already set from external fonts)
            if (_cjkFontBytes == null)
            {

            foreach (string resourceName in resourceNames)
            {
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                string[] rnameParts = resourceName.Split('.');
                string fname = rnameParts[rnameParts.Length - 2];

                if (fname == EmbeddedFontNames.WENQUANYI_MICRO_HEI)
                {
                    using (stream)
                    {
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        byte[] rawBytes = ms.ToArray();

                        // Handle TTC (TrueType Collection) - skip if TTC, use raw if TTF
                        if (rawBytes.Length >= 4 &&
                            rawBytes[0] == 't' && rawBytes[1] == 't' && rawBytes[2] == 'c' && rawBytes[3] == 'f')
                        {
#if DEBUG
                            Log.Warn("CJK font is TTC format - skipping fallback font. Place a .ttf font in the Fonts/ folder for CJK support.");
#endif
                            _cjkFontBytes = null;
                        }
                        else
                        {
                            _cjkFontBytes = rawBytes;
#if DEBUG
                            Log.Trace($"Loaded CJK fallback font: {fname}");
#endif
                        }
                    }

                    break;
                }
            }
            }

            // Second pass: load all fonts, adding CJK fallback to each
            foreach (string resourceName in resourceNames)
            {
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                string[] rnameParts = resourceName.Split('.');
                string fname = rnameParts[rnameParts.Length - 2];
#if DEBUG
                Log.Trace($"Loaded embedded font: {fname}");
#endif
                using (stream)
                {
                    var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);

                    byte[] filebytes = memoryStream.ToArray();

                    // Handle TTC for CJK font itself
                    bool isCjkFont = fname == EmbeddedFontNames.WENQUANYI_MICRO_HEI;
                    if (isCjkFont && _cjkFontBytes != null)
                        filebytes = _cjkFontBytes;
                    else if (isCjkFont)
                        continue; // Skip CJK font if TTC extraction failed

                    if (fname == EMBEDDED_FONT) //Special case for ImGui
                        ImGuiFont = filebytes;

                    var fontSystem = new FontSystem(settings);
                    fontSystem.AddFont(filebytes);

                    // Add CJK fallback to all font systems (except CJK itself and symbols)
                    if (_cjkFontBytes != null && fname != EmbeddedFontNames.WENQUANYI_MICRO_HEI &&
                        fname != EmbeddedFontNames.NOTO_SANS_2_SYMBOLS)
                        fontSystem.AddFont(_cjkFontBytes);

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
