#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace ClassicUO.Game.UI;

/// <summary>
/// One attribute of an <see cref="XmlGumpElement"/>, kept in the order the document declared it so a
/// round trip does not reorder a hand-written file.
/// </summary>
public sealed class XmlGumpAttribute
{
    public string Name { get; set; }
    public string Value { get; set; }

    public XmlGumpAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// A single element of an XML gump: a tag, its attributes in document order, and any children. This is
/// the editable representation the visual editor mutates before serializing, kept deliberately
/// attribute-agnostic so it can carry any tag the runtime parser understands.
/// </summary>
public sealed class XmlGumpElement
{
    public string Tag { get; }

    public List<XmlGumpAttribute> Attributes { get; } = new();

    public List<XmlGumpElement> Children { get; } = new();

    /// <summary>Text content, only meaningful for text elements.</summary>
    public string InnerText { get; set; } = string.Empty;

    public XmlGumpElement(string tag) => Tag = tag;

    /// <summary>The value of an attribute, or null when it is not present.</summary>
    public string? GetAttribute(string name)
    {
        foreach (XmlGumpAttribute attribute in Attributes)
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
                return attribute.Value;

        return null;
    }

    public int GetInt(string name, int fallback = 0) =>
        int.TryParse(GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

    public ushort GetUShort(string name, ushort fallback = 0) =>
        ushort.TryParse(GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort value) ? value : fallback;

    public float GetFloat(string name, float fallback = 0f) =>
        float.TryParse(GetAttribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;

    public bool GetBool(string name, bool fallback = false) =>
        bool.TryParse(GetAttribute(name), out bool value) ? value : fallback;

    /// <summary>Sets an attribute, appending it when new, or removes it when given null.</summary>
    public void SetAttribute(string name, string? value)
    {
        if (value == null)
        {
            RemoveAttribute(name);
            return;
        }

        foreach (XmlGumpAttribute attribute in Attributes)
        {
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                attribute.Value = value;

                return;
            }
        }

        Attributes.Add(new XmlGumpAttribute(name, value));
    }

    public void RemoveAttribute(string name) =>
        Attributes.RemoveAll(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>A deep copy, so a duplicated element shares nothing with its source.</summary>
    public XmlGumpElement Clone()
    {
        var clone = new XmlGumpElement(Tag) { InnerText = InnerText };

        foreach (XmlGumpAttribute attribute in Attributes)
            clone.Attributes.Add(new XmlGumpAttribute(attribute.Name, attribute.Value));

        foreach (XmlGumpElement child in Children)
            clone.Children.Add(child.Clone());

        return clone;
    }
}

/// <summary>
/// The whole editable XML gump: root attributes plus the top-level element list, with load and save to
/// the <c>Data/XmlGumps</c> files the runtime reads.
/// </summary>
public sealed class XmlGumpDocument
{
    public const string DEFAULT_ROOT_TAG = "xmlgump";

    public string RootTag { get; set; } = DEFAULT_ROOT_TAG;
    public string? FilePath { get; set; }
    public List<XmlGumpAttribute> RootAttributes { get; } = new();
    public List<XmlGumpElement> Elements { get; } = new();

    public string? GetRootAttribute(string name)
    {
        foreach (XmlGumpAttribute attribute in RootAttributes)
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
                return attribute.Value;

        return null;
    }

    public void SetRootAttribute(string name, string? value)
    {
        if (value == null)
        {
            RootAttributes.RemoveAll(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

            return;
        }

        foreach (XmlGumpAttribute attribute in RootAttributes)
        {
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                attribute.Value = value;

                return;
            }
        }

        RootAttributes.Add(new XmlGumpAttribute(name, value));
    }

    public static XmlGumpDocument Load(string path) => FromString(File.ReadAllText(path), path);

    /// <summary>Parses a document from its XML text, as used for clipboard import.</summary>
    /// <param name="xml">The XML text.</param>
    /// <param name="path">The file the text came from, if any.</param>
    public static XmlGumpDocument FromString(string xml, string? path = null)
    {
        var document = new XmlGumpDocument { FilePath = path };
        var source = new XmlDocument();
        source.LoadXml(xml);

        XmlElement? root = source.DocumentElement;

        if (root == null)
            return document;

        document.RootTag = root.Name;

        foreach (XmlAttribute attribute in root.Attributes)
            document.RootAttributes.Add(new XmlGumpAttribute(attribute.Name, attribute.Value));

        foreach (XmlNode child in root.ChildNodes)
            if (child is XmlElement element)
                document.Elements.Add(ReadElement(element));

        return document;
    }

    private static XmlGumpElement ReadElement(XmlElement element)
    {
        var model = new XmlGumpElement(element.Name);

        foreach (XmlAttribute attribute in element.Attributes)
            model.Attributes.Add(new XmlGumpAttribute(attribute.Name, attribute.Value));

        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement)
                model.Children.Add(ReadElement(childElement));
            else if (child.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
                model.InnerText += child.Value;
        }

        return model;
    }

    public void Save(string path)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using XmlWriter writer = XmlWriter.Create(path, Settings());

        WriteTo(writer);

        FilePath = path;
    }

    /// <summary>Serializes the document to XML text, as used for clipboard export.</summary>
    public string ToXmlString()
    {
        var buffer = new StringWriter();

        using (XmlWriter writer = XmlWriter.Create(buffer, Settings()))
            WriteTo(writer);

        return buffer.ToString();
    }

    private void WriteTo(XmlWriter writer)
    {
        writer.WriteStartElement(RootTag);

        foreach (XmlGumpAttribute attribute in RootAttributes)
            writer.WriteAttributeString(attribute.Name, attribute.Value);

        foreach (XmlGumpElement element in Elements)
            WriteElement(writer, element);

        writer.WriteEndElement();
        writer.Flush();
    }

    private static XmlWriterSettings Settings() => new()
    {
        Indent = true,
        IndentChars = "  ",
        OmitXmlDeclaration = true,
        NewLineChars = "\n"
    };

    private static void WriteElement(XmlWriter writer, XmlGumpElement element)
    {
        writer.WriteStartElement(element.Tag);

        foreach (XmlGumpAttribute attribute in element.Attributes)
            writer.WriteAttributeString(attribute.Name, attribute.Value);

        if (!string.IsNullOrEmpty(element.InnerText))
            writer.WriteString(element.InnerText);

        foreach (XmlGumpElement child in element.Children)
            WriteElement(writer, child);

        writer.WriteEndElement();
    }
}
