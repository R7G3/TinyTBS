using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Semantic sandbox check: every referenced symbol must pass <see cref="ScriptSandboxPolicy"/>,
/// and constructs that escape the managed, single-threaded model are rejected outright.
/// Runs on the compiled user tree, so tricks like fully qualified names or string-built identifiers
/// are resolved exactly as the compiler sees them.
/// </summary>
internal static class ScriptSandboxValidator
{
    private static readonly HashSet<SyntaxKind> ForbiddenTokenKinds =
    [
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.AsyncKeyword,
        SyntaxKind.FixedKeyword,
        SyntaxKind.StackAllocKeyword,
        SyntaxKind.ArgListKeyword,
        SyntaxKind.MakeRefKeyword,
        SyntaxKind.RefTypeKeyword,
        SyntaxKind.RefValueKeyword,
    ];

    public static IReadOnlyList<string> Validate(
        CSharpCompilation compilation,
        SyntaxTree userTree,
        ScriptSandboxPolicy policy)
    {
        var walker = new Walker(compilation.GetSemanticModel(userTree), policy);
        var root = userTree.GetRoot();
        walker.Visit(root);

        foreach (var token in root.DescendantTokens())
        {
            if (ForbiddenTokenKinds.Contains(token.Kind()))
                walker.Report(token.GetLocation(), $"'{token.Text}' is not allowed in scripts");
        }

        return walker.Violations;
    }

    private sealed class Walker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        private readonly ScriptSandboxPolicy _policy;
        private readonly List<string> _violations = [];
        private readonly HashSet<string> _reported = new(StringComparer.Ordinal);

        public Walker(SemanticModel model, ScriptSandboxPolicy policy)
        {
            _model = model;
            _policy = policy;
        }

        public IReadOnlyList<string> Violations => _violations;

        public override void Visit(SyntaxNode? node)
        {
            if (node is null)
                return;

            CheckNode(node);
            base.Visit(node);
        }

        public void Report(Location location, string message)
        {
            var line = location.GetMappedLineSpan().StartLinePosition.Line + 1;
            var entry = $"line {line}: {message}";
            if (_reported.Add(entry))
                _violations.Add(entry);
        }

        private void CheckNode(SyntaxNode node)
        {
            switch (node)
            {
                case TypeOfExpressionSyntax:
                    Report(node.GetLocation(), "'typeof' is not allowed in scripts");
                    break;
                case AttributeListSyntax:
                    Report(node.GetLocation(), "attributes are not allowed in scripts");
                    break;
                case DestructorDeclarationSyntax:
                    Report(node.GetLocation(), "finalizers are not allowed in scripts");
                    break;
                case PointerTypeSyntax or FunctionPointerTypeSyntax:
                    Report(node.GetLocation(), "pointers are not allowed in scripts");
                    break;
                case LockStatementSyntax:
                    Report(node.GetLocation(), "'lock' is not allowed in scripts");
                    break;
                case AwaitExpressionSyntax:
                    Report(node.GetLocation(), "'await' is not allowed in scripts");
                    break;
                case YieldStatementSyntax:
                    Report(node.GetLocation(), "iterators ('yield') are not allowed in scripts");
                    break;
                case CatchClauseSyntax catchClause:
                    CheckCatch(catchClause);
                    break;
                case SimpleNameSyntax name:
                    CheckSymbol(name, _model.GetSymbolInfo(name).Symbol);
                    break;
                case ImplicitObjectCreationExpressionSyntax creation:
                    CheckType(creation, _model.GetTypeInfo(creation).Type);
                    break;
                case ObjectCreationExpressionSyntax creation:
                    CheckSymbol(creation, _model.GetSymbolInfo(creation).Symbol);
                    break;
                case ElementAccessExpressionSyntax access:
                    CheckSymbol(access, _model.GetSymbolInfo(access).Symbol);
                    break;
            }
        }

        private void CheckCatch(CatchClauseSyntax catchClause)
        {
            if (catchClause.Declaration is null)
            {
                Report(catchClause.GetLocation(), "catch without an exception type is not allowed in scripts");
                return;
            }

            var caughtType = _model.GetTypeInfo(catchClause.Declaration.Type).Type;
            if (caughtType is null || !_policy.IsAllowedCatchType(caughtType))
            {
                Report(
                    catchClause.Declaration.GetLocation(),
                    $"catching '{caughtType?.ToDisplayString() ?? catchClause.Declaration.Type.ToString()}' is not allowed in scripts");
            }
        }

        private void CheckSymbol(SyntaxNode node, ISymbol? symbol)
        {
            if (symbol is null)
                return;
            if (!_policy.IsAllowedSymbol(symbol, out var reason))
                Report(node.GetLocation(), reason);
        }

        private void CheckType(SyntaxNode node, ITypeSymbol? type)
        {
            if (type is null)
                return;
            if (!_policy.IsAllowedType(type, out var reason))
                Report(node.GetLocation(), reason);
        }
    }
}
