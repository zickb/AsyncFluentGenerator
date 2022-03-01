namespace AsyncFluentGenerator;

internal record AsyncFluentClassConfiguration(
    string Namespace,
    string[] ExtensionTypes,
    string ClassName);