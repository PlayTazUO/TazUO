using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HandlebarsDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventGenerator;

// An alias for an event grouping by class name and namespace
using ClassMethodsGroup = IGrouping<(string Namespace, string ClassName), ApiEventInfo>;

[Generator]
public class EventSourceGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, INamedTypeSymbol> _qualifiedTypeToSymbol = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find methods with the attribute
        var methodsWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => IsAnnotatedEventDecl(s),
                static (ctx, _) => GetApiEventFromEventDeclNode(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndMethods = context.CompilationProvider.Combine(methodsWithAttribute.Collect());

        // Generate the source
        context.RegisterSourceOutput(compilationAndMethods, static (spc, source) =>
            Execute(source.Left, source.Right, spc)
        );
    }

    private static void Execute(Compilation _, IEnumerable<ApiEventInfo> methods, SourceProductionContext context)
    {
        var methodsArray = methods.ToArray() ?? [];
        var methodsByClass = methodsArray.GroupBy(m => (m.Namespace, m.ClassName));

        var handlebars = Handlebars.Create();
        handlebars.RegisterHelper("TrimOnPrefix", TrimOnPrefix);

        // Compile the templates once and reused. Saves a little time.
        var codeTemplate = GetTemplate("ApiCallbackDispatcher.hbs");
        var compiledCodeTemplate = handlebars.Compile(codeTemplate);

        var declTemplate = GetTemplate("LegionApiEventsDeclaration.hbs");
        var compiledDeclTemplate = handlebars.Compile(declTemplate);

        // For each clustering of class and methods, generate an API file as well as an abstract declaration file (for auto doc generation)
        foreach (var group in methodsByClass)
        {
            // Each class grouping has its own context. That context is used to fill in the templates
            var templateCtx = CreateContext(group);

            // Generate the source
            var source = compiledCodeTemplate(templateCtx);
            context.AddSource($"{group.Key.ClassName}.Api.g.cs", SourceText.From(source, Encoding.UTF8));

            // Generate the declaration
            var declSource = compiledDeclTemplate(templateCtx);
            context.AddSource($"{group.Key.ClassName}.Api.Decl.g.cs", SourceText.From(declSource, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     Determines whether the given node is an Event declaration with any attributes
    /// </summary>
    /// <param name="node">The attribute to test</param>
    /// <returns></returns>
    private static bool IsAnnotatedEventDecl(SyntaxNode node)
    {
        return node is EventFieldDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    /// <summary>
    ///     Tries to obtain <see cref="ApiEventInfo" /> from a <see cref="EventFieldDeclarationSyntax" /> in the current
    ///     <see cref="GeneratorSyntaxContext" />."/>"/>
    /// </summary>
    /// <param name="context">The current context. Should point to a valid <see cref="EventFieldDeclarationSyntax" /></param>
    /// <returns></returns>
    private static ApiEventInfo GetApiEventFromEventDeclNode(GeneratorSyntaxContext context)
    {
        var eventFieldDecl = (EventFieldDeclarationSyntax)context.Node;

        var (nsName, className) = AssertGetNamespaceAndClassName(eventFieldDecl);

        foreach (var attrList in eventFieldDecl.AttributeLists)
        {
            var apiAttr = attrList.Attributes.FirstOrDefault(attr => attr.Name.GetText().ToString() == "ApiEvent");
            if (apiAttr == null)
                continue;

            var varDecl = attrList.Parent?.ChildNodes()?.FirstOrDefault(c => c is VariableDeclarationSyntax);

            var declarator = varDecl?.ChildNodes().FirstOrDefault(c => c is VariableDeclaratorSyntax);
            if (declarator == null)
                continue;

            var eventName = declarator.GetText().ToString().Trim();
            if (string.IsNullOrWhiteSpace(eventName))
                continue;

            // Get the event args type from EventSink
            var eventArgsType = GetEventArgsType(
                context.SemanticModel.Compilation,
                "ClassicUO.Game.Managers.EventSink",
                eventName
            );

            if (string.IsNullOrEmpty(eventArgsType))
                continue;

            return new ApiEventInfo
            {
                EventName = eventName,
                EventArgsType = eventArgsType,
                ClassName = className,
                Namespace = nsName,
                EventDocs = GetDocstringForNode(eventFieldDecl)
            };
        }

        return null;
    }

    /// <summary>
    ///     Retrieves the documentation string for a given node.
    ///     <para>
    ///         The incremental compiler does not deliver doc Trivia alongside the nodes.
    ///         As such, we have to reparse the node first to reliably retrieve docs.
    ///     </para>
    /// </summary>
    /// <param name="node">The AST node to get the documentation of</param>
    /// <returns></returns>
    private static string GetDocstringForNode(SyntaxNode node)
    {
        // The AST we get here doesn't contain comment information.
        // We need to reparse the node with documentation mode to get the structured trivia
        var parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);
        var reparsedTree = CSharpSyntaxTree.ParseText(node.ToFullString(), parseOptions);
        var reparsedRoot = reparsedTree.GetRoot();

        // The reparsed root's first child should be our node
        // (it's wrapped in a CompilationUnit, so we get the first actual child)
        var reparsedNode = reparsedRoot.ChildNodes().FirstOrDefault() ?? reparsedRoot;

        // Now we can get the structured documentation
        var trivia = reparsedNode.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        return trivia == null ? string.Empty : trivia.ToFullString().TrimEnd();
    }

    /// <summary>
    ///     Gets a given field's (Event or otherwise) namespace and containing class's name or fails.
    ///     <br />
    ///     Note that only top-level classes and class fields are supported
    /// </summary>
    /// <param name="fieldDecl">The declaration to analyze</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">
    ///     An error thrown when unable to determine namespace or class name for the
    ///     given declaration
    /// </exception>
    private static (string Namespace, string ClassName) AssertGetNamespaceAndClassName(
        BaseFieldDeclarationSyntax fieldDecl
    )
    {
        // First, get the class parent - this will not work for nested fields.
        // It's the caller's responsibility to call this method with the right node/s
        var parentClass = (ClassDeclarationSyntax)fieldDecl.Parent;
        if (parentClass == null)
            throw new InvalidOperationException("Failed to determine parent class during code generation");

        // The actual class name
        var parentClassName = parentClass.Identifier.Text;

        // Now, the class itself should be directly under a namespace.
        // Again, nested classes are not supported.
        return parentClass.Parent switch
        {
            FileScopedNamespaceDeclarationSyntax ns => (ns.Name.ToString(), parentClassName),
            NamespaceDeclarationSyntax ns => (ns.Name.ToString(), parentClassName),
            _ => throw new InvalidOperationException("Failed to determine namespace during code generation")
        };
    }

    private static string GetEventArgsType(
        Compilation compilation,
        string sourceClassFullyQualifiedName,
        string eventName
    )
    {
        if (!_qualifiedTypeToSymbol.TryGetValue(sourceClassFullyQualifiedName, out var namedSymbol))
        {
            namedSymbol = compilation.GetTypeByMetadataName(sourceClassFullyQualifiedName);
            _qualifiedTypeToSymbol.Add(sourceClassFullyQualifiedName, namedSymbol);
        }

        var eventMember = namedSymbol?.GetMembers(eventName).OfType<IEventSymbol>().FirstOrDefault();
        if (eventMember?.Type is not INamedTypeSymbol { IsGenericType: true } namedType)
            return "object";

        var typeArg = namedType.TypeArguments.FirstOrDefault();
        return typeArg?.ToDisplayString() ?? "object";
    }

    /// <summary>
    ///     A method that trims the "On" prefix from a string.
    ///     Used as a *Handlebars* helper.
    /// </summary>
    /// <param name="output">The Handlebars output stream</param>
    /// <param name="_">The Handlebars engine context</param>
    /// <param name="arguments">The arguments being fed to the helper</param>
    /// <exception cref="ArgumentException"></exception>
    private static void TrimOnPrefix(
        EncodedTextWriter output,
        Context _,
        Arguments arguments
    )
    {
        if (arguments.Length != 1 || arguments[0] is not string)
            throw new ArgumentException("TrimOnPrefix helper requires exactly one string parameter");

        var asStr = (string)arguments[0];
        if (asStr.StartsWith("On", StringComparison.InvariantCultureIgnoreCase))
            asStr = asStr.Substring(2);
        output.WriteSafeString(asStr);
    }

    /// <summary>
    ///     Create a template context for a given class grouping.
    ///     Note that right now, each grouping will be output to a different file, so this method always returns a
    ///     context with exactly one class.
    /// </summary>
    /// <param name="flatInfos">The classes' methods</param>
    /// <returns></returns>
    private static TemplateContext CreateContext(ClassMethodsGroup flatInfos)
    {
        var methodArray = flatInfos.ToArray();
        var methods = methodArray.Select(m => new EventInfoSlim
        {
            Name = m.EventName,
            EventArgsType = m.EventArgsType,
            Docs = m.EventDocs
        }).ToArray();

        var genClass = new GeneratedClassSlim
        {
            Name = $"{flatInfos.Key.ClassName}Api",
            Namespace = flatInfos.Key.Namespace,
            GeneratedFromClassName = flatInfos.Key.ClassName,
            Events = methods.ToArray()
        };

        return new TemplateContext { Classes = [genClass] };
    }

    /// <summary>
    ///     Retrieves a Handlebars template from the assembly's embedded resources.
    ///     <br />
    ///     Template files are expected to be in the "Templates" folder.
    /// </summary>
    /// <param name="templateName"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">When unable to retrieve the requested template</exception>
    private static string GetTemplate(string templateName)
    {
        var info = Assembly.GetExecutingAssembly().GetName();
        var name = info.Name;
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"{name}.Templates.{templateName}");

        if (stream == null)
            throw new InvalidDataException("Could not obtain resource stream during code emission");

        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        return streamReader.ReadToEnd();
    }
}

/// <summary>
///     A context object for the Handlebars template.
///     Contains information pertaining to an event source
/// </summary>
internal class TemplateContext
{
    public GeneratedClassSlim[] Classes { get; set; }
}

/// <summary>
///     Information about a class from which an event proxy and declaration classes will be generated
/// </summary>
internal class GeneratedClassSlim
{
    /// <summary>
    ///     The generated class name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The namespace in which the class is declared
    /// </summary>
    public string Namespace { get; set; }

    /// <summary>
    ///     The source class's name
    /// </summary>
    public string GeneratedFromClassName { get; set; }

    /// <summary>
    ///     The events exposed by the class
    /// </summary>
    public EventInfoSlim[] Events { get; set; }
}

/// <summary>
///     Lightweight metadata about an event
/// </summary>
internal class EventInfoSlim
{
    /// <summary>
    ///     The event's name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The event's <see cref="EventArgs" /> type"/>
    /// </summary>
    public string EventArgsType { get; set; }

    /// <summary>
    ///     Any documentation associated with the event
    /// </summary>
    public string Docs { get; set; }
}

/// <summary>
///     A flattened representation of an event
/// </summary>
internal class ApiEventInfo
{
    /// <summary>
    ///     The event's name
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    ///     The event's <see cref='EventArgs' /> type
    /// </summary>
    public string EventArgsType { get; set; }

    /// <summary>
    ///     Any documentation associated with the event
    /// </summary>
    public string EventDocs { get; set; }

    /// <summary>
    ///     The class in which the event is declared
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    ///     The namespace in which the event's class owner is declared
    /// </summary>
    public string Namespace { get; set; }
}