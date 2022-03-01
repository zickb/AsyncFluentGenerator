namespace AsyncFluentGenerator;

internal record MethodInformation(
    bool IsNonVoidLikeType,
    bool IsAwaitable,
    bool IsEnumerable,
    bool IsAsyncEnumerable);