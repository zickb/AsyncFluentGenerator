using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AsyncFluentGenerator.Extensions;

namespace AsyncFluentGenerator;

[Generator(LanguageNames.CSharp)]
public class AsyncFluentGenerator : IIncrementalGenerator
{
    private const string AsyncFluentMethodAttributeName = "AsyncFluentMethod";
    private const string AsyncFluentMethodAttribute = "AsyncFluentGenerator.AsyncFluentMethod";
    private const string AsyncFluentClassAttribute = "AsyncFluentGenerator.AsyncFluentClass";
    private const string DefaultNamespace = "AsyncFluentMethodExtionsions";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #nullable disable
        IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), 
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        #nullable enable

        IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndTypes 
            = context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes, 
            static (spc, source) => Execute((CSharpCompilation)source.Item1, source.Item2, spc));

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            if (node is AttributeSyntax attributeSyntax && 
                attributeSyntax.Name is SimpleNameSyntax simpleNameSyntax && 
                simpleNameSyntax.Identifier.Text == AsyncFluentMethodAttributeName &&
                node.Parent is AttributeListSyntax attributeListSyntax &&
                attributeListSyntax.Parent is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return true;
            }
            return false;
        }

        static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            return (TypeDeclarationSyntax?)context.Node.Parent?.Parent?.Parent;
        }
    }

    private static void Execute(CSharpCompilation compilation, ImmutableArray<TypeDeclarationSyntax> types, SourceProductionContext context)
    {   
        if (types.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var distinctTypeSymbols = types.Select(x => compilation.GetSemanticModel(x.SyntaxTree).GetDeclaredSymbol(x)).Distinct<INamedTypeSymbol?>(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>();
        foreach (INamedTypeSymbol typeSymbol in distinctTypeSymbols)
        {
            var writer = new AsyncFluentExtensionWriter();

            var defaultExtensionTypes = new string[1] { "System.Threading.Tasks.Task" };
            var defaultClassName = $"{((TypeDeclarationSyntax)typeSymbol.DeclaringSyntaxReferences.First().GetSyntax()).Identifier.Text}AsyncFluentMethodExtensions";
            var classAttributeValues = typeSymbol.GetAttributes().Where(x => x.AttributeClass?.ToString() == AsyncFluentClassAttribute).FirstOrDefault()?.ConstructorArguments.Select(x => x.Kind == TypedConstantKind.Array ? x.Values.Select(x =>  x.Value) : x.Value);
            var classConfig = new AsyncFluentClassConfiguration(
                    typeSymbol.ContainingNamespace.IsGlobalNamespace ? DefaultNamespace : typeSymbol.ContainingNamespace.ToString(),
                    classAttributeValues?.Count() >= 1 && classAttributeValues.ElementAt(classAttributeValues.Count() - 1) is IEnumerable<object> extensionTypes && extensionTypes.Count() > 0 ? extensionTypes.Select(x => x.ToString()).ToArray() : defaultExtensionTypes,
                    classAttributeValues?.Count() >= 2 && classAttributeValues.ElementAt(classAttributeValues.Count() - 2) is string className && className.Trim() != string.Empty  ? className.Trim() : defaultClassName);
            writer.WriteNullableEnable();
            writer.WriteClassStart(classConfig);
            
            var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>();
            var distinctMethods = methods.Where(x => x.GetAttributes().Any(y => y.AttributeClass?.ToString() == AsyncFluentMethodAttribute)).Distinct(SymbolEqualityComparer.Default).OfType<IMethodSymbol>();
            foreach (IMethodSymbol methodSymbol in distinctMethods)
            {
                var interfaceSymbol = methodSymbol.ExplicitInterfaceImplementations.FirstOrDefault()?.ContainingType;
                var attributeLocation = methodSymbol.GetAttributes().Where(x => x.AttributeClass?.ToString() == AsyncFluentMethodAttribute).FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                
                List<Diagnostic> diagnostics = new();
                if (methodSymbol.IsStatic)
                {
                    var diagnosticsOptions = new DiagnosticDescriptor("AFG0001", "Wrong usage of AsyncFluentMethod attribute",
                        "The AsyncFluentMethod attribute can only be applied to instance methods.", "Usage", DiagnosticSeverity.Error, true);
                    diagnostics.Add(Diagnostic.Create(diagnosticsOptions, attributeLocation));
                }

                if (methodSymbol.ReturnsByRef)
                {
                    var diagnosticsOptions = new DiagnosticDescriptor("AFG0001", "Wrong usage of AsyncFluentMethod attribute",
                        "Method decorated with the AsyncFluentMethod attribute can not return values by reference.", "Usage", DiagnosticSeverity.Error, true);
                    diagnostics.Add(Diagnostic.Create(diagnosticsOptions, attributeLocation));
                }

                if ((interfaceSymbol != null && !methodSymbol.ExplicitInterfaceImplementations.FirstOrDefault()!.IsAccessable()) || 
                    (interfaceSymbol == null && !methodSymbol.IsAccessable()))
                {
                    var diagnosticsOptions = new DiagnosticDescriptor("AFG0001", "Wrong usage of AsyncFluentMethod attribute",
                        "Method is not visible outside the containing type.", "Usage", DiagnosticSeverity.Error, true);
                    diagnostics.Add(Diagnostic.Create(diagnosticsOptions, attributeLocation));
                }

                if (methodSymbol.Parameters.Any(x => x.RefKind != RefKind.None))
                {
                    var diagnosticsOptions = new DiagnosticDescriptor("AFG0001", "Wrong usage of AsyncFluentMethod attribute",
                        "Method decorated with the AsyncFluentMethod attribute can not have parameters annotated with the in, out or ref modifieres.", "Usage", DiagnosticSeverity.Error, true);
                    diagnostics.Add(Diagnostic.Create(diagnosticsOptions, attributeLocation));
                }

                if (diagnostics.Any())
                {
                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                    continue;
                }
                
                var methodAttributeValues = methodSymbol.GetAttributes().Where(x => x.AttributeClass?.ToString() == AsyncFluentMethodAttribute).First().ConstructorArguments.Select(x => x.Value);
                var methodConfig = new AsyncFluentMethodConfiguration(
                    methodAttributeValues?.Count() >= 2 && methodAttributeValues.ElementAt(methodAttributeValues.Count() - 2) is string methodName && methodName.Trim() != string.Empty ? methodName.Trim() : null,
                    methodAttributeValues?.Count() >= 1 && methodAttributeValues.ElementAt(methodAttributeValues.Count() - 1) is bool includeAttributes && includeAttributes);

                var methodInformation = new MethodInformation(
                    methodSymbol.ReturnType.IsNonVoidLikeType(compilation),
                    methodSymbol.ReturnType.IsAwaitableNonDynamic(compilation.GetSemanticModel(methodSymbol.DeclaringSyntaxReferences.First().SyntaxTree), methodSymbol.DeclaringSyntaxReferences.First().GetSyntax().SpanStart),
                    methodSymbol.ReturnType.IsIEnumerableType(compilation),
                    methodSymbol.ReturnType.IsIAsyncEnumerableType(compilation));

                IEnumerable<string> methodAttributes = methodSymbol.GetAttributes().Where(x => x.AttributeClass?.ToString() != AsyncFluentMethodAttribute).Select(y => y.ToString());
                IEnumerable<string> returnTypeAttributes = methodSymbol.GetReturnTypeAttributes().Select(x => x.ToString());
                List<string> modifiers = ((ISymbol?)interfaceSymbol ?? methodSymbol).DeclaredAccessibility.ToModifiers().ToList();
                modifiers.Add("static");
                modifiers.Add("async");
                string returnType = methodSymbol.ReturnType.GetAsyncReturnType(compilation);
                string? interfaceSpecifier = interfaceSymbol?.ToString();
                string methodIdentifier = ((MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxReferences.First().GetSyntax()).Identifier.Text;
                IEnumerable<MethodTypeParameter> methodTypeParameters = methodSymbol.GetAllAvailableTypeParameters().Select(x => new MethodTypeParameter(
                    x.MetadataName,
                    methodSymbol.TypeParameters.Any(y => y.MetadataName == x.MetadataName),
                    x.GetPrimaryConstraint(),
                    x.ConstraintTypes.Select(x => x.ToString()), 
                    x.HasConstructorConstraint));
                List<MethodParameter> parameters = methodSymbol.Parameters.Select(x => new MethodParameter(
                    x.GetAttributes().Select(x => x.ToString()),
                    x.IsParams ? new List<string> { "params" } : new List<string>(),
                    x.Type.ToString(),
                    ((ParameterSyntax)x.DeclaringSyntaxReferences.First().GetSyntax()).Identifier.Text, //need to resolve possible verbatim identifier
                    x.GetDefaultValue())).ToList();

                foreach (var type in classConfig.ExtensionTypes)
                {
                    parameters.Insert(0, new MethodParameter(new List<string>(), new List<string> { "this" }, $"{type}<{methodSymbol.ContainingType}>", $"methodContainingInstance", null));

                    var mH = new MethodHeader
                    (
                        Attributes: methodAttributes,
                        ReturnTypeAttributes: returnTypeAttributes,
                        Modifiers: modifiers,
                        ReturnType: returnType,
                        InterfaceSpecifier: interfaceSpecifier,
                        Name: methodIdentifier,
                        Types: methodTypeParameters,
                        Parameters: parameters
                    );
                    writer.WriteAsyncFluentMethod(methodConfig, mH, methodInformation);
                    parameters.RemoveAt(0);
                }
            }
            writer.WriteClassEnd();
            writer.WriteNullableDisable();
            var text = writer.ToString();
            context.AddSource($"{classConfig.ClassName}AsyncFluentMethodExtensions.g.cs", text);
            // System.Console.WriteLine(text);
        }
    }
}
