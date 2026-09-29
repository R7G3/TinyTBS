namespace TinyTBS.Game.Editor.Writers;

/// <summary>Default map <c>script.cs</c> template for new maps.</summary>
public static class MapScriptTemplates
{
    public const string EmptyHooks = """
// Map script — turn/action hooks (TinyTBS Editor template).
public void OnPlayerTurnStart(MapScriptContext context)
{
}

public void OnAfterPlayerAction(MapScriptContext context)
{
}

""";
}
