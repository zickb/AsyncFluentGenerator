namespace AsyncFluentGenerator;

internal class AsyncFluentExtensionWriter
{
    private readonly IndentedTextBuilder _codeBuilder;

    public AsyncFluentExtensionWriter()
    {
        _codeBuilder = new IndentedTextBuilder(new StringWriter());
    }

    public override string ToString()
    {
        string code = _codeBuilder.ToString();
        _codeBuilder.Close();
        return code;
    }
    public void WriteAsyncFluentMethod(AsyncFluentMethodConfiguration configuration, MethodHeader methodHeader, MethodInformation methodInformation)
    {
        if (!methodHeader.Parameters.Any())
            throw new ArgumentException($"Parameters cannot be empty. At least one parameter (Extension Parameter) must be provided.");

        WriteMethodHeader(configuration, methodHeader);
        WriteMethodBody(methodHeader, methodInformation);
    }

    public void WriteNullableEnable() => _codeBuilder.WriteLine("#nullable enable");

    public void WriteNullableDisable() => _codeBuilder.WriteLine("#nullable disable");
    

    public void WriteClassStart(AsyncFluentClassConfiguration configuration)
    {
        _codeBuilder.WriteLine($"namespace {configuration.Namespace}")
                    .WriteBeginScope()
                    .WriteLine($"public static class {configuration.ClassName.Trim()}AsyncFluentMethodExtensions")
                    .WriteBeginScope();
    }

    public void WriteClassEnd()
    {
        _codeBuilder.WriteEndScope()
                    .WriteEndScope();
    }

    private void WriteMethodBody(MethodHeader methodHeader, MethodInformation methodInformation)
    {
        _codeBuilder.WriteBeginScope()
                    .WriteLine($"var awaitedMethodContainingInstance ={(methodHeader.InterfaceSpecifier != null ? $" ({methodHeader.InterfaceSpecifier})" : string.Empty)} await {methodHeader.Parameters.First().Name};");

        if (methodInformation.IsAsyncEnumerable)
        {
            _codeBuilder
                .WriteLine($"await foreach(var resultItem in awaitedMethodContainingInstance.{methodHeader.Name}{WriteMethodTypeParameter(methodHeader)}({string.Join(", ", methodHeader.Parameters.Skip(1).Select(x => x.Name))}))")
                .WriteBeginScope()
                .WriteLine("yield return resultItem;")
                .WriteEndScope();
        }
        else if (methodInformation.IsEnumerable)
        {
            _codeBuilder
                .WriteLine($"foreach(var resultItem in awaitedMethodContainingInstance.{methodHeader.Name}{WriteMethodTypeParameter(methodHeader)}({string.Join(", ", methodHeader.Parameters.Skip(1).Select(x => x.Name))}))")
                .WriteBeginScope()
                .WriteLine("yield return resultItem;")
                .WriteEndScope();
        }
        else if (methodInformation.IsAwaitable)
        {
            _codeBuilder.WriteLine($"return await awaitedMethodContainingInstance.{methodHeader.Name}{WriteMethodTypeParameter(methodHeader)}({string.Join(", ", methodHeader.Parameters.Skip(1).Select(x => x.Name))});");
        }
        else
        {
            _codeBuilder.WriteLine($"return awaitedMethodContainingInstance.{methodHeader.Name}{WriteMethodTypeParameter(methodHeader)}({string.Join(", ", methodHeader.Parameters.Skip(1).Select(x => x.Name))});");

        }

        _codeBuilder.WriteEndScope();

        static string WriteMethodTypeParameter(MethodHeader methodHeader)
        {
            var methodTypeParameters = methodHeader.Types.Where(x => x.IsMethodTypeParameter).Select(y => y.Name);
            if (!methodTypeParameters.Any())
                return string.Empty;
            return $"<{string.Join(", ", methodTypeParameters)}>";
        }
    }

    private void WriteMethodHeader(AsyncFluentMethodConfiguration configuration, MethodHeader methodHeader)
    {
        if (configuration.IncludeAttributes)
        {
            WriteAttributes(methodHeader.Attributes, false, singleline: false);
            WriteAttributes(methodHeader.ReturnTypeAttributes, true, singleline: false);
        }
        WriteModifiers(methodHeader.Modifiers);
        WriteType(methodHeader.ReturnType);
        WriteIdentifier(configuration.MethodName ?? methodHeader.Name);
        WriteTypeArguments(methodHeader.Types);
        WriteMethodParameters(methodHeader.Parameters, configuration.IncludeAttributes);
        WriteMethodConstrains(methodHeader.Types);
        _codeBuilder.WriteLine();
    }

    private void WriteAttributes(IEnumerable<string> attributes, bool isReturnAttribute, bool singleline)
    {
        foreach (var attribute in attributes)
        {
            _codeBuilder.Write($"[");
            if (isReturnAttribute)
                _codeBuilder.Write("return: ");
            _codeBuilder.Write($"{attribute.Trim()}]");

            if (singleline)
            {
                _codeBuilder.Write(" ");
            }
            else
            {
                _codeBuilder.WriteLine();
            }
        }
    }

    private void WriteModifiers(IEnumerable<string> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            _codeBuilder.Write($"{modifier.Trim()} ");
        }
    }

    private void WriteType(string returnType)
    {
        _codeBuilder.Write($"{returnType.Trim()} ");
    }

    private void WriteIdentifier(string name)
    {
        _codeBuilder.Write(name.Trim());
    }

    private void WriteTypeArguments(IEnumerable<MethodTypeParameter> types)
    {
        if (!types.Any())
            return;

        _codeBuilder.Write("<");
        foreach (var type in types.Take(types.Count() - 1))
        {
            _codeBuilder.Write($"{type.Name.Trim()}, ");
        }
        _codeBuilder.Write($"{types.Last().Name.Trim()}>");
    }

    private void WriteMethodParameters(IEnumerable<MethodParameter> parameters, bool includeAttributes)
    {
        _codeBuilder.Write("(");
        foreach (var parameter in parameters.Take(parameters.Count() - 1))
        {
            WriteMethodParameter(parameter, includeAttributes);
            _codeBuilder.Write(", ");
        }
        WriteMethodParameter(parameters.Last(), includeAttributes);
        _codeBuilder.Write(")");
    }

    private void WriteMethodParameter(MethodParameter parameter, bool includeAttributes)
    {
        if (includeAttributes)
            WriteAttributes(parameter.Attributes, false, true);
        WriteModifiers(parameter.Modifiers);
        WriteType(parameter.Type);
        WriteIdentifier(parameter.Name);
        WriteDefaultValue(parameter.DefaultValue);
    }

    private void WriteMethodConstrains(IEnumerable<MethodTypeParameter> types)
    {
        var typesWithConstraints = types.Where(x => x.PrimaryConstraint is not null || x.SecondaryConstraints.Any() || x.HasConstructorConstraint);
        _codeBuilder.WriteBeginScope(false);
        foreach (var typeWithConstraints in typesWithConstraints)
        {
            _codeBuilder.WriteLine();
            _codeBuilder.Write($"where {typeWithConstraints.Name}:");
            if (typeWithConstraints.PrimaryConstraint is not null)
            {
                _codeBuilder.Write($"{typeWithConstraints.PrimaryConstraint.Trim()}");
                if (typeWithConstraints.SecondaryConstraints.Any() || typeWithConstraints.HasConstructorConstraint)
                    _codeBuilder.Write(", ");
            }

            if (typeWithConstraints.SecondaryConstraints.Any())
            {
                foreach (var secondaryConstraint in typeWithConstraints.SecondaryConstraints.Take(typeWithConstraints.SecondaryConstraints.Count() - 1))
                {
                    _codeBuilder.Write($"{secondaryConstraint.Trim()}, ");
                }
                _codeBuilder.Write($"{typeWithConstraints.SecondaryConstraints.Last().Trim()}");
                if (typeWithConstraints.HasConstructorConstraint)
                    _codeBuilder.Write(", ");
            }

            if (typeWithConstraints.HasConstructorConstraint)
            {
                _codeBuilder.Write($"new()");
            }
        }
        _codeBuilder.WriteEndScope(false);
    }

    private void WriteDefaultValue(string? defaultValue)
    {
        if (defaultValue != null)
        {
            _codeBuilder.Write($" = {defaultValue.Trim()}");
        }
    }
}