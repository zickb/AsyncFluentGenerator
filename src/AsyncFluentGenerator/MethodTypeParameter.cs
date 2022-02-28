namespace AsyncFluentGenerator;

internal record MethodTypeParameter(
    string Name,
    bool IsMethodTypeParameter,
    string? PrimaryConstraint,
    IEnumerable<string> SecondaryConstraints,
    bool HasConstructorConstraint);
