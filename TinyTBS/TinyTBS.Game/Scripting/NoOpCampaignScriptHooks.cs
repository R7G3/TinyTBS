using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>Default no-op campaign script.</summary>
public sealed class NoOpCampaignScriptHooks : ICampaignScriptHooks
{
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
}
