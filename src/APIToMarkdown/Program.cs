using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class GenDoc
{
    private static bool isMainAPI = false;

    public static Dictionary<string, Tuple<StringBuilder, StringBuilder>> GenerateMarkdown(string filePath)
    {
        Dictionary<string, Tuple<StringBuilder, StringBuilder>> classesDict = new();
        string code = File.ReadAllText(filePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        IEnumerable<ClassDeclarationSyntax> classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (ClassDeclarationSyntax classDeclaration in classes)
        {
            string className = classDeclaration.Identifier.Text;
            isMainAPI = className == "LegionAPI";
            classesDict.TryAdd(className, new Tuple<StringBuilder, StringBuilder>(new StringBuilder(), new StringBuilder()));
            StringBuilder sb = classesDict[className].Item1;
            StringBuilder python = classesDict[className].Item2;

            if (isMainAPI)
                GenUniversalMdHeader(sb);
            GenClassHeader(sb, python, classDeclaration);
            GenClassProperties(sb, python, classDeclaration);
            GenClassFields(sb, python, classDeclaration);
            GenClassEnums(sb, python, classDeclaration);
            GenClassMethods(sb, python, classDeclaration);
        }

        return classesDict;
    }

    private static void GenUniversalMdHeader(StringBuilder sb)
    {
        // Add Starlight frontmatter
        sb.AppendLf("---");
        sb.AppendLf("title: Python API Documentation");
        sb.AppendLf("description: Automatically generated documentation for the Python API scripting system");
        sb.AppendLf("tableOfContents:");
        sb.AppendLf("  minHeadingLevel: 1");
        sb.AppendLf("  maxHeadingLevel: 4");
        sb.AppendLf("---");
        sb.AppendLf();

        sb.AppendLf("This is automatically generated documentation for the Python API scripting.  ");
        sb.AppendLf();

        sb.AppendLf(":::note[Usage]");
        sb.AppendLf("All methods, properties, enums, etc need to pre prefaced with `API.` for example:\n `API.Msg(\"An example\")`.");
        sb.AppendLf(":::");
        sb.AppendLf();

        sb.AppendLf(":::tip[API.py File]");
        sb.AppendLf("If you download the [API.py](https://github.com/PlayTazUO/TazUO/blob/dev/src/ClassicUO.Client/LegionScripting/docs/API.py) file, put it in the same folder as your python scripts and add `import API` to your script, that will enable some mild form of autocomplete in an editor like VS Code.  ");
        sb.AppendLf();
        sb.AppendLf("You can now type `-updateapi` in game to download the latest API.py file.");
        sb.AppendLf(":::");
        sb.AppendLf();

        sb.AppendLf("[Additional notes](../notes/)  ");
        sb.AppendLf();
        sb.AppendLf($"*This was generated on `{DateTime.Now.Date.ToString("M/d/yy")}`.*");
        sb.AppendLf();
    }

    private static void GenClassHeader(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        if (!isMainAPI)
        {
            // Add Starlight frontmatter for non-main API classes
            sb.AppendLf("---");
            sb.AppendLf($"title: {classDeclaration.Identifier.Text}");
            string classSummary = GetXmlSummary(classDeclaration);
            if (!string.IsNullOrEmpty(classSummary))
            {
                sb.AppendLf($"description: {classSummary.Replace('\n', ' ').Replace('\r', ' ')}");
            }
            else
            {
                sb.AppendLf($"description: {classDeclaration.Identifier.Text} class documentation");
            }

            sb.AppendLf("---");
            sb.AppendLf();
        }

        // Add class description section for non-main API
        if (!string.IsNullOrEmpty(GetXmlSummary(classDeclaration)) && !isMainAPI)
        {
            sb.AppendLf("## Class Description");
            sb.AppendLf(GetXmlSummary(classDeclaration));
            sb.AppendLf();
        }

        if (!isMainAPI)
        {
            string baseClasses = string.Empty;
            if (classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Count > 0)
            {
                // Extract base class names and map them to Python types
                var bases = classDeclaration.BaseList.Types
                    .Select(t => MapCSharpTypeToPython(t.Type.ToString(), t.Type.ToString()))
                    .Where(b => !string.IsNullOrEmpty(b))
                    .ToList();

                if (bases.Any())
                    baseClasses = $"({string.Join(", ", bases)})";

            }

            python.AppendLf($"class {classDeclaration.Identifier.Text}{baseClasses}:");
            python.AppendLf("    \"\"");
        }
    }

    private static void GenClassProperties(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        // List properties
        sb.AppendLf("## Properties");
        IEnumerable<PropertyDeclarationSyntax> properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
        if (properties.Any())
        {
            foreach (PropertyDeclarationSyntax property in properties)
            {
                if (!property.Modifiers.Any(SyntaxKind.PublicKeyword))
                    continue;

                string propertySummary = GetXmlSummary(property);
                sb.AppendLf($"### `{property.Identifier.Text}`");
                sb.AppendLf();
                sb.AppendLf($"**Type:** `{property.Type}`");
                sb.AppendLf();

                if (!string.IsNullOrEmpty(propertySummary))
                {
                    sb.AppendLf(propertySummary);
                    sb.AppendLf();
                }

                string space = string.Empty;
                if (!isMainAPI)
                    space = "    ";

                string pyType = MapCSharpTypeToPython(property.Type.ToString(), "");

                if (!string.IsNullOrEmpty(pyType))
                    pyType = ": " + pyType;

                python.AppendLf($"{space}{property.Identifier.Text}{pyType} = None");
            }
        }
        else
        {
            sb.AppendLf("*No properties found.*");
        }

        sb.AppendLf();
    }

    private static void GenClassFields(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        IEnumerable<FieldDeclarationSyntax> fields = classDeclaration.Members.OfType<FieldDeclarationSyntax>();
        if (fields.Any())
        {
            foreach (FieldDeclarationSyntax field in fields)
            {
                TypeSyntax typeSyntax = field.Declaration.Type;
                string typeName = typeSyntax.ToString();
                foreach (VariableDeclaratorSyntax fieldVar in field.Declaration.Variables)
                {
                    if (!field.Modifiers.Any(SyntaxKind.PublicKeyword))
                        continue;

                    if (fieldVar.Identifier.Text == "QueuedPythonActions")
                        continue;

                    string fieldSummary = GetXmlSummary(field);
                    sb.AppendLf($"### `{fieldVar.Identifier.Text}`");
                    sb.AppendLf();
                    sb.AppendLf($"**Type:** `{typeName}`");
                    sb.AppendLf();

                    if (!string.IsNullOrEmpty(fieldSummary))
                    {
                        sb.AppendLf(fieldSummary);
                        sb.AppendLf();
                    }

                    string space = string.Empty;

                    if (!isMainAPI)
                        space = "    ";

                    string pyType = MapCSharpTypeToPython(typeName, "");

                    if (!string.IsNullOrEmpty(pyType))
                        pyType = ": " + pyType;

                    python.AppendLf($"{space}{fieldVar.Identifier.Text}{pyType} = None");
                }
            }
        }
        else
        {
            sb.AppendLf("*No fields found.*");
        }

        sb.AppendLf();
    }

    private static void GenClassEnums(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        // List enums
        sb.AppendLf("## Enums");
        IEnumerable<EnumDeclarationSyntax> enums = classDeclaration.Members.OfType<EnumDeclarationSyntax>();
        if (enums.Any())
        {
            foreach (EnumDeclarationSyntax enumDeclaration in enums)
            {
                if (!enumDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
                    continue;

                string pySpace = isMainAPI ? string.Empty : "    ";

                python.AppendLf();
                python.AppendLf($"{pySpace}class {enumDeclaration.Identifier.Text}:");

                sb.AppendLf($"### {enumDeclaration.Identifier.Text}");
                sb.AppendLf();

                string enumSummary = GetXmlSummary(enumDeclaration);
                if (!string.IsNullOrEmpty(enumSummary))
                {
                    sb.AppendLf(":::note[Description]");
                    sb.AppendLf(enumSummary);
                    sb.AppendLf(":::");
                    sb.AppendLf();
                }

                sb.AppendLf("**Values:**");
                byte last = 0;
                foreach (EnumMemberDeclarationSyntax member in enumDeclaration.Members)
                {
                    sb.AppendLf($"- `{member.Identifier.Text}`");

                    byte value = last += 1;
                    if (member.EqualsValue?.Value.ToString() != null)
                    {
                        if (byte.TryParse(member.EqualsValue?.Value.ToString(), out last))
                            value = last;
                    }

                    python.AppendLf($"{pySpace}    {member.Identifier.Text} = {value}");
                }

                sb.AppendLf();
            }
        }
        else
        {
            sb.AppendLf("*No enums found.*");
        }

        python.AppendLf();
        sb.AppendLf();
    }

    private static void GenClassMethods(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        // List methods
        sb.AppendLf("## Methods");
        IEnumerable<MethodDeclarationSyntax> methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();
        if (methods.Any())
        {
            foreach (MethodDeclarationSyntax method in methods)
            {
                if (!method.Modifiers.Any(SyntaxKind.PublicKeyword))
                    continue;

                string methodSummary = GetXmlSummary(method);

                sb.AppendLf($"### {method.Identifier.Text}");
                GenParametersParenthesis(method.ParameterList.Parameters, ref sb);
                sb.AppendLf();

                if (!string.IsNullOrEmpty(methodSummary))
                {
                    sb.AppendLf(methodSummary);
                    sb.AppendLf();
                }

                GenParameters(method.ParameterList.Parameters, ref sb, method);

                GenReturnType(method.ReturnType, ref sb);

                sb.AppendLf("---");
                sb.AppendLf();

                string pySpace = isMainAPI ? string.Empty : "    ";
                string pyReturn = MapCSharpTypeToPython(method.ReturnType.ToString());

                if (pyReturn != "None")
                    pyReturn = $"\"{pyReturn}\"";

                python.AppendLf($"{pySpace}def {method.Identifier.Text}({GetPythonParameters(method.ParameterList.Parameters, !isMainAPI)})"
                                  + $" -> {pyReturn}:");
                if (!string.IsNullOrWhiteSpace(methodSummary))
                {
                    // Indent and escape triple quotes in summary if present
                    string pyDoc = methodSummary.Replace("\"\"\"", "\\\"\\\"\\\"");
                    string indentedDoc = string.Join("\n", pyDoc.Split('\n').Select(line => $"{pySpace}    " + line.TrimEnd()));
                    python.AppendLf($"{pySpace}    \"\"\"");
                    python.AppendLf(indentedDoc);
                    python.AppendLf($"{pySpace}    \"\"\"");
                }

                python.AppendLf($"{pySpace}    pass");
                python.AppendLf();
            }
        }
        else
        {
            sb.AppendLf("*No methods found.*");
        }
    }

    private static string GetXmlSummary(SyntaxNode node)
    {
        DocumentationCommentTriviaSyntax? trivia = node.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (trivia != null)
        {
            XmlElementSyntax? summary = trivia.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");

            if (summary != null)
            {
                string rawText = string.Join(" ", summary.Content.Select(c => c.ToString().Trim()));

                // 2. Remove any potential leftover XML comment markers and trim ends
                //rawText = rawText.Replace("///", "").Trim();

                // 3. Split by space, remove empty results, join with single space
                //string cleanedText = string.Join(" ", rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                string cleanedDocumentation = Regex.Replace(
                    rawText,
                    @"^\s*(///.*)$", // The pattern to find
                    "$1", // The replacement string (content of group 1)
                    RegexOptions.Multiline // Treat ^ and $ as start/end of LINE
                );

                return cleanedDocumentation.Replace("///", "");
            }
        }

        return string.Empty;
    }

    private static void GenReturnType(TypeSyntax returnType, ref StringBuilder sb)
    {
        if (returnType.ToString() != "void")
        {
            sb.AppendLf($"**Return Type:** `{returnType}`");
        }
        else
        {
            sb.AppendLf("**Return Type:** `void` *(Does not return anything)*");
        }

        sb.AppendLf();
    }

    private static void GenParameters(SeparatedSyntaxList<ParameterSyntax> parameters, ref StringBuilder sb, SyntaxNode methodNode)
    {
        if (parameters.Count == 0) return;

        sb.AppendLf("**Parameters:**");
        sb.AppendLf();
        sb.AppendLf("| Name | Type | Optional | Description |");
        sb.AppendLf("| --- | --- | --- | --- |");

        foreach (ParameterSyntax param in parameters)
        {
            string isOptional = param.Default != null ? "✅ Yes" : "❌ No";
            string paramSummary = GetXmlParamSummary(methodNode, param.Identifier.Text);
            sb.AppendLf($"| `{param.Identifier.Text}` | `{param.Type}` | {isOptional} | {paramSummary} |");
        }

        sb.AppendLf();
    }

    private static string GetXmlParamSummary(SyntaxNode methodNode, string paramName)
    {
        DocumentationCommentTriviaSyntax? trivia = methodNode.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (trivia != null)
        {
            XmlElementSyntax? paramElement = trivia.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "param" &&
                                     e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                                         .Any(a => a.Identifier.Identifier.Text == paramName));

            if (paramElement != null)
            {
                string r = string.Join(" ", paramElement.Content.Select(c => c.ToString().Trim()));
                r = r.Replace("///", "").Trim()
                    .Replace("\n", "  \n");
                return r;
            }
        }

        return string.Empty;
    }

    private static void GenParametersParenthesis(SeparatedSyntaxList<ParameterSyntax> parameters, ref StringBuilder sb)
    {
        if (parameters.Count == 0)
            return;

        sb.Append("`(");

        foreach (ParameterSyntax param in parameters)
        {
            sb.Append($"{param.Identifier.Text}, ");
        }

        sb.Remove(sb.Length - 2, 2);

        sb.Append(")`");
    }

    private static string GetPythonParameters(SeparatedSyntaxList<ParameterSyntax> parameters, bool inClass)
    {
        if (parameters.Count == 0) return inClass ? "self" : string.Empty;

        var sb = new StringBuilder();

        if (inClass)
            sb.Append("self, ");

        foreach (ParameterSyntax param in parameters)
        {
            string pythonType = MapCSharpTypeToPython(param.Type!.ToString());

            if (pythonType != "None")
                    pythonType = $"\"{pythonType}\"";

            string defaultValue = param.Default != null ? $" = {MapDefaultToPython(param.Default.ToString())}" : string.Empty;

            sb.Append($"{param.Identifier.Text}: {pythonType}{defaultValue}, ");
        }

        sb.Remove(sb.Length - 2, 2);

        return sb.ToString();
    }

    private static string MapDefaultToPython(string defaultValue)
    {
        defaultValue = defaultValue.Replace("=", "").Trim();

        if (defaultValue != "false")
            defaultValue = defaultValue.Replace("f", ""); //Remove f suffix from float literals

        // Map C# default values to Python
        return defaultValue.Trim() switch
        {
            "uint.MaxValue" => "1337",
            "ushort.MaxValue" => "1337",
            "int.MinValue" => "1337",
            "true" => "True",
            "false" => "False",
            "null" => "None",
            _ => defaultValue // Keep the original value if not mapped
        };
    }

    private static string MapCSharpTypeToPython(string csharpType, string noMatch = "Any")
    {
        // Trim whitespace just in case
        csharpType = csharpType.Trim();

        if (csharpType == "PythonList")
            return "list";

        // 1. Handle array types (e.g., int[], string[], MyClass[])
        if (csharpType.EndsWith("[]"))
        {
            // Get the element type (e.g., "int" from "int[]")
            string elementType = csharpType.Substring(0, csharpType.Length - 2);
            // Recursively map the element type
            string pythonElementType = MapCSharpTypeToPython(elementType);
            // Use modern Python list hint syntax: list[T]
            return $"list[{pythonElementType}]";
        }

        // 2. Handle common generic collection types (List<T>, IEnumerable<T>, etc.)
        // This uses basic string parsing; more robust parsing might be needed for complex cases.
        string[] collectionPrefixes = { "List<", "IList<", "IEnumerable<", "ICollection<", "Collection<", "System.Collections.Generic.List<", "System.Collections.Generic.IList<", "System.Collections.Generic.IEnumerable<", "System.Collections.Generic.ICollection<", "System.Collections.ObjectModel.Collection<" };

        // Check if the type starts with one of the prefixes and ends with ">"
        string? matchedPrefix = collectionPrefixes.FirstOrDefault(prefix => csharpType.StartsWith(prefix));
        if (matchedPrefix != null && csharpType.EndsWith(">"))
        {
            // Extract the element type T from Collection<T>
            int openBracketIndex = matchedPrefix.Length - 1; // Index of '<'
            int closeBracketIndex = csharpType.Length - 1; // Index of '>'

            if (closeBracketIndex > openBracketIndex)
            {
                string elementType = csharpType.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();
                // Recursively map the element type
                string pythonElementType = MapCSharpTypeToPython(elementType);
                // Use modern Python list hint syntax: list[T]
                return $"list[{pythonElementType}]";
            }
        }

        // 3. Handle Nullable<T> or T?
        if (csharpType.EndsWith("?") || csharpType.StartsWith("Nullable<") || csharpType.StartsWith("System.Nullable<"))
        {
            string underlyingType;
            if (csharpType.EndsWith("?"))
            {
                underlyingType = csharpType.Substring(0, csharpType.Length - 1);
            }
            else // StartsWith("Nullable<") or StartsWith("System.Nullable<")
            {
                int openBracket = csharpType.IndexOf('<');
                int closeBracket = csharpType.LastIndexOf('>');
                if (openBracket != -1 && closeBracket > openBracket)
                {
                    underlyingType = csharpType.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
                }
                else
                {
                    underlyingType = "object"; // Fallback
                }
            }

            string pythonUnderlyingType = MapCSharpTypeToPython(underlyingType);
            // Use Python 3.10+ Union syntax: T | None
            return $"{pythonUnderlyingType} | None";
        }


        // 4. Handle base types (add more as needed)
        // Include fully qualified names if they might appear from ToString()
        return csharpType switch
        {
            "int" or "int?" or "Int32" or "System.Int32" => "int",
            "uint" or "uint?" or "UInt32" or "System.UInt32" => "int", // Map unsigned to int
            "short" or "Int16" or "System.Int16" => "int",
            "ushort" or "UInt16" or "System.UInt16" => "int",
            "long" or "Int64" or "System.Int64" => "int",
            "ulong" or "UInt64" or "System.UInt64" => "int",
            "byte" or "Byte" or "System.Byte" => "int", // Map C# byte to Python int
            "sbyte" or "SByte" or "System.SByte" => "int",
            "string" or "String" or "System.String" => "str",
            "char" or "Char" or "System.Char" => "str", // Map C# char to Python str
            "bool" or "bool?" or "Boolean" or "System.Boolean" => "bool",
            "double" or "Double" or "System.Double" => "float",
            "float" or "Single" or "System.Single" => "float", // C# float is System.Single
            "decimal" or "Decimal" or "System.Decimal" => "float", // Or use Python's Decimal type
            "object" or "Object" or "System.Object" => "Any", // Requires 'from typing import Any'
            "void" or "System.Void" => "None", // Typically for return types

            // Add specific mappings for other common types if desired
            "DateTime" or "System.DateTime" => "datetime", // Requires 'import datetime'
            "Guid" or "System.Guid" => "str", // Often represented as string or UUID

            "Gump" => "ApiUiBaseGump", // Custom types
            "Control" or "ScrollArea" or "SimpleProgressBar" or "TextBox" or "TTFTextInputField" or "GumpPic" => "ApiUiBaseControl",
            "RadioButton" or "NiceButton" or "Button" or "ResizableStaticPic" or "AlphaBlendControl" or "Label" => "ApiUiBaseControl",
            "Checkbox" => "ApiUiCheckbox",
            "Item" or "ApiItem" => "ApiItem",
            "Mobile" or "ApiMobile" => "ApiMobile",
            "Skill" => "Skill",
            "ApiBuff" => "ApiBuff",
            // This type sits outside the ApiClasses namespace - we either have to duplicate it
            // or update the type resolution logic.
            // "BuffIconType" => "BuffIconType",
            "ScanType" => "ScanType",
            "Notoriety" => "Notoriety",
            "GameObject" or "ApiGameObject" => "ApiGameObject",
            "ApiUserProfile" => "ApiUserProfile",
            "ApiUiControlDropDown" => "ApiUiControlDropDown",
            "ApiUiBaseControl" => "ApiUiBaseControl",
            "ApiUiBaseGump" => "ApiUiBaseGump",
            "ApiUiScrollArea" => "ApiUiScrollArea",
            "IList" or "List" => "list",
            "ApiPlayer" => "ApiPlayer",
            "ApiUiGump" => "ApiUiGump",
            "ApiUiLabel" => "ApiUiLabel",
            "ApiUiRadioButton" => "ApiUiRadioButton",
            "ApiUiNiceButton" => "ApiUiNiceButton",
            "ApiUiButton" => "ApiUiButton",
            "ApiUiResizableStaticPic" => "ApiUiResizableStaticPic",
            "ApiUiAlphaBlendControl" => "ApiUiAlphaBlendControl",
            "ApiUiTtfTextInputField" => "ApiUiTtfTextInputField",
            "ApiUiTextBox" => "ApiUiTextBox",
            "ApiUiSimpleProgressBar" => "ApiUiSimpleProgressBar",
            "ApiUiGumpPic" => "ApiUiGumpPic",
            "ApiUiNineSliceGump" => "ApiUiNineSliceGump",
            "ApiUiCheckbox" => "ApiUiCheckbox",
            "EventSinkApi" => "EventSinkApiDeclaration",
            "ApiPoint3D" => "ApiPoint3D",
            "ApiSoundEntry" => "ApiSoundEntry",
            "ApiJournalEntry" => "ApiJournalEntry",
            "ApiEntity" => "ApiEntity",
            "ApiStatic" => "ApiStatic",
            "ApiItemData" => "ApiItemData",
            "ApiUiMenuItem" => "ApiUiMenuItem",
            "ApiMulti" => "ApiMulti",
            "PersistentVar" => "PersistentVar",
            "LegionApiConfig" => "LegionApiConfig",
            "ApiUiTiledGumpPic" => "ApiUiTiledGumpPic",

            // Fallback for unknown types
            _ => noMatch
        };
    }
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
            return;

        string docsDir = args[0];

        string pyFilePath = Path.Combine(docsDir, "API.py");
        if (File.Exists(pyFilePath))
            File.Delete(pyFilePath);

        var files = args.Skip(1).ToList();

        foreach (string? filePath in files)
        {
            Console.WriteLine("Processing file: " + filePath);

            if (string.IsNullOrEmpty(filePath))
                continue;

            if (!File.Exists(filePath))
                continue;

            Dictionary<string, Tuple<StringBuilder, StringBuilder>> gen = GenDoc.GenerateMarkdown(filePath);
            //Console.WriteLine($"Generation complete for [{filePath}].");

            foreach (KeyValuePair<string, Tuple<StringBuilder, StringBuilder>> kvp in gen)
            {
                if (!Directory.Exists(docsDir))
                    Directory.CreateDirectory(docsDir);

                // Normalize to LF line endings to avoid cross-platform conflicts
                string mdContent = kvp.Value.Item1.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
                string pyContent = kvp.Value.Item2.ToString().Replace("\r\n", "\n").Replace("\r", "\n");

                File.WriteAllText(Path.Combine(docsDir, $"{kvp.Key}.md"), mdContent);
                File.AppendAllText(pyFilePath, pyContent);
            }
        }
    }
}

public static class SbExtensions
{
    public static StringBuilder AppendLf(this StringBuilder sb, string? value = "")
    {
        if (value == null)
            value = string.Empty;

        return sb.Append(value).Append('\n');
    }
}

