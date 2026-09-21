// Demo map board script. Methods are compiled as members of a generated IMapScriptHooks class.

public void OnPlayerTurnStart(MapScriptContext context)
{
    // Tiny grant so gold in the HUD proves the hook ran.
    context.AddMoney(context.PlayerId, 1);
}

public void OnAfterPlayerAction(MapScriptContext context)
{
    // var action = context.LastAction;
}
