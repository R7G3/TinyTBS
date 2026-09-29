namespace TinyTBS.Game.Editor.Writers;

/// <summary>Default <c>Campaign/script.cs</c> template.</summary>
public static class CampaignScriptTemplates
{
    public const string EmptyHooks = """
// Campaign meta hooks (TinyTBS Editor template).
public void OnCampaignStarted(CampaignScriptContext context)
{
}

public void OnChapterStarted(CampaignScriptContext context)
{
}

public void OnChapterWon(CampaignScriptContext context)
{
}

public void OnChapterLost(CampaignScriptContext context)
{
}

public void OnCampaignCompleted(CampaignScriptContext context)
{
}

""";
}
