namespace AsyncFluentGenerator;

internal record MethodParameter(
    IEnumerable<string> Attributes,
    IEnumerable<string> Modifiers,
    string Type,
    string Name,
    string? DefaultValue);
