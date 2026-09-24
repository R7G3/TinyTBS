// Proving grounds — richer script surface for hook testing.
public void OnPlayerTurnStart(MapScriptContext context)
{
    // Small income bonus so HUD gold changes are visible while testing.
    context.AddMoney(context.PlayerId, 2);
}

public void OnAfterPlayerAction(MapScriptContext context)
{
}
