
namespace AsyncFluentGenerator;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AsyncFluentMethod: Attribute
{
    public string? MethodName { get; }
    public bool IncludeAttributes { get; }

    public AsyncFluentMethod(bool includeAttributes = false) 
    { 
        IncludeAttributes = includeAttributes;
    }

    public AsyncFluentMethod(string methodName, bool includeAttributes = false)
    {
        MethodName = methodName;
        IncludeAttributes = includeAttributes;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AsyncFluentClass: Attribute
{
    public string ExtensionClassName { get; }

    public AsyncFluentClass(string extensionClassName) 
    { 
        ExtensionClassName = extensionClassName;
    }
}