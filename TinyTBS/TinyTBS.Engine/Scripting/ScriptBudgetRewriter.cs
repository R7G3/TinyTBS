using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Inserts budget checks into validated script code: a step tick at the top of every loop body and before
/// every <c>goto</c>, and a call-depth frame at the top of every function body (methods, local functions,
/// lambdas, accessors, constructors, operators). Endless loops and runaway recursion then stop themselves
/// instead of spinning a thread the host cannot abort.
/// </summary>
internal sealed class ScriptBudgetRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _model;
    private readonly string _budgetTypeName;
    private int _frameCounter;

    private ScriptBudgetRewriter(SemanticModel model, string budgetTypeName)
    {
        _model = model;
        _budgetTypeName = budgetTypeName.StartsWith("global::", StringComparison.Ordinal)
            ? budgetTypeName
            : "global::" + budgetTypeName;
    }

    public static SyntaxTree Rewrite(SyntaxTree tree, SemanticModel model, string budgetTypeName)
    {
        var rewriter = new ScriptBudgetRewriter(model, budgetTypeName);
        var root = rewriter.Visit(tree.GetRoot());
        return tree.WithRootAndOptions(root, tree.Options);
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var visited = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return visited.WithStatement(PrependTick(visited.Statement));
    }

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
    {
        var visited = (DoStatementSyntax)base.VisitDoStatement(node)!;
        return visited.WithStatement(PrependTick(visited.Statement));
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var visited = (ForStatementSyntax)base.VisitForStatement(node)!;
        return visited.WithStatement(PrependTick(visited.Statement));
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return visited.WithStatement(PrependTick(visited.Statement));
    }

    public override SyntaxNode? VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        var visited = (ForEachVariableStatementSyntax)base.VisitForEachVariableStatement(node)!;
        return visited.WithStatement(PrependTick(visited.Statement));
    }

    public override SyntaxNode? VisitGotoStatement(GotoStatementSyntax node)
    {
        var visited = (GotoStatementSyntax)base.VisitGotoStatement(node)!;
        return SyntaxFactory.Block(TickStatement(), visited);
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var returnsVoid = _model.GetDeclaredSymbol(node)?.ReturnsVoid ?? true;
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var returnsVoid = (_model.GetDeclaredSymbol(node) as IMethodSymbol)?.ReturnsVoid ?? true;
        var visited = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var visited = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid: true))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        var visited = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid: false))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        var visited = (ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid: false))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        var returnsVoid = !node.IsKind(SyntaxKind.GetAccessorDeclaration);
        var visited = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node)!;
        if (visited.Body is not null)
            return visited.WithBody(PrependFrame(visited.Body));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBody(FrameBlock(visited.ExpressionBody.Expression, returnsVoid))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (visited.ExpressionBody is null)
            return visited;

        var getter = SyntaxFactory.AccessorDeclaration(
            SyntaxKind.GetAccessorDeclaration,
            FrameBlock(visited.ExpressionBody.Expression, returnsVoid: false));
        return visited
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        var visited = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!;
        if (visited.ExpressionBody is null)
            return visited;

        var getter = SyntaxFactory.AccessorDeclaration(
            SyntaxKind.GetAccessorDeclaration,
            FrameBlock(visited.ExpressionBody.Expression, returnsVoid: false));
        return visited
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        var returnsVoid = LambdaReturnsVoid(node);
        var visited = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node)!;
        if (visited.Block is not null)
            return visited.WithBlock(PrependFrame(visited.Block));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBlock(FrameBlock(visited.ExpressionBody, returnsVoid))
            .WithExpressionBody(null);
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        var returnsVoid = LambdaReturnsVoid(node);
        var visited = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
        if (visited.Block is not null)
            return visited.WithBlock(PrependFrame(visited.Block));
        if (visited.ExpressionBody is null)
            return visited;

        return visited
            .WithBlock(FrameBlock(visited.ExpressionBody, returnsVoid))
            .WithExpressionBody(null);
    }

    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
    {
        var visited = (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node)!;
        return visited.WithBlock(PrependFrame(visited.Block));
    }

    private bool LambdaReturnsVoid(LambdaExpressionSyntax lambda) =>
        (_model.GetSymbolInfo(lambda).Symbol as IMethodSymbol)?.ReturnsVoid ?? true;

    private StatementSyntax PrependTick(StatementSyntax statement) =>
        statement is BlockSyntax block
            ? block.WithStatements(block.Statements.Insert(0, TickStatement()))
            : SyntaxFactory.Block(TickStatement(), statement);

    private BlockSyntax PrependFrame(BlockSyntax body) =>
        body.WithStatements(body.Statements.Insert(0, FrameStatement()));

    private BlockSyntax FrameBlock(ExpressionSyntax expression, bool returnsVoid)
    {
        StatementSyntax bodyStatement = expression switch
        {
            ThrowExpressionSyntax throwExpression => SyntaxFactory.ThrowStatement(
                KeywordWithSpace(SyntaxKind.ThrowKeyword),
                throwExpression.Expression,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            _ when returnsVoid => SyntaxFactory.ExpressionStatement(expression),
            _ => SyntaxFactory.ReturnStatement(
                KeywordWithSpace(SyntaxKind.ReturnKeyword),
                expression,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
        };
        return SyntaxFactory.Block(FrameStatement(), bodyStatement);
    }

    private static SyntaxToken KeywordWithSpace(SyntaxKind kind) =>
        SyntaxFactory.Token(SyntaxFactory.TriviaList(), kind, SyntaxFactory.TriviaList(SyntaxFactory.Space));

    private StatementSyntax TickStatement() =>
        SyntaxFactory.ParseStatement($"{_budgetTypeName}.Tick();");

    private StatementSyntax FrameStatement() =>
        SyntaxFactory.ParseStatement(
            $"using var __tinyTbsBudgetFrame{++_frameCounter} = {_budgetTypeName}.EnterFrame();");
}
