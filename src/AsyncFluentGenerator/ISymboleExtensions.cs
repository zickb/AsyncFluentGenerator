using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AsyncFluentGenerator.Extensions;

internal static class ISymbolExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> if that type is "awaitable".
    /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
    /// </summary>
    public static bool IsAwaitableNonDynamic(this ITypeSymbol type, SemanticModel semanticModel, int position)
    {
        // otherwise: needs valid GetAwaiter
        var potentialGetAwaiters = semanticModel.LookupSymbols(position,
                                                               container: type.OriginalDefinition,
                                                               name: WellKnownMemberNames.GetAwaiter,
                                                               includeReducedExtensionMethods: true);
        var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
        return getAwaiters.Any(VerifyGetAwaiter);
    }

    private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
    {
        var returnType = getAwaiter.ReturnType;
        if (returnType == null)
        {
            return false;
        }

        // bool IsCompleted { get }
        if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
        {
            return false;
        }

        var methods = returnType.GetMembers().OfType<IMethodSymbol>();

        // NOTE: The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
        // NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
        // NOTE: (rather than any OnCompleted method conforming to a certain pattern).
        // NOTE: Should this code be updated to match the spec?

        // void OnCompleted(Action) 
        // Actions are delegates, so we'll just check for delegates.
        if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters.Length == 1 && x.Parameters.First().Type.TypeKind == TypeKind.Delegate))
        {
            return false;
        }

        // void GetResult() || T GetResult()
        return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
    }

    internal static bool IsNonGenericTaskType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (type is not INamedTypeSymbol namedType || namedType.Arity != 0)
        {
            return false;
        }

        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskType != null && taskType.DeclaredAccessibility == Accessibility.Public && (object)namedType.ConstructedFrom == taskType)
        {
            return true;
        }
        var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        if (valueTaskType != null && valueTaskType.DeclaredAccessibility == Accessibility.Public && (object)namedType.ConstructedFrom == valueTaskType)
        {
            return true;
        }
        if (namedType.IsVoidType())
        {
            return false;
        }
        return namedType.ConstructedFrom.IsCustomTaskType(builderArgument: out _);
    }

    internal static bool IsGenericTaskType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (!(type is INamedTypeSymbol { Arity: 1 } namedType))
        {
            return false;
        }

        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        if (taskType != null && taskType.DeclaredAccessibility == Accessibility.Public && (object)namedType.ConstructedFrom == taskType)
        {
            return true;
        }
        var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        if (valueTaskType != null && valueTaskType.DeclaredAccessibility == Accessibility.Public && (object)namedType.ConstructedFrom == valueTaskType)
        {
            return true;
        }
        return namedType.ConstructedFrom.IsCustomTaskType(builderArgument: out _);
    }

    internal static bool IsIAsyncEnumerableType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (!(type is INamedTypeSymbol { Arity: 1 } namedType))
        {
            return false;
        }

        var potentialTypes = compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"))
            .ToList();
        potentialTypes.Add(compilation.Assembly.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"));
        var iAsyncEnumerableTypes = potentialTypes.Where(x => x is not null && x.DeclaredAccessibility == Accessibility.Public).ToList();
        foreach (var iAsyncEnumerableType in iAsyncEnumerableTypes)
        {
            if ((object)namedType.ConstructedFrom == iAsyncEnumerableType)
                return true;
        }
        return false;
    }

    internal static bool IsIEnumerableType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (!(type is INamedTypeSymbol { Arity: 1 } namedType))
        {
            return false;
        }

        var potentialTypes = compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"))
            .ToList();
        potentialTypes.Add(compilation.Assembly.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"));
        var iEnumerableTypes = potentialTypes.Where(x => x is not null && x.DeclaredAccessibility == Accessibility.Public).ToList();
        foreach (var iEnumerableType in iEnumerableTypes)
        {
            if ((object)namedType.ConstructedFrom == iEnumerableType)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the type is generic or non-generic custom task-like type due to the
    /// [AsyncMethodBuilder(typeof(B))] attribute. It returns the "B".
    /// </summary>
    /// <remarks>
    /// For the Task types themselves, this method might return true or false depending on mscorlib.
    /// The definition of "custom task-like type" is one that has an [AsyncMethodBuilder(typeof(B))] attribute,
    /// no more, no less. Validation of builder type B is left for elsewhere. This method returns B
    /// without validation of any kind.
    /// </remarks>
    internal static bool IsCustomTaskType(this INamedTypeSymbol type, out object? builderArgument)
    {
        var arity = type.Arity;
        if (arity < 2)
        {
            return type.HasAsyncMethodBuilderAttribute(out builderArgument);
        }

        builderArgument = null;
        return false;
    }

    internal static bool IsVoidType(this ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_Void;
    }

    internal static bool HasAsyncMethodBuilderAttribute(this ISymbol symbol, out object? builderArgument)
    {
        // Find the AsyncMethodBuilder attribute.
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.IsTargetAttribute("System.Runtime.CompilerServices", "AsyncMethodBuilderAttribute")
                && attr.ConstructorArguments.Length == 1
                && attr.ConstructorArguments[0].Kind == TypedConstantKind.Type)
            {
                builderArgument = attr.ConstructorArguments[0].Value!;
                return true;
            }
        }

        builderArgument = null;
        return false;
    }


    /// <summary>
    /// Compares the namespace and type name with the attribute's namespace and type name.
    /// Returns true if they are the same.
    /// </summary>
    internal static bool IsTargetAttribute(this AttributeData attributeData, string namespaceName, string typeName)
    {
        if (attributeData.AttributeClass is null)
        {
            return false;
        }

        if (!attributeData.AttributeClass.Name.Equals(typeName))
        {
            return false;
        }

        if (attributeData.AttributeClass.IsErrorType())
        {
            // Can't guarantee complete name information.
            return false;
        }

        return attributeData.AttributeClass.HasNameQualifier(namespaceName);
    }

    /// <summary>
    /// Return true if the fully qualified name of the type's containing symbol
    /// matches the given name. This method avoids string concatenations
    /// in the common case where the type is a top-level type.
    /// </summary>
    internal static bool HasNameQualifier(this INamedTypeSymbol type, string qualifiedName)
    {
        const StringComparison comparison = StringComparison.Ordinal;

        var container = type.ContainingSymbol;
        if (container.Kind != SymbolKind.Namespace)
        {
            // Nested type. For simplicity, compare qualified name to SymbolDisplay result.
            return string.Equals(container.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted, SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)), qualifiedName, comparison);
        }

        var @namespace = (INamespaceSymbol)container;
        if (@namespace.IsGlobalNamespace)
        {
            return qualifiedName.Length == 0;
        }

        return HasNamespaceName(@namespace, qualifiedName, comparison, length: qualifiedName.Length);
    }

    private static bool HasNamespaceName(INamespaceSymbol @namespace, string namespaceName, StringComparison comparison, int length)
    {
        if (length == 0)
        {
            return false;
        }

        var container = @namespace.ContainingNamespace;
        int separator = namespaceName.LastIndexOf('.', length - 1, length);
        int offset = 0;
        if (separator >= 0)
        {
            if (container.IsGlobalNamespace)
            {
                return false;
            }

            if (!HasNamespaceName(container, namespaceName, comparison, length: separator))
            {
                return false;
            }

            int n = separator + 1;
            offset = n;
            length -= n;
        }
        else if (!container.IsGlobalNamespace)
        {
            return false;
        }

        var name = @namespace.Name;
        return (name.Length == length) && (string.Compare(name, 0, namespaceName, offset, length, comparison) == 0);
    }

    public static bool IsErrorType(this ITypeSymbol type)
    {
        return type.Kind == SymbolKind.ErrorType;
    }

    public static string? GetDefaultValue(this IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return null;

        if (parameter.Type.IsValueType && parameter.ExplicitDefaultValue == null)
            return "default";

        return parameter.ExplicitDefaultValue switch
        {
            null => "null",
            string stringValue => $"\"{stringValue}\"",
            char charValue => $"\'{charValue}\'",
            float floatValue => $"{floatValue.ToString(CultureInfo.InvariantCulture)}f",
            double doubleValue => $"{doubleValue.ToString(CultureInfo.InvariantCulture)}d",
            decimal decimalValue => $"{decimalValue.ToString(CultureInfo.InvariantCulture)}m",
            sbyte sbyteValue => $"{sbyteValue.ToString(CultureInfo.InvariantCulture)}",
            byte byteValue => $"{byteValue.ToString(CultureInfo.InvariantCulture)}",
            short shortValue => $"{shortValue.ToString(CultureInfo.InvariantCulture)}",
            ushort ushortValue => $"{ushortValue.ToString(CultureInfo.InvariantCulture)}",
            int intValue => $"{intValue.ToString(CultureInfo.InvariantCulture)}",
            uint uintValue => $"{uintValue.ToString(CultureInfo.InvariantCulture)}u",
            long longValue => $"{longValue.ToString(CultureInfo.InvariantCulture)}l",
            ulong ulongValue => $"{ulongValue.ToString(CultureInfo.InvariantCulture)}ul",
            nint nintValue => $"{nintValue}",
            nuint nuintValue => $"{nuintValue}",
            bool boolValue => $"{boolValue.ToString(CultureInfo.InvariantCulture)}",
            _ => parameter.ExplicitDefaultValue.ToString()
        };
    }

    public static string? GetPrimaryConstraint(this ITypeParameterSymbol typeParameter)
    {
        if (typeParameter.HasNotNullConstraint)
            return "notnull";
        if (typeParameter.HasReferenceTypeConstraint)
            return $"class{(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty)}";
        if (typeParameter.HasUnmanagedTypeConstraint)
            return "unmanaged";
        if (typeParameter.HasValueTypeConstraint)
            return "struct";

        return null;
    }

    internal static bool TryGetTypeArgument(this ITypeSymbol type, out ITypeSymbol? typeArgumentSymbol)
    {
        typeArgumentSymbol = null;
        if (!(type is INamedTypeSymbol { Arity: 1 } namedType) || namedType.TypeArguments.Count() != 1)
        {
            return false;
        }

        typeArgumentSymbol = namedType.TypeArguments.First();
        return true;
    }

    public static string GetAsyncReturnType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (type.IsGenericTaskType(compilation) || type.IsNonGenericTaskType(compilation) || type.IsIAsyncEnumerableType(compilation))
            return type.ToString();

        if (type.IsVoidType())
            return "System.Threading.Tasks.Task";

        if (type.IsIEnumerableType(compilation) && type.TryGetTypeArgument(out ITypeSymbol? typeArgument))
            return $"System.Collections.Generic.IAsyncEnumerable<{typeArgument}>";

        return $"System.Threading.Tasks.Task<{type}>";
    }

    public static bool IsNonVoidLikeType(this ITypeSymbol type, CSharpCompilation compilation)
    {
        if (type.IsVoidType() || type.IsNonGenericTaskType(compilation) )
            return false;

        return true;
    }

    public static bool IsAccessable(this ISymbol symbol)
    {
        var currentSymbol = symbol;
        while (currentSymbol != null)
        {
            if (currentSymbol.DeclaredAccessibility < Accessibility.Internal)
                return false;
            currentSymbol = currentSymbol.ContainingType;
        }
        return true;
    }

    public static ImmutableArray<ITypeParameterSymbol> GetAllAvailableTypeParameters(this ISymbol symbol)
    {
        Dictionary<string, ITypeParameterSymbol> typeParameters = new();
        ImmutableArray<ITypeParameterSymbol> currentTypeParameters;
        var currentSymbol = symbol;
        while (currentSymbol != null)
        {
            currentTypeParameters = currentSymbol switch
            {
                INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeParameters,
                IMethodSymbol methodSymbol => methodSymbol.TypeParameters,
                _ => new ()
            };
            
            foreach (var typeParameter in currentTypeParameters)
            {
                if (!typeParameters.ContainsKey(typeParameter.Name))
                    typeParameters.Add(typeParameter.Name, typeParameter);
            }
            currentSymbol = currentSymbol.ContainingType;
        }
        return typeParameters.Values.ToImmutableArray();
    }
}
