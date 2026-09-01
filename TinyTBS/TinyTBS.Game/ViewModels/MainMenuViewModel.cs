using TinyTBS.Core.Assets;

namespace TinyTBS.Game.ViewModels;

/// <summary>
/// View model for the main menu. Gum controls sync manually in the screen.
/// </summary>
public sealed class MainMenuViewModel
{
    public const string VanillaModLabel = "Vanilla";

    public string Title { get; set; } = "TinyTBS";

    public IReadOnlyList<string> ModOptions { get; private set; } = [VanillaModLabel];

    public string SelectedModOption { get; set; } = VanillaModLabel;

    public void RefreshMods(IAssetResolver assets)
    {
        var options = new List<string> { VanillaModLabel };
        options.AddRange(assets.ListMods());
        ModOptions = options;

        SelectedModOption = string.IsNullOrWhiteSpace(assets.ActiveModId)
            ? VanillaModLabel
            : assets.ActiveModId;
    }

    public void ApplyModSelection(IAssetResolver assets)
    {
        assets.ActiveModId = SelectedModOption == VanillaModLabel
            ? null
            : SelectedModOption;
    }
}
