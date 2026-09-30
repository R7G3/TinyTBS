using Microsoft.CodeAnalysis;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Allowlist of symbols a sandboxed script may reference: a small, deterministic slice of the framework,
/// the script API namespaces, and the script's own declarations. Everything else is rejected.
/// </summary>
internal sealed class ScriptSandboxPolicy
{
    private const string SystemNamespace = "System";

    private static readonly string[] FrameworkNamespaces =
    [
        SystemNamespace,
        "System.Collections.Generic",
        "System.Text",
    ];

    /// <summary>
    /// Deterministic value types, math, strings, collections and common exceptions.
    /// No reflection, IO, threading, LINQ, randomness, environment or interop.
    /// </summary>
    private static readonly HashSet<string> AllowedSystemTypes = new(StringComparer.Ordinal)
    {
        "Object", "ValueType", "Enum", "Array", "String", "Char", "Boolean",
        "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
        "Single", "Double", "Decimal", "Math", "MathF", "Convert", "HashCode", "TimeSpan",
        "Index", "Range", "Nullable", "ValueTuple", "Tuple", "FormattableString",
        "StringComparison", "StringComparer", "StringSplitOptions", "MidpointRounding",
        "Func", "Action", "Predicate", "Comparison", "Converter", "EventHandler", "EventArgs",
        "IComparable", "IEquatable", "IDisposable",
        "Exception", "InvalidOperationException", "ArgumentException", "ArgumentNullException",
        "ArgumentOutOfRangeException", "NotSupportedException", "NotImplementedException",
        "IndexOutOfRangeException", "FormatException", "OverflowException", "DivideByZeroException",
        "ArithmeticException", "NullReferenceException",
    };

    private static readonly HashSet<string> AllowedTextTypes = new(StringComparer.Ordinal)
    {
        "StringBuilder",
    };

    /// <summary>Scripts may only catch specific exceptions, never the base types that would swallow budget stops.</summary>
    private static readonly HashSet<string> AllowedCatchTypes = new(StringComparer.Ordinal)
    {
        "System.InvalidOperationException",
        "System.ArgumentException",
        "System.ArgumentNullException",
        "System.ArgumentOutOfRangeException",
        "System.Collections.Generic.KeyNotFoundException",
        "System.FormatException",
        "System.OverflowException",
        "System.DivideByZeroException",
        "System.ArithmeticException",
        "System.IndexOutOfRangeException",
        "System.NullReferenceException",
        "System.NotSupportedException",
    };

    private static readonly HashSet<string> ForbiddenMembers = new(StringComparer.Ordinal)
    {
        "System.Object.GetType",
        "System.Object.MemberwiseClone",
    };

    private readonly HashSet<string> _apiNamespaces;
    private readonly HashSet<string> _forbiddenApiTypes;
    private readonly HashSet<string> _allowedNamespaceNames;
    private readonly IAssemblySymbol _scriptAssembly;

    public ScriptSandboxPolicy(
        IReadOnlyList<string> apiNamespaces,
        IReadOnlyList<string> forbiddenApiTypes,
        string generatedNamespace,
        IAssemblySymbol scriptAssembly)
    {
        _apiNamespaces = new HashSet<string>(apiNamespaces, StringComparer.Ordinal);
        _forbiddenApiTypes = new HashSet<string>(forbiddenApiTypes, StringComparer.Ordinal);
        _scriptAssembly = scriptAssembly;
        _allowedNamespaceNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var namespaceName in FrameworkNamespaces.Concat(apiNamespaces).Append(generatedNamespace))
            AddNamespaceWithParents(namespaceName);
        _allowedNamespaceNames.Add("System.Collections");
    }

    public bool IsAllowedSymbol(ISymbol symbol, out string reason)
    {
        reason = string.Empty;
        switch (symbol)
        {
            case IAliasSymbol alias:
                return IsAllowedSymbol(alias.Target, out reason);
            case INamespaceSymbol namespaceSymbol:
                if (namespaceSymbol.IsGlobalNamespace || _allowedNamespaceNames.Contains(namespaceSymbol.ToDisplayString()))
                    return true;
                reason = $"namespace '{namespaceSymbol.ToDisplayString()}' is not available to scripts";
                return false;
            case ITypeSymbol type:
                return IsAllowedType(type, out reason);
            case IMethodSymbol method:
                return IsAllowedMethod(method, out reason);
            case IPropertySymbol property:
                return IsAllowedMember(property, property.Type, out reason);
            case IFieldSymbol field:
                return IsAllowedMember(field, field.Type, out reason);
            case IEventSymbol eventSymbol:
                return IsAllowedMember(eventSymbol, eventSymbol.Type, out reason);
            case ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or ILabelSymbol or IDiscardSymbol:
                return true;
            default:
                reason = $"'{symbol.ToDisplayString()}' is not available to scripts";
                return false;
        }
    }

    public bool IsAllowedType(ITypeSymbol type, out string reason)
    {
        reason = string.Empty;
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return IsAllowedType(array.ElementType, out reason);
            case INamedTypeSymbol named:
                return IsAllowedNamedType(named, out reason);
            default:
                reason = $"type '{type.ToDisplayString()}' is not available to scripts";
                return false;
        }
    }

    public bool IsAllowedCatchType(ITypeSymbol type) =>
        AllowedCatchTypes.Contains(type.OriginalDefinition.ToDisplayString());

    private bool IsAllowedMethod(IMethodSymbol method, out string reason)
    {
        if (method.MethodKind is MethodKind.AnonymousFunction or MethodKind.LambdaMethod or MethodKind.LocalFunction)
        {
            reason = string.Empty;
            return true;
        }

        var declared = method.ReducedFrom ?? method;
        if (!IsAllowedMember(declared, returnType: null, out reason))
            return false;
        if (method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor)
            && !method.ReturnsVoid
            && !IsAllowedType(method.ReturnType, out reason))
        {
            reason = $"'{method.ToDisplayString()}' returns a type that is not available to scripts";
            return false;
        }

        foreach (var typeArgument in method.TypeArguments)
        {
            if (!IsAllowedType(typeArgument, out reason))
                return false;
        }

        return true;
    }

    private bool IsAllowedMember(ISymbol member, ITypeSymbol? returnType, out string reason)
    {
        reason = string.Empty;
        var containingType = member.ContainingType;
        if (containingType is null)
            return true;

        var memberName = containingType.OriginalDefinition.ToDisplayString() + "." + member.Name;
        if (ForbiddenMembers.Contains(memberName))
        {
            reason = $"'{memberName}' is not available to scripts";
            return false;
        }

        if (!IsAllowedType(containingType, out reason))
            return false;

        if (returnType is not null && !IsAllowedType(returnType, out _))
        {
            reason = $"'{member.ToDisplayString()}' has a type that is not available to scripts";
            return false;
        }

        return true;
    }

    private bool IsAllowedNamedType(INamedTypeSymbol named, out string reason)
    {
        reason = string.Empty;
        if (named.IsAnonymousType)
            return true;

        foreach (var typeArgument in named.TypeArguments)
        {
            if (!IsAllowedType(typeArgument, out reason))
                return false;
        }

        var topLevel = named.OriginalDefinition;
        while (topLevel.ContainingType is not null)
            topLevel = topLevel.ContainingType;

        if (SymbolEqualityComparer.Default.Equals(topLevel.ContainingAssembly, _scriptAssembly))
            return true;

        var fullName = topLevel.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None));
        if (_forbiddenApiTypes.Contains(fullName))
        {
            reason = $"'{fullName}' is reserved for the script host";
            return false;
        }

        var namespaceName = topLevel.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var allowed = namespaceName switch
        {
            SystemNamespace => AllowedSystemTypes.Contains(topLevel.Name),
            "System.Collections.Generic" => true,
            "System.Text" => AllowedTextTypes.Contains(topLevel.Name),
            _ => _apiNamespaces.Contains(namespaceName),
        };

        if (!allowed)
            reason = $"type '{fullName}' is not available to scripts";
        return allowed;
    }

    private void AddNamespaceWithParents(string namespaceName)
    {
        var current = namespaceName;
        while (!string.IsNullOrEmpty(current))
        {
            _allowedNamespaceNames.Add(current);
            var lastDot = current.LastIndexOf('.');
            current = lastDot < 0 ? string.Empty : current[..lastDot];
        }
    }
}
