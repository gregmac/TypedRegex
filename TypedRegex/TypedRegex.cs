using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace TypedRegex
{
    [Generator]
    public class TypedRegexGenerator : ISourceGenerator
    {
        private const string RegexAttribute = @"
using System;
using System.Text.RegularExpressions;
namespace TypedRegex
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class RegexAttribute : Attribute
    {
        public RegexAttribute(string pattern) : this(pattern, RegexOptions.None) { }
        public RegexAttribute(string pattern, RegexOptions options)
        {
            Regex = new Regex(pattern, options);
        }

        public Regex Regex { get; }
    }
}
";
        private const string MatchGroup = @"
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace TypedRegex
{
    /// <summary>
    /// Wrapper for a regular expression matched <see cref=""Group"" />
    /// </summary>
    public class MatchGroup
    {
        public MatchGroup(Group group) 
        {
            RawGroup = group;
        }
        
        /// <summary>The original <see cref=""Group"" /> object.</summary>
        public Group RawGroup {get;}

        /// <inheritdoc cref=""Capture.Count"" />
        public int CaptureCount => RawGroup.Captures.Count;
        
        /// <inheritdoc cref=""Group.Captures"" />
        public IEnumerable<string> Values => RawGroup.Captures.Cast<Capture>().Select(x => x.Value);

        /// <summary>The full value of the captured group.</summary>
        public string Value => RawGroup.Value;
        
        /// <inheritdoc cref=""Value"" />
        public override string ToString() => RawGroup.Value;
        
        /// <summary>Conversion to <see cref=""string"" />.</summary>
        public static implicit operator string(MatchGroup d) => d.RawGroup.Value;
        
        /// <summary>Conversion to <see cref=""Group"" />.</summary>
        public static implicit operator Group(MatchGroup d) => d.RawGroup;
    }
}";
        private readonly Regex IntOnly = new Regex(@"^\d+$");

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add static source
            context.AddSource(nameof(RegexAttribute) + ".cs", RegexAttribute);
            context.AddSource(nameof(MatchGroup) + ".cs", MatchGroup);

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions parseOptions = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(RegexAttribute, Encoding.UTF8), parseOptions));

            // get the newly bound attribute
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("TypedRegex.RegexAttribute");

            // loop over the candidate classes, and keep the ones that are actually annotated
            foreach (var @class in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(@class.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(@class);
                var attributeData = classSymbol.GetAttributes()
                    .SingleOrDefault(a => a.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                // not one we care about
                if (attributeData == null) continue;

                string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                string className = classSymbol.Name;

                // validate arguments
                var pattern = attributeData.ConstructorArguments[0].Value as string;
                var options = attributeData.ConstructorArguments.Length > 1
                    ? (RegexOptions)attributeData.ConstructorArguments[1].Value
                    : RegexOptions.None;

                var regex = new Regex(pattern, options);
                var groupNames = regex.GetGroupNames()
                    .Select((name, idx) => (idx, IntOnly.IsMatch(name) ? "Group" + name : FirstToUpper(name)));

                var xmlEscapedPattern = HttpUtility.HtmlEncode(pattern);

#pragma warning disable RCS1214 // Unnecessary interpolated string.
#pragma warning disable RCS1197 // Optimize StringBuilder.Append/AppendLine call.
                var source = new StringBuilder($@"
// {namespaceName}.{className}.generated.cs
// Pattern: {pattern}
// Options: {options}

namespace {namespaceName} {{
    /// <summary>
    /// Typed operations on the regular expression
    /// <c>{xmlEscapedPattern}</c>.
    /// </summary>
    public partial class {className} {{
        /// <summary>
        /// The actual <see cref=""System.Text.RegularExpressions.Regex"" /> used for matching this object.
        /// </summary>
        public static System.Text.RegularExpressions.Regex Regex {{ get; }} = new System.Text.RegularExpressions.Regex(@""{pattern.Replace("\"", "\"\"")}"", (System.Text.RegularExpressions.RegexOptions){(int)options});

        /// <summary>
        /// Check if the <paramref name=""input""/> matches the regular expression
        /// <c>{xmlEscapedPattern}</c>.
        /// </summary>
        /// <param name=""input"">The string to search for a match</param>
        public static bool IsMatch(string input) => Regex.IsMatch(input);

        /// <summary>
        /// Find the first match for <paramref name=""input""/> against the regular expression
        /// <c>{xmlEscapedPattern}</c>. Returns null if no match.
        /// </summary>
        /// <param name=""input"">The string to search for a match</param>
        public static {className} Match(string input)
        {{
            var match = Regex.Match(input);
            return match.Success ? new {className}(match) : null;
        }}

        /// <summary>
        /// Find the first match for <paramref name=""input""/> against the regular expression
        /// <c>{xmlEscapedPattern}</c>, returning true if found.
        /// </summary>
        /// <param name=""input"">The string to search for a match</param>
        public static bool TryMatch(string input, out {className} match)
        {{
            match = Match(input);
            return match != null;
        }}

        /// <summary>
        /// Find all matches for <paramref name=""input""/> against the regular expression
        /// <c>{xmlEscapedPattern}</c>. Returns an empty collection if there are no matches.
        /// </summary>
        /// <param name=""input"">The string to search for a match</param>
        public static System.Collections.Generic.IEnumerable<{className}> Matches(string input)
        {{
            var match = Regex.Match(input);
            while (match.Success)
            {{
                yield return new {className}(match);
                match = match.NextMatch();
            }}
        }}

        private {className}(System.Text.RegularExpressions.Match match)
        {{
            RawMatch = match;
");
                foreach ((var matchIndex, var propertyName) in groupNames)
                {
                    source.AppendLine($"            {propertyName} = new TypedRegex.MatchGroup(match.Groups[{matchIndex}]);");
                }
                source.Append($@"
        }}

        /// <summary>The original <see cref=""System.Text.RegularExpressions.Match""> object.</summary>
        public System.Text.RegularExpressions.Match RawMatch {{ get; }}

        /// <summary>The captured substring matching the full expression.</summary>
        public string Value => RawMatch.Value;

");
                foreach ((var matchIndex, var propertyName) in groupNames)
                {
                    source.AppendLine($"        /// <summary>The capture group at index {matchIndex}</summary>");
                    source.AppendLine($"        public TypedRegex.MatchGroup {propertyName} {{ get; }}");
                }

                source.Append($@"
    }}
}}
");
#pragma warning restore RCS1197 // Optimize StringBuilder.Append/AppendLine call.
#pragma warning restore RCS1214 // Unnecessary interpolated string.

                context.AddSource($"{namespaceName}.{className}.generated.cs", source.ToString());
            }
        }

#pragma warning disable IDE0057 // Use range operator (not compatible with .NET standard)
        private static string FirstToUpper(string input) => input[0].ToString().ToUpper() + input.Substring(1);
#pragma warning restore IDE0057 // Use range operator

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        internal class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ClassDeclarationSyntax cds
                    && cds.AttributeLists.Count > 0)
                {
                    CandidateClasses.Add(cds);
                }
            }
        }
    }
}
