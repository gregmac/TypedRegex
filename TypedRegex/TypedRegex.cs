using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorSamples
{
    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;
using System.Text.RegularExpressions;
namespace TypedRegex
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TypedRegexAttribute : Attribute
    {
        public TypedRegexAttribute(string pattern) : this(pattern, RegexOptions.None) { }
        public TypedRegexAttribute(string pattern, RegexOptions options)
        {
            Regex = new Regex(pattern, options);
        }

        public Regex Regex { get; }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("TypedRegexAttribute", attributeText);

            // retrieve the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions parseOptions = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), parseOptions));

            // get the newly bound attribute
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("TypedRegex.TypedRegexAttribute");

            // loop over the candidate classes, and keep the ones that are actually annotated
            foreach (var @class in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(@class.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(@class);
                var attributeData = classSymbol.GetAttributes()
                    .Single(a => a.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                string className = classSymbol.Name;

                // validate arguments
                var pattern = attributeData.ConstructorArguments[0].Value as string;
                var options = attributeData.ConstructorArguments.Length > 1
                    ? (RegexOptions)attributeData.ConstructorArguments[1].Value
                    : RegexOptions.None;

                context.AddSource($"{namespaceName}.{className}.generated.cs", $@"
// {namespaceName}.{className}.generated.cs
// Pattern: {pattern}
// Options: {options}
using System.Text.RegularExpressions;
namespace {namespaceName} {{
    public partial class {className} {{
        protected Regex Regex {{ get; }} = new Regex(@""{pattern.Replace("\"", "\"\"")}"", (RegexOptions){(int)options});

        public bool IsMatch(string value) => Regex.IsMatch(value);
    }}
}}

");

            }
        }

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
