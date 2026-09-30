namespace TinyTBS.Scripting.Api;

/// <summary>Campaign meta hooks (chapter lifecycle, not per-turn map events).</summary>
public interface ICampaignScriptHooks
{
    void OnCampaignStarted(CampaignScriptContext context);

    void OnChapterStarted(CampaignScriptContext context);

    void OnChapterWon(CampaignScriptContext context);

    void OnChapterLost(CampaignScriptContext context);

    void OnCampaignCompleted(CampaignScriptContext context);
}
