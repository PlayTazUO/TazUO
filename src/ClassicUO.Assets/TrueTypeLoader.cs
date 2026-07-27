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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using ClassicUO.IO.Persistency;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Assets;

/// <summary>
///     Contains a list of embedded fonts available for use in the application.
///     Note that this list is not exhaustive and may be expanded in the future.
/// </summary>
public static class EmbeddedFontNames
{
    public const string ROBOTO = "Roboto-Regular";
    public const string ROBOTO_BOLD = "Roboto-Bold";
    public const string ROBOTO_MONO = "Roboto-Mono";
    public const string NOTO_SANS_2_SYMBOLS = "NotoSansSymbols2-Regular";
    public const string IBM_PLEX = "ibm-plex";
    public const string ALAGARD = "alagard";
    public const string AVADONIAN = "avadonian";
    public const string KINGTHINGS_EXETER = "Kingthings Exeter";
    public const string LEAGUE_SPARTAN_BOLD = "LeagueSpartan-Bold";
    public const string UO_UNICODE = "uo-unicode-1";

    /// <summary>
    ///     The names of all embedded fonts
    /// </summary>
    public static FrozenSet<string> Names { get; }

    static EmbeddedFontNames()
    {
        // Effectively a 'const'; Ideally, this entire class would've been a string enum but alas that cannot be done.
        Names = typeof(EmbeddedFontNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(fi => (string)fi.GetRawConstantValue())
            .ToFrozenSet();
    }
}

public class TrueTypeLoader
{
    public const string EMBEDDED_FONT = EmbeddedFontNames.ROBOTO;

    private const uint MAX_NUMBER_OF_SYS_FONT_FAMILIES = 1000;

    private readonly Dictionary<string, FontSystem> _fonts = new();

    private (string[], int)? _orderedFontNames;
    private readonly Lock _orderedFontNamesLock = new();

    /// <summary>
    ///     Contains the names of all available system fonts.
    ///     This can be used to render a font list without loading the fonts into memory
    /// </summary>
    private HashSet<string> _availableSystemFontFamilyNames = [];

    private static TrueTypeLoader _instance;
    public static TrueTypeLoader Instance => _instance ??= new TrueTypeLoader();

    private readonly FontSystemSettings _fontSysSettings = new() { FontResolutionFactor = 2, KernelWidth = 2, KernelHeight = 2 };

    private TrueTypeLoader()
    {
        // Register a global resolver so rich-text '/f[fontName, size]' commands can resolve fonts.
        // Without this, FontStashSharp throws "FontResolver isnt set" whenever such a command is
        // encountered (e.g. a user-typed InfoBar label). Resolving here reuses the normal font
        // lookup, which safely falls back to an embedded font when the requested one is unavailable.
        RichTextDefaults.FontResolver ??= ResolveRichTextFont;
    }

    private ArtLoader _artLoader;
    private GraphicsDevice _graphicsDevice;
    public void SetImageResolver(ArtLoader artLoader, GraphicsDevice graphicsDevice)
    {
        _artLoader = artLoader;
        _graphicsDevice = graphicsDevice;
        RichTextDefaults.ImageResolver ??= ImageResolver;

    }

    private Dictionary<int, Texture2D> _imageCache;
    private IRenderable ImageResolver(string arg)
    {
        if (!Utility.StringHelper.TryParseInt(arg, out int g) || _artLoader == null || _graphicsDevice == null)
            return FSSRenderableTexture.Empty;
        
        _imageCache ??= new();

        if(_imageCache.TryGetValue(g, out Texture2D tex))
            return new TextureFragment(tex);

        ArtInfo info = _artLoader.GetArt((uint)g + 0x4000);

        if (info.Pixels.IsEmpty)
            return FSSRenderableTexture.Empty;
        
        tex = new Texture2D(_graphicsDevice, info.Width, info.Height);
        tex.SetData(info.Pixels.ToArray());

        _imageCache[g] = tex;

        Log.Debug($"Texture set: {info.Width}x{info.Height}");

        return new TextureFragment(tex);
    }

    private class FSSRenderableTexture : IRenderable
    {
        public static readonly FSSRenderableTexture Empty = new FSSRenderableTexture();
        public Point Size => Point.Zero;
        public void Draw(FSRenderContext context, Vector2 position, Color color){}
    }

    /// <summary>
    ///     Resolves a font for a rich-text '/f[...]' command.
    /// </summary>
    /// <param name="parameters">
    ///     The command parameters, expected as '&lt;fontName&gt;' or '&lt;fontName&gt;, &lt;size&gt;'.
    /// </param>
    /// <returns>The requested font, or a fallback font when it cannot be loaded.</returns>
    private SpriteFontBase ResolveRichTextFont(string parameters)
    {
        string fontName = parameters ?? string.Empty;
        float size = 12;

        int commaIndex = fontName.IndexOf(',');
        if (commaIndex >= 0)
        {
            if (float.TryParse(fontName.AsSpan(commaIndex + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedSize))
                size = parsedSize;

            fontName = fontName.Substring(0, commaIndex);
        }

        return GetFont(fontName.Trim(), size);
    }

    public void Load()
    {
        LoadUserFonts();
        LoadEmbeddedFonts();
        BuildSysFontsCache();
    }

    /// <summary>
    ///     Loads user-provided fonts present in the 'Fonts' directory
    /// </summary>
    private void LoadUserFonts()
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts");

        if (!Directory.Exists(fontPath))
            Directory.CreateDirectory(fontPath);

        foreach (string ttf in Directory.GetFiles(fontPath, "*.ttf"))
        {
            try
            {
                var fontSystem = new FontSystem(_fontSysSettings);
                fontSystem.AddFont(File.ReadAllBytes(ttf));

                _fonts[Path.GetFileNameWithoutExtension(ttf)] = fontSystem;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load user font '{ttf}' - {e.Message}");
            }
        }
    }

    /// <summary>
    ///     Greedily attempts to load all available system fonts to determine which ones can be processed
    ///     by FontStashSharp and marks them accordingly in a cache file
    /// </summary>
    /// <remarks>
    ///     This is a sort-of prefetch routine; Some fonts may have valid extensions and be perfectly fine but may not be
    ///     properly loaded by <em>FontStashSharp</em>.
    ///     To allow for a consistent experience when displaying available fonts in the UI, we need to figure out, in advance,
    ///     which ones are usable and which aren't.
    ///     This method does so and stores a 'blacklist' of font families that cannot be loaded.'
    ///     It does *not* attempt to actually keep the fonts loaded in memory.
    ///     The underlying implementation currently resolves only <em>TTF, TTC</em>, and <em>OTF</em> files
    /// </remarks>
    private void BuildSysFontsCache()
    {
        int totalLoaded = 0;
        int familyCount = 0;

        var cacheDefinition = new FontPersistentDefinition();
        FontCacheData cachedData = GetFontCacheData(cacheDefinition);

        if (cachedData.IsCacheFresh)
        {
            Log.Debug("Font cache is fresh, skipping rebuild");
            _availableSystemFontFamilyNames = [..cachedData.Families];
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            Log.Debug("Rebuilding system fonts cache...");
            foreach (FontsByFamily fontFamily in SystemFontProvider.GetSystemFonts())
            {
                totalLoaded += DryLoadSysFontFamily(cachedData, fontFamily);
                ++familyCount;

                // A quick check to keep the cache small and load times manageable
                if (familyCount > MAX_NUMBER_OF_SYS_FONT_FAMILIES)
                {
                    Log.Warn(
                        $"Exceeded maximum number of allowed system font families ({MAX_NUMBER_OF_SYS_FONT_FAMILIES}). Will not load any more system fonts.");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load system fonts - {e.Message}");
        }

        _availableSystemFontFamilyNames = [..cachedData.Families];
        UpdateFontCache(cacheDefinition, cachedData);

        stopwatch.Stop();
        Log.Debug($"Cache build concluded. Processed a total of {totalLoaded} fonts over {familyCount} families in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    ///     Gets the font cache data object
    /// </summary>
    /// <param name="definition">The cache definition to use</param>
    /// <returns>A fully usable <see cref="FontCacheData" /> object with all properties initialized</returns>
    private static FontCacheData GetFontCacheData(FontPersistentDefinition definition)
    {
        // The result of .Get can't actually be null but doesn't hurt to be defensive.
        // The arrays should also be initialized to empty, but just in case.
        FontCacheData data = CacheManager.Instance.Get(definition) ?? new FontCacheData();
        data.Families ??= [];
        data.DoNotLoadFamilies ??= [];
        return data;
    }

    /// <summary>
    ///     Updates the font cache file with the given values.
    ///     Cache update timestamp is automatically updated
    /// </summary>
    /// <param name="cacheDefinition">The cache definition to use</param>
    /// <param name="cacheData">The data to store</param>
    private static void UpdateFontCache(FontPersistentDefinition cacheDefinition, FontCacheData cacheData)
    {
        cacheData.LastUpdated = DateTime.UtcNow;
        if (CacheManager.Instance.Set(cacheDefinition, cacheData))
            Log.Debug("System fonts cache updated");
        else
            Log.WarnDebug("Failed to update system font cache");
    }

    /// <summary>
    ///     Attempts to load a font family (if it is not excluded) to ensure it is valid.
    ///     Updates the cache with the results (either a valid or invalid family name) and returns the number of fonts loaded
    /// </summary>
    /// <remarks>
    ///     This method does not attempt to retain loaded fonts, it simply updates the cache and discards the font system
    ///     afterward.
    /// </remarks>
    /// <param name="cachedData">The cache to update</param>
    /// <param name="fontFamily">The font family to load</param>
    /// <returns>The number of fonts loaded</returns>
    private int DryLoadSysFontFamily(FontCacheData cachedData, FontsByFamily fontFamily)
    {
        if (cachedData.DoNotLoadFamilies.Contains(fontFamily.FamilyName))
        {
            Log.Debug($"Font family '{fontFamily.FamilyName}' is already excluded from loading");
            return 0;
        }

        (int loadedInFamily, FontSystem system) = CreateFontSystemForFamily(fontFamily);

        // We just want to verify everything can be loaded, not actually keep the data.
        system?.Dispose();

        if (loadedInFamily > 0)
            cachedData.Families.Add(fontFamily.FamilyName);
        else
        {
            Log.Warn($"Font family '{fontFamily.FamilyName}' is empty or unavailable. It will be ignored.");
            cachedData.DoNotLoadFamilies.Add(fontFamily.FamilyName);
        }

        return loadedInFamily;
    }

    /// <summary>
    ///     Creates a FontSystem object for a given FontsByFamily object
    /// </summary>
    /// <param name="family">The family to create a <see cref="FontSystem" /> of</param>
    /// <returns>
    ///     A tuple containing the number of fonts loaded and the created <see cref="FontSystem" />
    ///     object, or (0, null) if the family is empty or could not be loaded
    /// </returns>
    private (int LoadedCount, FontSystem FontSys) CreateFontSystemForFamily(FontsByFamily family)
    {
        if (family.FontFaces.Length <= 0)
        {
            Log.Warn($"Could not find any available fonts for family '{family.FamilyName}'");
            return (0, null);
        }

        int numLoadedInSystem = 0;
        var fontSystem = new FontSystem(_fontSysSettings);
        foreach (byte[] font in family.FontFaces)
            try
            {
                fontSystem.AddFont(font);
                numLoadedInSystem++;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load a font binary from family {family.FamilyName} - {e.Message}");
            }

        return (numLoadedInSystem, numLoadedInSystem > 0 ? fontSystem : null);
    }

    /// <summary>
    ///     Loads a font family and returns a FontSystem object if successful
    /// </summary>
    /// <param name="family">The family to load</param>
    /// <returns>
    ///     A <see cref="FontSystem" /> object created for the family or <c>null</c> of the family is empty or could not
    ///     be loaded
    /// </returns>
    private FontSystem LoadAndGetFontByFamily(FontsByFamily family)
    {
        (int loadedInFamily, FontSystem fontSystem) = CreateFontSystemForFamily(family);

        if (loadedInFamily > 0)
        {
            _fonts[family.FamilyName] = fontSystem;
            Log.Debug($"Loaded {loadedInFamily} fonts for family '{family.FamilyName}'");
        }
        else
            Log.Warn($"Could not load any fonts for family '{family.FamilyName}'. The entire family will be ignored");

        return loadedInFamily > 0 ? fontSystem : null;
    }

    /// <summary>
    ///     Loads the fonts embedded into the TUO binary
    /// </summary>
    private void LoadEmbeddedFonts()
    {
        var settings = new FontSystemSettings();

        Assembly assembly = GetType().Assembly;
        string fontAssetFolder = assembly.GetName().Name + ".fonts";
        // Get all embedded resource names
        string[] resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(fontAssetFolder))
            .ToArray();

        foreach (string resourceName in resourceNames)
        {
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                continue;

            using (stream)
            {
                string[] rNameParts = resourceName.Split('.');
                string fName = rNameParts[^2];
#if DEBUG
                Log.Trace($"Loaded embedded font: {fName}");
#endif
                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);

                byte[] fileBytes = memoryStream.ToArray();

                var fontSystem = new FontSystem(settings);
                fontSystem.AddFont(fileBytes);
                _fonts[fName] = fontSystem;
            }
        }
    }

    /// <summary>
    ///     Attempts to get, from disk, a system font by family name
    /// </summary>
    /// <param name="familyName">The font family to obtain</param>
    /// <param name="size">The requested font face size</param>
    /// <param name="font">The loaded font, if successful, otherwise <c>null</c></param>
    /// <returns>True if the font was successfully loaded, otherwise false</returns>
    private bool TryGetSystemFont(string familyName, float size, out SpriteFontBase font)
    {
        FontsByFamily? fontFamily = SystemFontProvider.GetSystemFontFamilyByName(familyName);
        if (fontFamily == null)
        {
            font = null;
            return false;
        }

        FontSystem fontSystem = LoadAndGetFontByFamily(fontFamily.Value);
        font = fontSystem?.GetFont(size);
        return font != null;
    }

    /// <summary>
    ///     Returns a font, specified by name
    /// </summary>
    /// <param name="name">The name of the font to load. For system fonts, this is usually the family name</param>
    /// <param name="size">The size of the font face to return</param>
    /// <returns>
    ///     In order of priority:
    ///     <list type="number">
    ///         <item> The requested font</item>
    ///         <item> The primary TUO embedded font (Roboto)</item>
    ///         <item> The first available loaded font</item>
    ///         <item> null (catastrophic failure)</item>
    ///     </list>
    /// </returns>
    public SpriteFontBase GetFont(string name, float size)
    {
        // A null or empty name can't be looked up (Dictionary.TryGetValue throws on a null key),
        // so skip straight to the fallback font below.
        if (!string.IsNullOrEmpty(name))
        {
            // Try standard fonts first
            if (_fonts.TryGetValue(name, out FontSystem font))
                return font.GetFont(size);

            // If the font isn't present in the loaded ones but is available on the system, try to load it
            if (_availableSystemFontFamilyNames.Contains(name))
            {
                if (TryGetSystemFont(name, size, out SpriteFontBase sysFont))
                    return sysFont;

                // This is to prevent repeated disk hits if a font is botched or otherwise unusable.
                // We can also note in cache that this font family is problematic, but if we've gotten here,
                // it means the initial cache population run concluded that this font is valid.
                Log.Warn($"Could not load system font '{name}'. Family will be ignored");
                _availableSystemFontFamilyNames.Remove(name);
            }
        }

        // Use the default embedded font as a fallback
        if (_fonts.TryGetValue(EmbeddedFontNames.ROBOTO, out FontSystem embeddedFont))
            return embeddedFont.GetFont(size);

        // Otherwise, use the first font we have or give up with a null.
        return _fonts.Count > 0 ? _fonts.First().Value.GetFont(size) : null;
    }

    public SpriteFontBase GetFont(string name) => GetFont(name, 12);

    public string[] Fonts
    {
        get
        {
            // Construct a deduplicated list of font names.
            // This is used only for the options gump so performance is less of a concern here.
            var fontNames = new List<string>(_fonts.Keys.Where(name => !_availableSystemFontFamilyNames.Contains(name)));
            return fontNames.Concat(_availableSystemFontFamilyNames).ToArray();
        }
    }

    /// <summary>
    ///     Retrieves a list of available font names, sorted alphabetically, as well as the longest font name length.
    /// </summary>
    /// <param name="fresh">
    ///     Whether to re-compute the list or used a cache one.
    ///     Computation efficiently is O(3n).
    /// </param>
    /// <returns></returns>
    public (string[] Names, int MaxNameLength) GetSortedFontNames(bool fresh = false)
    {
        // Can be a RW lock if we ever want to eke out a bit more performance
        lock (_orderedFontNamesLock)
        {
            if (!_orderedFontNames.HasValue || fresh)
                _orderedFontNames = GetOrderedFontNames();

            (string[] names, int maxNameLength) = _orderedFontNames.Value;
            return ([.. names], maxNameLength);
        }
    }

    /// <summary>
    ///     Retrieves an ordered collection of font names along with the maximum length of all font names.
    ///     The font names are sorted to prioritize embedded fonts, followed by alphabetical order.
    /// </summary>
    /// <returns>
    ///     A tuple containing:
    ///     <ul>
    ///         <li> An array of ordered font names.</li>
    ///         <li>The maximum length of any font name in the collection.</li>
    ///     </ul>
    /// </returns>
    private (string[] Names, int MaxNameLength) GetOrderedFontNames()
    {
        int maxLength = 0;

        string[] availableFonts = Fonts
            .Select(font =>
            {
                // Keep track of the max name length
                maxLength = Math.Max(maxLength, font.Length);
                return font;
            })
            .OrderBy(font => EmbeddedFontNames.Names.Contains(font) ? 0 : 1) // Embedded fonts should be first in line, ordered by name
            .ThenBy(font => font) // Then, dynamically loaded fonts, ordered by name as well
            .ToArray();

        return (availableFonts, maxLength);
    }
}

/// <summary>
///     Cached data representing available font families and their status
/// </summary>
internal class FontCacheData
{
    public DateTime? LastUpdated { get; set; }
    public HashSet<string> DoNotLoadFamilies { get; set; } = [];
    public HashSet<string> Families { get; set; } = [];

    [JsonIgnore]
    // We can re-build the cache every 30 days for good measure
    public bool IsCacheFresh => LastUpdated != null && DateTime.UtcNow - LastUpdated.Value < TimeSpan.FromDays(30);
}

internal class FontPersistentDefinition : PersistentItemDefinition<CacheType, FontCacheData>
{
    public override CacheType Key => CacheType.Font;
}
