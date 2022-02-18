using Microsoft.CodeAnalysis;

namespace AsyncFluentGenerator.Extensions;

internal static class AccessibilityExtensions
{
    public static IEnumerable<string> ToModifiers(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => new List<string> { "private" },
            Accessibility.ProtectedAndInternal => new List<string> { "protected", "internal" },
            Accessibility.Protected => new List<string> { "protected" },
            Accessibility.Internal => new List<string> { "internal" },
            Accessibility.ProtectedOrInternal => new List<string> { "internal" },
            Accessibility.Public => new List<string> { "public" },
            _ => new List<string>(),
        };
    }
}