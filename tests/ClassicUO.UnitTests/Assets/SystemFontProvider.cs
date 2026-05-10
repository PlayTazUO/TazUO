using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using Xunit;

namespace ClassicUO.UnitTests.Assets;

public class SystemFontProviderTests
{
    [Fact]
    public void FontsByFamily_Record_Should_Expose_Constructor_Values()
    {
        byte[][] faces =
        [
            [1, 2, 3],
            [4, 5]
        ];

        var family = new FontsByFamily("TestFamily", faces);

        Assert.Equal("TestFamily", family.FamilyName);
        Assert.Same(faces, family.FontFaces);
        Assert.Equal(2, family.FontFaces.Length);
    }
    
    [Fact]
    public void GetSystemFonts_Should_Return_A_Deferred_Enumerable()
    {
        IEnumerable<FontsByFamily> fonts = SystemFontProvider.GetSystemFonts();

        Assert.NotNull(fonts);
        using var enumerator = fonts.GetEnumerator();
        Assert.NotNull(enumerator);
    }
    
    [Fact]
    public void GetSystemFonts_Should_Be_Enumerable_More_Than_Once()
    {
        // We don't assert exact font contents because that depends on the host machine.
        // The important part is that the sequence can be iterated safely.
        IEnumerable<FontsByFamily> fonts = SystemFontProvider.GetSystemFonts();

        // ReSharper disable once PossibleMultipleEnumeration
        var firstPass = fonts.Select(f => (f.FamilyName, FontCount: f.FontFaces.Length)).ToList();

        // ReSharper disable once PossibleMultipleEnumeration
        var secondPass = fonts.Select(f => (f.FamilyName, FontCount: f.FontFaces.Length)).ToList();

        Assert.Equal(firstPass, secondPass);
    }
}
