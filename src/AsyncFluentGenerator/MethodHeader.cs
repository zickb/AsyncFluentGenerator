namespace AsyncFluentGenerator;

internal record MethodHeader(
    IEnumerable<string> Attributes,
    IEnumerable<string> ReturnTypeAttributes,
    IEnumerable<string> Modifiers,
    string ReturnType,
    string? InterfaceSpecifier,
    string Name,
    IEnumerable<MethodTypeParameter> Types,
    IEnumerable<MethodParameter> Parameters);
