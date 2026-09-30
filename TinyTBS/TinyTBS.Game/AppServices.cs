using TinyTBS.Engine.Input;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Flow;
using TinyTBS.Game.Input;

namespace TinyTBS.Game;

/// <summary>
/// Long-lived application services. Screens read this instead of casting <see cref="Microsoft.Xna.Framework.Game"/>
/// and constructing catalogs themselves.
/// </summary>
public sealed class AppServices
{
    public required IUserDataPaths UserDataPaths { get; init; }

    public required IFileSystem Files { get; init; }

    public required IAssetResolver Assets { get; init; }

    public required IExternalFilePicker FilePicker { get; init; }

    public required IExternalUriLauncher UriLauncher { get; init; }

    public required IGameCommandSource Commands { get; init; }

    public required IPointerSource Pointer { get; init; }

    public required SaveResumeService Saves { get; init; }

    public required CampaignFlowService Campaigns { get; init; }

    public required NewGameSetupService NewGame { get; init; }
}
