using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static partial class GenDoc
{
    private static bool isMainApi;

    public static Dictionary<string, Tuple<StringBuilder, StringBuilder>> GenerateMarkdown(string filePath)
    {
        Dictionary<string, Tuple<StringBuilder, StringBuilder>> classesDict = new();
        string code = File.ReadAllText(filePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // Nested constant holders are rendered into their parent's page by GenClassConstants, so they
        // must not also become one. A page named for one would be titled after a type a script reaches
        // only through its parent - "Ids" rather than anything that says what it holds. A top-level
        // one has no parent to be rendered into, so it keeps its page rather than vanishing.
        IEnumerable<ClassDeclarationSyntax> classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(declaration => declaration.Parent is not ClassDeclarationSyntax || !IsConstantHolder(declaration));

        foreach (ClassDeclarationSyntax classDeclaration in classes)
        {
            string className = classDeclaration.Identifier.Text;
            isMainApi = className == "LegionAPI";
            classesDict.TryAdd(className, new Tuple<StringBuilder, StringBuilder>(new StringBuilder(), new StringBuilder()));
            StringBuilder sb = classesDict[className].Item1;
            StringBuilder python = classesDict[className].Item2;

            if (isMainApi)
                GenUniversalMdHeader(sb);
            GenClassHeader(sb, python, classDeclaration);
            GenClassProperties(sb, python, classDeclaration);
            GenClassFields(sb, python, classDeclaration);
            GenClassEnums(sb, python, classDeclaration);
            GenClassConstants(sb, python, classDeclaration);
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

        sb.AppendLf();
        sb.AppendLf($"*This was generated on `{DateTime.Now.Date.ToString("M/d/yy")}`.*");
        sb.AppendLf();
    }

    private static void GenClassHeader(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        if (!isMainApi)
        {
            // Add Starlight frontmatter for non-main API classes
            sb.AppendLf("---");
            sb.AppendLf($"title: {classDeclaration.Identifier.Text}");
            string classSummary = GetXmlSummary(classDeclaration);
            if (!string.IsNullOrEmpty(classSummary))
            {
                sb.AppendLf($"description: {YamlScalar(FirstParagraph(classSummary))}");
            }
            else
            {
                sb.AppendLf($"description: {YamlScalar($"{classDeclaration.Identifier.Text} class documentation")}");
            }

            sb.AppendLf("---");
            sb.AppendLf();
        }

        // Add class description section for non-main API
        if (!string.IsNullOrEmpty(GetXmlSummary(classDeclaration)) && !isMainApi)
        {
            sb.AppendLf("## Class Description");
            sb.AppendLf(GetXmlSummary(classDeclaration));
            sb.AppendLf();
        }

        if (!isMainApi)
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
                if (!isMainApi)
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

                    if (!isMainApi)
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

                string pySpace = isMainApi ? string.Empty : "    ";

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

    /// <summary>
    /// Whether a class exists only to name constants, like an enum that has to carry strings.
    /// <para>
    /// These read best as a section of whatever they hang off - a script reaches them only through
    /// the parent, so a page of their own would be titled after a name that means nothing alone.
    /// </para>
    /// </summary>
    /// <param name="classDeclaration">The class to judge.</param>
    /// <returns>True where every member is a constant and there is at least one.</returns>
    private static bool IsConstantHolder(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members.Count > 0
        && classDeclaration.Members.All(member =>
            member is FieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.ConstKeyword)
        );

    /// <summary>
    /// Renders the constant holders nested in a class, each as its own section, mirroring how enums
    /// are rendered rather than given pages.
    /// </summary>
    /// <param name="sb">Page being built.</param>
    /// <param name="python">Stub file being built.</param>
    /// <param name="classDeclaration">The owning class.</param>
    private static void GenClassConstants(StringBuilder sb, StringBuilder python, ClassDeclarationSyntax classDeclaration)
    {
        var holders = classDeclaration.Members
            .OfType<ClassDeclarationSyntax>()
            .Where(nested => nested.Modifiers.Any(SyntaxKind.PublicKeyword) && IsConstantHolder(nested))
            .ToList();

        if (holders.Count == 0)
            return;

        sb.AppendLf("## Constants");

        foreach (ClassDeclarationSyntax holder in holders)
        {
            string pySpace = isMainApi ? string.Empty : "    ";

            python.AppendLf();
            python.AppendLf($"{pySpace}class {holder.Identifier.Text}:");

            sb.AppendLf($"### {holder.Identifier.Text}");
            sb.AppendLf();

            string holderSummary = GetXmlSummary(holder);

            if (!string.IsNullOrEmpty(holderSummary))
            {
                sb.AppendLf(":::note[Description]");
                sb.AppendLf(holderSummary);
                sb.AppendLf(":::");
                sb.AppendLf();
            }

            // Name and value both: a constant's value is its whole point, and a reader comparing one
            // against something the client reported back has nothing to compare without it.
            sb.AppendLf("| Name | Value | Description |");
            sb.AppendLf("| --- | --- | --- |");

            foreach (FieldDeclarationSyntax field in holder.Members.OfType<FieldDeclarationSyntax>())
            {
                // Inline form: a table row is one line, and CleanDocumentation keeps the newlines a
                // wrapped summary carries - one of those mid-row terminates the table.
                string fieldSummary = GetInlineXmlSummary(field);

                foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
                {
                    string value = declarator.Initializer?.Value.ToString() ?? string.Empty;

                    sb.AppendLf($"| `{declarator.Identifier.Text}` | `{value.Trim('"')}` | {fieldSummary} |");
                    python.AppendLf($"{pySpace}    {declarator.Identifier.Text} = {value}");
                }
            }

            sb.AppendLf();
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

                GenReturnType(method.ReturnType, ref sb, method);
                GenExceptions(sb, method);

                sb.AppendLf("---");
                sb.AppendLf();

                string pySpace = isMainApi ? string.Empty : "    ";
                string pyReturn = MapCSharpTypeToPython(method.ReturnType.ToString());

                if (pyReturn != "None")
                    pyReturn = $"\"{pyReturn}\"";

                python.AppendLf($"{pySpace}def {method.Identifier.Text}({GetPythonParameters(method.ParameterList.Parameters, !isMainApi)})"
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

    /// <summary>A member's summary as a Markdown block, line breaks preserved.</summary>
    /// <param name="node">The declaration to read the doc comment off.</param>
    /// <returns>The Markdown, or empty if the member has no summary.</returns>
    private static string GetXmlSummary(SyntaxNode node)
    {
        XmlElementSyntax? summary = SummaryElement(node);
        return summary == null ? string.Empty : CleanDocumentation(TagText(summary));
    }

    /// <summary>A member's summary as a single line, for a table cell or list item.</summary>
    /// <param name="node">The declaration to read the doc comment off.</param>
    /// <returns>The Markdown on one line, or empty if the member has no summary.</returns>
    private static string GetInlineXmlSummary(SyntaxNode node)
    {
        XmlElementSyntax? summary = SummaryElement(node);
        return summary == null ? string.Empty : CleanInline(summary);
    }

    /// <summary>The `&lt;summary&gt;` tag of a declaration's doc comment, if it has one.</summary>
    /// <param name="node">The declaration to read the doc comment off.</param>
    /// <returns>The element, or null if the member is undocumented.</returns>
    private static XmlElementSyntax? SummaryElement(SyntaxNode node)
    {
        DocumentationCommentTriviaSyntax? trivia = node.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        return trivia?.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");
    }

    /// <summary>Joined text of a doc element's content, before cleaning.</summary>
    /// <param name="element">The doc element.</param>
    /// <returns>The raw text.</returns>
    private static string TagText(XmlElementSyntax element) =>
        string.Join(" ", element.Content.Select(c => c.ToString().Trim()));

    /// <summary>
    /// A doc tag rendered as one line, for the values that sit inline rather than as a body: a
    /// wrapped source comment would otherwise carry its line breaks into a table cell or list item.
    /// </summary>
    /// <param name="element">The doc element.</param>
    /// <returns>The Markdown, on a single line.</returns>
    private static string CleanInline(XmlElementSyntax element)
    {
        string cleaned = CleanDocumentation(TagText(element));

        // A deliberate break survives as <br>, since a table cell cannot hold a newline and running
        // the two halves together would lose the author's point. Soft wrapping collapses to spaces.
        cleaned = cleaned.Replace("\n\n", "<br>");

        return WhitespaceRx().Replace(cleaned, " ").Trim();
    }

    /// <summary>
    /// Turns the raw text of a doc tag into Markdown: strips the comment markers, and renders the
    /// XMLDoc elements that have no Markdown equivalent rather than letting them through as literal
    /// tags.
    /// </summary>
    /// <param name="rawText">Joined content of a doc element.</param>
    /// <returns>The Markdown.</returns>
    private static string CleanDocumentation(string rawText)
    {
        string cleanedDocumentation = XmlDocPrefixRx().Replace(rawText, "$1");
        cleanedDocumentation = CodeBlockRx().Replace(cleanedDocumentation, "`$1`");

        // Before the asides, so a <br/> inside one is already a break by the time its body is laid
        // out. Markdown has no single-line break worth the ambiguity, so one reads as a paragraph.
        cleanedDocumentation = BreakRx().Replace(cleanedDocumentation, "\n\n");

        // Before <para>, so an aside's own body still gets its paragraph breaks.
        cleanedDocumentation = AsideRx().Replace(cleanedDocumentation, RenderAside);

        // <para> has no Markdown equivalent, so it reaches the page as an unknown element and the
        // paragraph break it stands for is lost. Both halves become one blank line.
        cleanedDocumentation = ParaRx().Replace(cleanedDocumentation, "\n\n");

        // Cross-references carry the only noun in sentences like "inherits from <see cref="X"/>",
        // so they have to become that name - dropping the tag would leave the sentence dangling.
        cleanedDocumentation = ReferenceRx().Replace(cleanedDocumentation, match => $"`{match.Groups[1].Value}`");

        // A tag is its own syntax node, so the text after it was joined on with a space - leaving
        // "inherits from `ApiGameObject` ." wherever an author wrote the punctuation tight against
        // the tag. Only whitespace is removed, and only between a span and the mark that follows it.
        cleanedDocumentation = SpanPunctuationRx().Replace(cleanedDocumentation, "$1");

        // Dropping the marker alone leaves the space that followed it, indenting every wrapped line
        // by one. Four such lines read as a code block, and one before a fence breaks the fence.
        cleanedDocumentation = DocMarkerRx().Replace(cleanedDocumentation, string.Empty);

        // A line left holding only the space that followed its marker is not blank, so it would
        // survive the run collapse below and show as an indented empty paragraph.
        cleanedDocumentation = BlankLineRx().Replace(cleanedDocumentation, string.Empty);

        // Consecutive </para><para> each yield their own pair of breaks, so the run adds up.
        cleanedDocumentation = BlankRunRx().Replace(cleanedDocumentation, "\n\n");

        // Authors indent doc comments to match the tags they sit inside, and four spaces of that is
        // a Markdown code block. Run after the breaks are settled, so each paragraph is dedented
        // against itself and a block indented more deeply than its neighbours still comes out flat.
        cleanedDocumentation = Dedent(cleanedDocumentation);

        // A fence on the line directly after prose is not a fence; Markdown needs the blank line.
        // Same for the "Example:" label authors put immediately above one.
        cleanedDocumentation = ExampleLabelRx().Replace(cleanedDocumentation, "\n\n**Example:**");
        cleanedDocumentation = SeparateOpeningFences(cleanedDocumentation);

        return BlankRunRx().Replace(cleanedDocumentation, "\n\n").Trim();
    }

    /// <summary>
    /// Renders one authored aside as a Starlight aside. The four kinds map onto Starlight's own, so
    /// a page gets its native callout styling rather than a bold run of text in the body.
    /// </summary>
    /// <param name="match">The matched aside element.</param>
    /// <returns>The directive block.</returns>
    private static string RenderAside(Match match)
    {
        string kind = match.Groups["tag"].Value.ToLowerInvariant() switch
        {
            "tip" => "tip",
            "warn" => "caution",
            "danger" => "danger",
            _ => "note"
        };

        string title = match.Groups["title"].Success ? $"[{match.Groups["title"].Value}]" : string.Empty;

        // Markers come off here rather than in the usual pass: the body's last line is a bare "///"
        // that would otherwise survive the trim and leave a blank line above the closing fence.
        // Dedented here too, because an aside's body is usually indented further than the prose
        // around it, so the shared pass would measure the two together and flatten neither.
        string body = DocMarkerRx().Replace(match.Groups["body"].Value, string.Empty);

        body = Dedent(BlankRunRx().Replace(BlankLineRx().Replace(body, string.Empty), "\n\n")).Trim();

        // Padded with blank lines because a directive fence has to own its line, and the author
        // wrote the tag mid-paragraph.
        return $"\n\n:::{kind}{title}\n{body}\n:::\n\n";
    }

    /// <summary>
    /// Removes the indentation an author gave their doc comment, one paragraph at a time.
    /// <para>
    /// Per paragraph rather than over the whole text, so a block indented more deeply than its
    /// neighbours - an aside's body beside unindented prose - is still flattened. Each block is
    /// shifted by one amount, so indentation <em>within</em> it survives: a fenced code sample keeps
    /// its own shape, and the fence markers are measured along with the code, so a sample indented
    /// with its comment comes out aligned rather than mangled.
    /// </para>
    /// </summary>
    /// <param name="text">Cleaned documentation, with blank lines already settled.</param>
    /// <returns>The text, flattened.</returns>
    private static string Dedent(string text)
    {
        string[] blocks = text.Split("\n\n");
        List<string> dedented = new(blocks.Length);
        int next = 0;

        while (next < blocks.Length)
        {
            string block = blocks[next++];

            // A blank line inside a sample belongs to the sample, so the paragraph split cut the
            // block mid-fence. Rejoined until the fence closes, or the half without the fence
            // markers would be measured - and shifted - against its own deepest-indented line.
            while (next < blocks.Length && FenceCount(block) % 2 != 0)
                block = $"{block}\n\n{blocks[next++]}";

            dedented.Add(DedentBlock(block));
        }

        return string.Join("\n\n", dedented);
    }

    /// <summary>How many fence markers a block holds, to tell an open sample from a closed one.</summary>
    /// <param name="block">The paragraph to count over.</param>
    /// <returns>The number of fence lines.</returns>
    private static int FenceCount(string block)
    {
        int count = 0;

        foreach (string line in block.Split('\n'))
        {
            if (IsFence(line))
                count++;
        }

        return count;
    }

    /// <summary>Whether a line is a code fence, indentation aside.</summary>
    /// <param name="line">The line to test.</param>
    /// <returns>True when the line opens or closes a fenced block.</returns>
    private static bool IsFence(string line) => line.AsSpan().TrimStart(" \t").StartsWith("```");

    /// <summary>
    /// Puts the blank line Markdown needs above every opening fence, which an author writing a
    /// sample directly under their prose does not.
    /// <para>
    /// Fence state is tracked rather than matched line by line, because a closing fence looks the
    /// same as an opening one: a blank line inserted above that one lands <em>inside</em> the code
    /// block and shows as a trailing empty line in every sample on the page.
    /// </para>
    /// </summary>
    /// <param name="text">Cleaned documentation, already dedented.</param>
    /// <returns>The text, with its fences separated from the prose above them.</returns>
    private static string SeparateOpeningFences(string text)
    {
        List<string> lines = [..text.Split('\n')];
        bool insideFence = false;

        for (int i = 0; i < lines.Count; i++)
        {
            if (!IsFence(lines[i]))
                continue;

            // Four spaces of indentation is itself a code block, and the markers were measured
            // along with the sample's own lines, so whatever is left of theirs comes off here.
            lines[i] = lines[i].TrimStart(' ', '\t');

            if (!insideFence && i > 0 && lines[i - 1].Trim().Length > 0)
            {
                lines[i - 1] = lines[i - 1].TrimEnd(' ', '\t');
                lines.Insert(i, string.Empty);
                i++;
            }

            insideFence = !insideFence;
        }

        return string.Join('\n', lines);
    }

    /// <summary>Shifts one paragraph left by the indentation its least-indented line carries.</summary>
    /// <param name="block">The paragraph.</param>
    /// <returns>The paragraph, flattened.</returns>
    private static string DedentBlock(string block)
    {
        string[] lines = block.Split('\n');
        int shared = int.MaxValue;

        foreach (string line in lines)
        {
            if (line.Trim().Length == 0)
                continue;

            shared = Math.Min(shared, line.Length - line.TrimStart(' ', '\t').Length);
        }

        if (shared is 0 or int.MaxValue)
            return block;

        for (int i = 0; i < lines.Length; i++)
            lines[i] = lines[i].Length >= shared ? lines[i][shared..] : string.Empty;

        return string.Join("\n", lines);
    }

    /// <summary>
    /// First paragraph of a summary, for the frontmatter description. The whole summary would put
    /// every caveat and code sample into a meta description and a sidebar card.
    /// </summary>
    /// <param name="summary">The cleaned summary.</param>
    /// <returns>The opening paragraph.</returns>
    private static string FirstParagraph(string summary)
    {
        // Asides come out whole before splitting, not skipped paragraph by paragraph: one carries
        // its own blank lines, so a split would hand back the middle of a callout as the summary.
        string prose = AsideBlockRx().Replace(summary, string.Empty);

        foreach (string paragraph in prose.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = paragraph.Trim();

            if (trimmed.Length > 0)
                return trimmed;
        }

        return string.Empty;
    }

    /// <summary>
    /// Renders a doc summary as a YAML frontmatter value.
    /// <para>
    /// Always quoted. An unquoted scalar breaks the whole page the moment a summary contains ": ",
    /// which YAML reads as a nested mapping - and the site build fails on the file rather than
    /// degrading, so one colon in one C# doc comment takes the documentation down.
    /// </para>
    /// </summary>
    /// <param name="summary">The summary text, possibly spanning lines.</param>
    /// <returns>A double-quoted single-line scalar.</returns>
    private static string YamlScalar(string summary)
    {
        // Any markup left over (<em>, <see cref="..."/>) would show as itself in a meta
        // description and in search results, where there is nothing to render it.
        string flattened = XmlTagRx().Replace(summary, string.Empty);
        flattened = WhitespaceRx().Replace(flattened, " ").Trim();

        return $"\"{flattened.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    /// <summary>
    /// Renders the return line, with whatever the <c>&lt;returns&gt;</c> tag says about the value.
    /// The type alone answers what comes back but not what it means - and for the APIs that report
    /// failure in their return value, the prose is the only place that is written down.
    /// </summary>
    /// <param name="returnType">The declared return type.</param>
    /// <param name="sb">Page being built.</param>
    /// <param name="methodNode">The method, for its doc comment.</param>
    private static void GenReturnType(TypeSyntax returnType, ref StringBuilder sb, SyntaxNode methodNode)
    {
        if (returnType.ToString() != "void")
        {
            string returns = GetXmlTagText(methodNode, "returns");

            sb.AppendLf(
                string.IsNullOrEmpty(returns)
                    ? $"**Return Type:** `{returnType}`"
                    : $"**Return Type:** `{returnType}` - {returns}"
            );
        }
        else
        {
            sb.AppendLf("**Return Type:** `void` *(Does not return anything)*");
        }

        sb.AppendLf();
    }

    /// <summary>
    /// Renders what a method throws. Worth a section of its own rather than a line in the summary:
    /// a script cannot see a signature, so an undocumented throw is one a script author meets at
    /// runtime or not at all.
    /// </summary>
    /// <param name="sb">Page being built.</param>
    /// <param name="methodNode">The method, for its doc comment.</param>
    private static void GenExceptions(StringBuilder sb, SyntaxNode methodNode)
    {
        DocumentationCommentTriviaSyntax? trivia = methodNode.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (trivia == null)
            return;

        var thrown = trivia.Content
            .OfType<XmlElementSyntax>()
            .Where(element => element.StartTag.Name.LocalName.Text == "exception")
            .ToList();

        if (thrown.Count == 0)
            return;

        sb.AppendLf("**Raises:**");
        sb.AppendLf();

        foreach (XmlElementSyntax element in thrown)
        {
            // The cref is the exception's own name, which is what a script sees when it catches one.
            string name = element.StartTag.Attributes
                .OfType<XmlCrefAttributeSyntax>()
                .Select(attribute => attribute.Cref.ToString())
                .FirstOrDefault() ?? "Exception";

            string reason = CleanInline(element);

            sb.AppendLf($"- `{name}` - {reason}");
        }

        sb.AppendLf();
    }

    /// <summary>
    /// Text of a single-occurrence doc tag, cleaned the same way a summary is.
    /// </summary>
    /// <param name="node">The documented member.</param>
    /// <param name="tag">Local name of the tag, without angle brackets.</param>
    /// <returns>The text, or an empty string where the tag is absent.</returns>
    private static string GetXmlTagText(SyntaxNode node, string tag)
    {
        DocumentationCommentTriviaSyntax? trivia = node.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        XmlElementSyntax? element = trivia?.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == tag);

        if (element == null)
            return string.Empty;

        return CleanInline(element);
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

        XmlElementSyntax? paramElement = trivia?.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e =>
                e.StartTag.Name.LocalName.Text == "param" &&
                e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                    .Any(a => a.Identifier.Identifier.Text == paramName)
            );

        return paramElement == null ? string.Empty : CleanInline(paramElement);
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

        // Remove the 'f' suffix from float literals (e.g. "2f"), but leave every other 'f' intact.
        if (defaultValue.Length > 1 &&
            (defaultValue[^1] == 'f' || defaultValue[^1] == 'F') &&
            char.IsDigit(defaultValue[^2]))
            defaultValue = defaultValue[..^1];

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
            "EffectType" => "EffectType",
            "EffectBlendMode" => "EffectBlendMode",
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
            "ApiUiSlider" => "ApiUiSlider",
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

            // Visual effects
            "ApiVisualEffects" => "ApiVisualEffects",
            "ApiVector2" => "ApiVector2",
            "ApiEffectProfile" => "ApiEffectProfile",
            "ApiEffectProfileInfo" => "ApiEffectProfileInfo",
            "ApiProfileLayer" => "ApiProfileLayer",
            "ApiLayerEffect" => "ApiLayerEffect",
            "ApiTintLayer" => "ApiTintLayer",
            "ApiBlurLayer" => "ApiBlurLayer",
            "ApiRadialBlurLayer" => "ApiRadialBlurLayer",
            "ApiChromaticLayer" => "ApiChromaticLayer",
            "ApiShapeSpec" => "ApiShapeSpec",
            "ApiJitterSpec" => "ApiJitterSpec",
            "ApiNoiseSpec" => "ApiNoiseSpec",
            "ApiPulseSpec" => "ApiPulseSpec",
            "ApiFadeSpec" => "ApiFadeSpec",
            "ApiShakeSpec" => "ApiShakeSpec",
            "ApiShakeConfig" => "ApiShakeConfig",

            // Fallback for unknown types
            _ => noMatch
        };
    }

    [GeneratedRegex(@"<c(?:ode)?\>\s*(.*?)\s*<\/c(?:ode)?>", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRx();

    [GeneratedRegex(@"[ \t]*</?para\s*/?>[ \t]*", RegexOptions.IgnoreCase)]
    private static partial Regex ParaRx();

    /// <summary>
    /// A cross-reference tag, capturing what it names: <c>see cref</c>, <c>seealso cref</c>,
    /// <c>see langword</c> and <c>paramref name</c>. The optional <c>T:</c>-style prefix Roslyn
    /// accepts is excluded from the capture.
    /// </summary>
    [GeneratedRegex("""<(?:see(?:also)?\s+(?:cref|langword)|paramref\s+name)\s*=\s*"(?:[A-Za-z]:)?([^"]+)"\s*/?>""")]
    private static partial Regex ReferenceRx();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRx();

    /// <summary>
    /// An inline code span followed by horizontal whitespace and a punctuation mark. Confined to one
    /// line and to spans with no backtick inside, so it cannot reach across a fenced sample.
    /// </summary>
    [GeneratedRegex(@"(`[^`\n]+`)[ \t]+(?=[.,;:!?)])")]
    private static partial Regex SpanPunctuationRx();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRx();

    /// <summary>
    /// An authored callout: <c>&lt;note&gt;</c>, <c>&lt;tip&gt;</c>, <c>&lt;warn&gt;</c> or
    /// <c>&lt;danger&gt;</c>, each taking an optional <c>title</c>. Not XMLDoc elements - the
    /// compiler ignores tags it does not know, which is what lets the docs define their own.
    /// </summary>
    [GeneratedRegex(
        """<(?<tag>note|tip|warn|danger)(?:\s+title\s*=\s*"(?<title>[^"]*)")?\s*>(?<body>.*?)</\k<tag>\s*>""",
        RegexOptions.Singleline | RegexOptions.IgnoreCase
    )]
    private static partial Regex AsideRx();

    /// <summary>The comment marker and the single space authors put after it.</summary>
    [GeneratedRegex(@"///[ \t]?")]
    private static partial Regex DocMarkerRx();

    /// <summary>
    /// An explicit break, <c>&lt;br&gt;</c> or <c>&lt;br/&gt;</c>. Reads as a paragraph break:
    /// Markdown's single-line break is two trailing spaces, which no author would mean to type and
    /// no reviewer could see.
    /// </summary>
    [GeneratedRegex(@"[ \t]*<br\s*/?>[ \t]*", RegexOptions.IgnoreCase)]
    private static partial Regex BreakRx();

    /// <summary>Three or more line breaks, which Markdown renders the same as two.</summary>
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankRunRx();

    /// <summary>A line holding nothing but whitespace.</summary>
    [GeneratedRegex(@"^[ \t]+$", RegexOptions.Multiline)]
    private static partial Regex BlankLineRx();

    /// <summary>A rendered aside, open fence to close fence, including the lines between.</summary>
    [GeneratedRegex(@"^:::.*?^:::[ \t]*$", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex AsideBlockRx();

    /// <summary>The bare "Example:" label authors write above a sample.</summary>
    [GeneratedRegex(@"(?:(?<=\S)[ \t]*\n[ \t]*|^[ \t]*)Example:[ \t]*(?=\n)", RegexOptions.Multiline)]
    private static partial Regex ExampleLabelRx();

    [GeneratedRegex(@"^\s*(///.*)$", RegexOptions.Multiline // Treat ^ and $ as start/end of LINE
    )]
    private static partial Regex XmlDocPrefixRx();
}

class Program
{
    /// <summary>Folder the API classes are grouped under, and the anchor page folders are relative to.</summary>
    private const string ApiClassesFolder = "ApiClasses";

    /// <summary>
    /// Published folder name per source folder under <see cref="ApiClassesFolder"/>, where the two
    /// should differ. Anything absent publishes under its own name, and a class sitting directly in
    /// <see cref="ApiClassesFolder"/> publishes at the root.
    /// <para>
    /// The website copies the docs directory wholesale into its content collection and lets
    /// Starlight autogenerate the sidebar from it, so these names are published URL segments and
    /// sidebar group labels - which is why they read as prose rather than as C# folder names.
    /// Renaming one moves every page under it. No "API" in a label - everything here is the API, so
    /// the word would only repeat the section it sits in.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> PublishedFolders = new()
    {
        ["VisualEffects"] = "Visual Effects"
    };

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

            string sourceFolder = PublishedFolder(filePath);

            foreach (KeyValuePair<string, Tuple<StringBuilder, StringBuilder>> kvp in gen)
            {
                string pageDir = Path.Combine(docsDir, sourceFolder);

                if (!Directory.Exists(pageDir))
                    Directory.CreateDirectory(pageDir);

                // Normalize to LF line endings to avoid cross-platform conflicts
                string mdContent = kvp.Value.Item1.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
                string pyContent = kvp.Value.Item2.ToString().Replace("\r\n", "\n").Replace("\r", "\n");

                File.WriteAllText(Path.Combine(pageDir, $"{kvp.Key}.md"), mdContent);
                File.AppendAllText(pyFilePath, pyContent);
            }
        }
    }

    /// <summary>
    /// Where a source file's pages are published, relative to the docs directory.
    /// <para>
    /// Derived from the source tree rather than from a list of class names, so grouping a new class
    /// is a matter of putting its file in the right folder - a name list would silently publish
    /// anything nobody remembered to add to it at the root.
    /// </para>
    /// </summary>
    /// <param name="filePath">The source file being documented.</param>
    /// <returns>The folder, or an empty string for the docs root.</returns>
    private static string PublishedFolder(string filePath)
    {
        string[] segments = Path.GetFullPath(filePath)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        int anchor = Array.LastIndexOf(segments, ApiClassesFolder);

        // Not under ApiClasses at all (LegionAPI.cs, the generated event declarations), or sitting
        // directly in it - either way the page belongs at the root, which is where it has always
        // been published from.
        if (anchor < 0 || anchor >= segments.Length - 2)
            return string.Empty;

        IEnumerable<string> folders = segments[(anchor + 1)..^1]
            .Select(segment => PublishedFolders.GetValueOrDefault(segment, segment));

        return Path.Combine(folders.ToArray());
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

