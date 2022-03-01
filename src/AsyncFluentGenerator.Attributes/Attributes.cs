
namespace AsyncFluentGenerator;

/// <summary>
/// Use this attribute to generate a async version of an instance method.
/// </summary>
/// <remarks>
/// The instance method and the containing type(s) must be at least internally visible.
/// Also it is not allowed to use the <c>in</c>, <c>out</c> or <c>ref</c> modifier for parameters or the <c>ref</c> modifer for the return type in a method annotated with this attribute.  
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AsyncFluentMethod: Attribute
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="includeAttributes">An optional parameter to indicate if other attributes should be included in the generated method decleration. The <c>AsyncFluentMethod</c> attribute is allways ignored. Default is <c>false</c>.</param>
    public AsyncFluentMethod(bool includeAttributes = false) 
    { 
        IncludeAttributes = includeAttributes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="methodName">The name of the generated method.</param>
    /// <param name="includeAttributes">An optional parameter to indicate if other attributes should be included in the generated method decleration. The <c>AsyncFluentMethod</c> attribute is allways ignored. Default is <c>false</c>.</param>
    public AsyncFluentMethod(string methodName, bool includeAttributes = false)
    {
        MethodName = methodName;
        IncludeAttributes = includeAttributes;
    }

    /// <summary>
    /// The name of the generated method.
    /// </summary>
    public string? MethodName { get; }
    /// <summary>
    /// Indicates if other attributes should be included in the generated method decleration. The <c>AsyncFluentMethod</c> attribute is allways ignored.
    /// </summary>
    public bool IncludeAttributes { get; }
}

/// <summary>
/// Use this attribute to specify the name of the generated class which contains the generated async versions of the instance methods.
/// </summary>
/// <remarks>
/// This attribute has only an effect if at least one of the instance methods of the class is annotated with the <c>AsyncFluentMethod</c> attribute. 
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AsyncFluentClass: Attribute
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="extensionTypes">The (awaitable) types which will be extended.</param>
    public AsyncFluentClass(params Type[] extensionTypes) 
    { 
        ExtensionTypes = extensionTypes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="extensionClassName">The name of the generated class.</param>
    /// <param name="extensionTypes">The (awaitable) types which will be extended.</param>
    public AsyncFluentClass(string extensionClassName, params Type[] extensionTypes) 
    { 
        ExtensionClassName = extensionClassName;
        ExtensionTypes = extensionTypes;
    }

    /// <summary>
    /// The name of the generated class.
    /// </summary>
    public string? ExtensionClassName { get; }

    /// <summary>
    /// The (awaitable) types which will be extended.
    /// </summary>
    public Type[] ExtensionTypes { get; }
}