using System.IO;
using ClassicUO.Game.UI;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class XmlGumpDocumentTests
{
    [Fact]
    public void SaveThenLoad_PreservesRootAttributesStructureAndText()
    {
        var document = new XmlGumpDocument { RootTag = "xmlgump" };
        document.SetRootAttribute("x", "10");
        document.SetRootAttribute("locked", "true");

        XmlGumpElement image = XmlGumpSchema.CreateElement("image");
        image.SetAttribute("id", "500");
        image.SetAttribute("hue", "33");

        XmlGumpElement text = XmlGumpSchema.CreateElement("text");
        text.InnerText = "{hp}/{maxhp}";

        XmlGumpElement container = XmlGumpSchema.CreateElement("control");
        container.SetAttribute("width", "200");
        container.Children.Add(XmlGumpSchema.CreateElement("colorbox"));

        document.Elements.Add(image);
        document.Elements.Add(text);
        document.Elements.Add(container);

        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

        try
        {
            document.Save(path);
            XmlGumpDocument loaded = XmlGumpDocument.Load(path);

            loaded.RootTag.Should().Be("xmlgump");
            loaded.GetRootAttribute("x").Should().Be("10");
            loaded.GetRootAttribute("locked").Should().Be("true");
            loaded.Elements.Should().HaveCount(3);
            loaded.Elements[0].Tag.Should().Be("image");
            loaded.Elements[0].GetAttribute("id").Should().Be("500");
            loaded.Elements[1].InnerText.Should().Be("{hp}/{maxhp}");
            loaded.Elements[2].Children.Should().HaveCount(1);
            loaded.Elements[2].Children[0].Tag.Should().Be("colorbox");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ToXmlStringThenFromString_RoundTrips()
    {
        var document = new XmlGumpDocument();
        document.SetRootAttribute("x", "5");

        XmlGumpElement image = XmlGumpSchema.CreateElement("image");
        image.SetAttribute("id", "42");
        document.Elements.Add(image);

        string xml = document.ToXmlString();
        XmlGumpDocument loaded = XmlGumpDocument.FromString(xml);

        loaded.GetRootAttribute("x").Should().Be("5");
        loaded.Elements.Should().HaveCount(1);
        loaded.Elements[0].GetAttribute("id").Should().Be("42");
    }

    [Fact]
    public void CreateElement_OmitsEmptyDefaultsAndSizesBareElements()
    {
        XmlGumpElement box = XmlGumpSchema.CreateElement("colorbox");
        box.GetAttribute("width").Should().Be("20");
        box.GetAttribute("height").Should().Be("20");

        XmlGumpElement image = XmlGumpSchema.CreateElement("image");
        image.GetAttribute("width").Should().BeNull();
        image.GetAttribute("height").Should().BeNull();
    }

    [Fact]
    public void Clone_IsDeepCopy()
    {
        XmlGumpElement original = XmlGumpSchema.CreateElement("control");
        original.SetAttribute("width", "100");
        original.Children.Add(XmlGumpSchema.CreateElement("text"));

        XmlGumpElement copy = original.Clone();
        copy.SetAttribute("width", "50");
        copy.Children[0].SetAttribute("x", "25");

        original.GetAttribute("width").Should().Be("100");
        original.Children[0].GetAttribute("x").Should().Be("0");
        copy.GetAttribute("width").Should().Be("50");
        copy.Children[0].GetAttribute("x").Should().Be("25");
    }
}
