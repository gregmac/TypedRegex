using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TypedRegex
{
    //https://dominikjeske.github.io/source-generators/

    public abstract class TypedRegex
    {
        protected Regex Regex { get; }

        protected TypedRegex(string pattern) : this (new Regex(pattern)) { }
        protected TypedRegex(string pattern, RegexOptions options) : this(new Regex(pattern, options)) { }
        protected TypedRegex(Regex regex)
        {
            Regex = regex;
        }

        public bool IsMatch(string input) => Regex.IsMatch(input);
    }

    [Generator]
    public class AugmentingGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;


            // the generator infrastructure will create a receiver and populate it
            // we can retrieve the populated instance via the context
            var syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

            // get the recorded user class
            var userClass = syntaxReceiver.ClassToAugment;
            if (userClass is null)
            {
                // if we didn't find the user class, there is nothing to do
                return;
            }

            var classNamespace = GetNamespaceFrom(userClass);


            //System.Diagnostics.Debugger.Launch();

    var constructors = userClass.ChildNodes()
                .Where(n => n.IsKind(SyntaxKind.ConstructorDeclaration))
                .Cast<ConstructorDeclarationSyntax>();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.ParameterList
                   .ChildNodes()
                   .Cast<ParameterSyntax>()
                   .OrderBy(node => ((IdentifierNameSyntax)node.Type).Identifier.ToString())
                   .Select(node => SyntaxFactory.Parameter(
                       SyntaxFactory.List<AttributeListSyntax>(),
                       SyntaxFactory.TokenList(),
                       SyntaxFactory.ParseTypeName(((IdentifierNameSyntax)node.Type).Identifier.Text),
                       SyntaxFactory.Identifier(node.Identifier.Text),
                       null))
                 .ToList();
                //System.Diagnostics.Debugger.Launch();
            }


            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();


                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            }

                // add the generated implementation to the compilation
                SourceText sourceText = SourceText.From($@"
namespace {classNamespace} {{
    public partial class {userClass.Identifier}
    {{
        public string FoundIt => ""yes"";
        public class Result {{
        
       }}
    }}
}}", Encoding.UTF8);
            context.AddSource("UserClass.Generated.cs", sourceText);
        }

        /// <summary>
        /// From https://stackoverflow.com/a/63686228/7913
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetNamespaceFrom(SyntaxNode s)
            => s.Parent switch
            {
                NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
                null => string.Empty, // or whatever you want to do
                    _ => GetNamespaceFrom(s.Parent)
            };

        public class MySyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax ClassToAugment { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                
                // Business logic to decide what we're interested in goes here
                if (syntaxNode is ClassDeclarationSyntax cds &&
                    cds.BaseList?.Types.Any(x => x.Type?.ToString()?.Contains("TypedRegex") == true) == true) // todo: yuck
                {
                    ClassToAugment = cds;
                }
            }
        }
    }
}