namespace AsyncFluentGenerator;

internal record MethodInformation(
    bool IsAwaitable,
    bool IsEnumerable,
    bool IsAsyncEnumerable);