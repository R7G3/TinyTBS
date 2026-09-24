// Vanilla demo map script — gold tick proves OnPlayerTurnStart.
public void OnPlayerTurnStart(MapScriptContext context)
{
    context.AddMoney(context.PlayerId, 1);
}

public void OnAfterPlayerAction(MapScriptContext context)
{
}
