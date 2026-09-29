using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Presentation.NewGame;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// New Game tabbed flow: Mode → Scenario → Level → Composition → Lobby → loading.
/// Lobby slots: Local / Bot (Easy·Normal) / Remote greyed.
/// </summary>
public sealed class NewGameScreen : GameScreen
{
    private const int GoldStep = 50;
    private const int UnitCapStep = 1;
    private const int MinGold = 0;
    private const int MaxGold = 9999;
    private const int MinUnitCap = 1;
    private const int MaxUnitCap = 99;

    private readonly IAssetResolver _assets;
    private readonly NewGameViewModel _viewModel = new();
    private readonly NewGameView _view = new();
    private readonly List<MatchPlayerSeat> _playerSeats = [];
    private readonly Dictionary<string, ScenarioLevelInfo[]> _levelsByScenario = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CampaignDefinition> _campaignByScenario = new(StringComparer.Ordinal);
    private readonly CampaignProgressStore _campaignProgressStore;

    private MainMenuBackground? _background;
    private ContentModuleLibrary? _moduleLibrary;
    private ContentModuleLocator? _moduleLocator;
    private ContentBundleLibrary? _bundleLibrary;
    private ContentModuleInfo[] _allScenarios = [];
    private bool _lobbyInitializedForLevel;
    private NewGameFocusAnchor _focusAnchor = NewGameFocusAnchor.Auto;

    public NewGameScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
        _campaignProgressStore = new CampaignProgressStore(game.UserDataPaths);
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        TinyGame.UserDataPaths.EnsureCreated();
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _moduleLocator = new ContentModuleLocator(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleLibrary = new ContentBundleLibrary(TinyGame.Files, TinyGame.UserDataPaths);

        _viewModel.ActiveTab = NewGameTab.Mode;
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        Refresh("Pick Campaign or Skirmish, then scenario and level.");
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _moduleLibrary = null;
        _moduleLocator = null;
        _bundleLibrary = null;
        _levelsByScenario.Clear();
        _campaignByScenario.Clear();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.ApplyResponsiveLayout();
        _view.HandleInput(
            TinyGame.Commands,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            TinyGame.Pointer);
        _view.HandlePointerScroll(TinyGame.Pointer.ScrollWheelDelta);

        if (_viewModel.ShowAddPlayerTypeChooser
            && (TinyGame.Commands.WasPressed(GameCommand.Back)
                || TinyGame.Commands.WasPressed(GameCommand.Cancel)))
        {
            CancelAddPlayerChooser();
            return;
        }

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            GoToMainMenu();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));

        var texture = _background?.Texture;
        if (texture is not null)
        {
            ViewportFit.DrawCentered(
                TinyGame.SharedSpriteBatch,
                texture,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                Color.White * 0.35f);
        }

        GumService.Default.Draw();
    }

    private void Refresh(string statusText)
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_moduleLocator);
        ArgumentNullException.ThrowIfNull(_bundleLibrary);

        RebuildScenarioCatalog();
        PruneSelectionAgainstMode();

        _viewModel.Modes =
        [
            new NewGameModeRowViewModel
            {
                Mode = NewGamePlayMode.Campaign,
                Title = "Campaign",
                DetailLine = "story levels, Local vs Bot",
                IsSelected = _viewModel.SelectedMode == NewGamePlayMode.Campaign,
            },
            new NewGameModeRowViewModel
            {
                Mode = NewGamePlayMode.Skirmish,
                Title = "Skirmish",
                DetailLine = "gold, unit cap, slots",
                IsSelected = _viewModel.SelectedMode == NewGamePlayMode.Skirmish,
            },
        ];

        var filteredScenarios = FilterScenariosForMode(_viewModel.SelectedMode);
        _viewModel.Scenarios = filteredScenarios
            .Select(module => new NewGameScenarioRowViewModel
            {
                ModuleId = module.ModuleId,
                Title = module.Title,
                SourceLabel = module.Source == ContentModuleSource.UserLibrary ? "user" : "bundled",
                IsSelected = module.ModuleId == _viewModel.SelectedScenarioModuleId,
            })
            .ToArray();

        var filteredLevels = FilterLevelsForSelection();
        var unlocked = ResolveUnlockedLevelIds();
        _viewModel.Levels = filteredLevels
            .Select(level => new NewGameLevelRowViewModel
            {
                LevelId = level.LevelId,
                Title = level.Title,
                ModesLabel = FormatModesLabel(level.Modes),
                IsSelected = level.LevelId == _viewModel.SelectedLevelId,
                IsLocked = _viewModel.SelectedMode == NewGamePlayMode.Campaign
                    && unlocked is not null
                    && !unlocked.Contains(level.LevelId),
            })
            .ToArray();

        var selectedLevel = filteredLevels.FirstOrDefault(level => level.LevelId == _viewModel.SelectedLevelId);
        ApplyLobbyFromLevel(selectedLevel);

        ScenarioModuleDefinition? scenarioDefinition = null;
        if (!string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId))
        {
            try
            {
                var scenarioRoot = _moduleLocator.ResolveModuleRoot(_viewModel.SelectedScenarioModuleId);
                scenarioDefinition = ScenarioModuleLoader.Load(scenarioRoot, TinyGame.Files);
            }
            catch (Exception exception)
            {
                statusText = "Scenario load issue: " + exception.Message;
            }
        }

        var bundles = _bundleLibrary.ListEffectiveBundles();
        EnsureCompositionSource(bundles);
        _viewModel.CompositionOptions = BuildCompositionOptions(bundles);
        var compositionSummary = BuildCompositionSummary(scenarioDefinition, bundles);
        if (compositionSummary.StartsWith("Composition error:", StringComparison.Ordinal))
            statusText = compositionSummary;

        _viewModel.CompositionSummary = compositionSummary;
        _viewModel.TabEmptyHint = BuildTabEmptyHint();
        _viewModel.StatusText = statusText;

        var focusAnchor = _focusAnchor;
        _focusAnchor = NewGameFocusAnchor.Auto;

        _view.Build(
            _viewModel,
            onSelectTab: SelectTab,
            onSelectMode: SelectMode,
            onSelectScenario: SelectScenario,
            onSelectLevel: SelectLevel,
            onSelectComposition: SelectComposition,
            onOpenAddPlayerChooser: OpenAddPlayerChooser,
            onAddLocalPlayer: AddLocalPlayer,
            onAddBotPlayer: AddBotPlayer,
            onCancelAddPlayerChooser: CancelAddPlayerChooser,
            onRemovePlayerAt: RemovePlayerAt,
            onActivatePlayerSlot: ActivatePlayerSlot,
            onDecreaseGold: () => AdjustGold(-GoldStep),
            onIncreaseGold: () => AdjustGold(GoldStep),
            onDecreaseUnitCap: () => AdjustUnitCap(-UnitCapStep),
            onIncreaseUnitCap: () => AdjustUnitCap(UnitCapStep),
            onStart: StartMatch,
            onBack: GoToMainMenu,
            focusAnchor: focusAnchor);
    }

    private void RebuildScenarioCatalog()
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_moduleLocator);

        _allScenarios = _moduleLibrary.ListEffectiveModules()
            .Where(module => module.Type == ContentModuleType.Scenario)
            .OrderBy(module => module.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _levelsByScenario.Clear();
        _campaignByScenario.Clear();
        foreach (var scenario in _allScenarios)
        {
            try
            {
                var scenarioRoot = _moduleLocator.ResolveModuleRoot(scenario.ModuleId);
                _levelsByScenario[scenario.ModuleId] =
                    ScenarioLevelCatalog.ListLevels(scenarioRoot, TinyGame.Files).ToArray();

                var definition = ScenarioModuleLoader.Load(scenarioRoot, TinyGame.Files);
                var campaign = CampaignLoader.TryLoadFromScenario(
                    scenarioRoot,
                    definition.CampaignManifestRelativePath,
                    TinyGame.Files);
                if (campaign is not null)
                    _campaignByScenario[scenario.ModuleId] = campaign;
            }
            catch
            {
                _levelsByScenario[scenario.ModuleId] = [];
            }
        }
    }

    private void PruneSelectionAgainstMode()
    {
        if (_viewModel.SelectedMode is null)
        {
            _viewModel.SelectedScenarioModuleId = null;
            _viewModel.SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
            return;
        }

        var filteredScenarios = FilterScenariosForMode(_viewModel.SelectedMode);
        if (string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId)
            || filteredScenarios.All(scenario => scenario.ModuleId != _viewModel.SelectedScenarioModuleId))
        {
            _viewModel.SelectedScenarioModuleId = filteredScenarios.FirstOrDefault()?.ModuleId;
            _viewModel.SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }

        var filteredLevels = FilterLevelsForSelection();
        if (string.IsNullOrWhiteSpace(_viewModel.SelectedLevelId)
            || filteredLevels.All(level => level.LevelId != _viewModel.SelectedLevelId)
            || IsSelectedLevelLocked(filteredLevels))
        {
            _viewModel.SelectedLevelId = _viewModel.SelectedMode == NewGamePlayMode.Campaign
                ? PreferCampaignLevelId(filteredLevels)
                : PreferDefaultLevelId(filteredLevels);
            _lobbyInitializedForLevel = false;
        }
    }

    private bool IsSelectedLevelLocked(IReadOnlyList<ScenarioLevelInfo> filteredLevels)
    {
        if (_viewModel.SelectedMode != NewGamePlayMode.Campaign
            || string.IsNullOrWhiteSpace(_viewModel.SelectedLevelId))
        {
            return false;
        }

        var unlocked = ResolveUnlockedLevelIds();
        return unlocked is not null && !unlocked.Contains(_viewModel.SelectedLevelId);
    }

    private IReadOnlyList<ContentModuleInfo> FilterScenariosForMode(NewGamePlayMode? mode)
    {
        if (mode is null)
            return [];

        return _allScenarios
            .Where(scenario =>
                _levelsByScenario.TryGetValue(scenario.ModuleId, out var levels)
                && levels.Any(level => LevelMatchesMode(level, mode.Value)))
            .ToArray();
    }

    private IReadOnlyList<ScenarioLevelInfo> FilterLevelsForSelection()
    {
        if (_viewModel.SelectedMode is null
            || string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId)
            || !_levelsByScenario.TryGetValue(_viewModel.SelectedScenarioModuleId, out var levels))
        {
            return [];
        }

        var modeFiltered = levels
            .Where(level => LevelMatchesMode(level, _viewModel.SelectedMode.Value))
            .ToArray();

        if (_viewModel.SelectedMode != NewGamePlayMode.Campaign
            || !_campaignByScenario.TryGetValue(_viewModel.SelectedScenarioModuleId, out var campaign))
        {
            return modeFiltered;
        }

        // Campaign tab: order by campaign.json; drop levels not listed there.
        var byId = modeFiltered.ToDictionary(level => level.LevelId, StringComparer.Ordinal);
        var ordered = new List<ScenarioLevelInfo>();
        foreach (var chapter in campaign.Chapters)
        {
            if (byId.TryGetValue(chapter.LevelId, out var level))
                ordered.Add(level);
        }

        return ordered;
    }

    private HashSet<string>? ResolveUnlockedLevelIds()
    {
        if (_viewModel.SelectedMode != NewGamePlayMode.Campaign
            || string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId)
            || !_campaignByScenario.TryGetValue(_viewModel.SelectedScenarioModuleId, out var campaign))
        {
            return null;
        }

        var progress = _campaignProgressStore.TryLoadLatestForCampaign(
            _viewModel.SelectedScenarioModuleId,
            campaign.CampaignId);
        return CampaignProgressFactory.BuildUnlockedSet(progress, campaign);
    }

    private static string? PreferDefaultLevelId(IReadOnlyList<ScenarioLevelInfo> levels)
    {
        if (levels.Count == 0)
            return null;

        return levels.FirstOrDefault(level => level.LevelId == GameplaySessionFactory.ProvingGroundsLevelId)
                   ?.LevelId
               ?? levels[0].LevelId;
    }

    private string? PreferCampaignLevelId(IReadOnlyList<ScenarioLevelInfo> levels)
    {
        if (levels.Count == 0
            || string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId)
            || !_campaignByScenario.TryGetValue(_viewModel.SelectedScenarioModuleId, out var campaign))
        {
            return PreferDefaultLevelId(levels);
        }

        var progress = _campaignProgressStore.TryLoadLatestForCampaign(
            _viewModel.SelectedScenarioModuleId,
            campaign.CampaignId);
        var preferred = CampaignProgressFactory.PreferPlayableLevelId(campaign, progress);
        var unlocked = CampaignProgressFactory.BuildUnlockedSet(progress, campaign);

        if (levels.Any(level => level.LevelId == preferred && unlocked.Contains(preferred)))
            return preferred;

        return levels.FirstOrDefault(level => unlocked.Contains(level.LevelId))?.LevelId
            ?? PreferDefaultLevelId(levels);
    }

    private static bool LevelMatchesMode(ScenarioLevelInfo level, NewGamePlayMode mode) =>
        mode switch
        {
            NewGamePlayMode.Campaign => level.SupportsCampaign,
            NewGamePlayMode.Skirmish => level.SupportsSkirmish,
            _ => false,
        };

    private string BuildTabEmptyHint() =>
        _viewModel.ActiveTab switch
        {
            NewGameTab.Scenario when _viewModel.SelectedMode is null =>
                "Pick Campaign or Skirmish on the Mode tab first.",
            NewGameTab.Scenario =>
                "No scenario modules have levels for this mode.",
            NewGameTab.Level when _viewModel.SelectedMode is null =>
                "Pick Campaign or Skirmish on the Mode tab first.",
            NewGameTab.Level when string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId) =>
                "Pick a scenario on the Scenario tab first.",
            NewGameTab.Level =>
                "No levels for this scenario in the selected mode.",
            NewGameTab.Composition when string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId) =>
                "Pick a scenario first to choose composition.",
            NewGameTab.Lobby when string.IsNullOrWhiteSpace(_viewModel.SelectedLevelId) =>
                "Pick a level first to configure the lobby.",
            _ => string.Empty,
        };

    private void ApplyLobbyFromLevel(ScenarioLevelInfo? level)
    {
        _viewModel.ShowSkirmishLobby = _viewModel.SelectedMode == NewGamePlayMode.Skirmish;
        _viewModel.AllowEditPlayerSeats = level is not null;

        if (level is null)
        {
            _viewModel.LobbyNote = "Select a level to configure the match.";
            _viewModel.PlayersMin = 2;
            _viewModel.PlayersMax = 2;
            _viewModel.StartingGold = 0;
            _viewModel.UnitCap = MinUnitCap;
            _playerSeats.Clear();
            _viewModel.PlayerSlots = [];
            _viewModel.ShowAddPlayerTypeChooser = false;
            _viewModel.AllowEditPlayerSeats = false;
            _lobbyInitializedForLevel = false;
            return;
        }

        _viewModel.PlayersMin = level.PlayersMin;
        _viewModel.PlayersMax = level.PlayersMax;

        if (!_lobbyInitializedForLevel)
        {
            _viewModel.StartingGold = level.DefaultStartingGold;
            _viewModel.UnitCap = level.DefaultUnitCap;
            _playerSeats.Clear();
            var slotCount = Math.Clamp(level.PlayersDefaultSlots, level.PlayersMin, level.PlayersMax);
            for (var index = 0; index < slotCount; index++)
                _playerSeats.Add(CreateDefaultSeat(index));
            _lobbyInitializedForLevel = true;
            _viewModel.ShowAddPlayerTypeChooser = false;
        }

        ClampPlayerSlots();

        if (_playerSeats.Count >= _viewModel.PlayersMax)
            _viewModel.ShowAddPlayerTypeChooser = false;

        if (_viewModel.ShowSkirmishLobby)
        {
            _viewModel.LobbyNote =
                "Skirmish: Local and Bot (Easy/Normal). Confirm a Bot seat to cycle difficulty. Gold and unit cap below.";
        }
        else
        {
            _viewModel.LobbyNote =
                "Campaign: Player 2 defaults to Bot · Easy. Confirm a Bot seat to cycle Easy/Normal. Gold and unit cap come from the level.";
            _viewModel.StartingGold = level.DefaultStartingGold;
            _viewModel.UnitCap = level.DefaultUnitCap;
        }

        _viewModel.PlayerSlots = _playerSeats
            .Select((seat, index) => new NewGamePlayerSlotViewModel
            {
                SlotIndex = index,
                Kind = seat.Kind == MatchPlayerKind.Bot ? NewGamePlayerKind.Bot : NewGamePlayerKind.Local,
                BotDifficulty = seat.BotDifficulty,
                PaletteIndex = index,
            })
            .ToArray();
    }

    private MatchPlayerSeat CreateDefaultSeat(int slotIndex)
    {
        // Campaign: second seat is Bot Easy so the story opponent is ready out of the box.
        if (_viewModel.SelectedMode == NewGamePlayMode.Campaign && slotIndex == 1)
        {
            return new MatchPlayerSeat
            {
                Kind = MatchPlayerKind.Bot,
                BotDifficulty = BotDifficulty.Easy,
            };
        }

        return new MatchPlayerSeat { Kind = MatchPlayerKind.Local };
    }

    private void ClampPlayerSlots()
    {
        while (_playerSeats.Count > _viewModel.PlayersMax)
            _playerSeats.RemoveAt(_playerSeats.Count - 1);
        // Do not auto-fill Local when below min — that made it impossible to replace a seat with Bot.
        // Padding to min happens only on level init and right before StartMatch.
    }

    private void EnsureMinimumPlayerSeats()
    {
        while (_playerSeats.Count < _viewModel.PlayersMin)
            _playerSeats.Add(new MatchPlayerSeat { Kind = MatchPlayerKind.Local });
    }

    private void EnsureCompositionSource(IReadOnlyList<ContentBundleDefinition> bundles)
    {
        if (_viewModel.SelectedCompositionSourceId == NewGameCompositionSources.ScenarioDefaults)
            return;

        if (bundles.Any(bundle => bundle.BundleId == _viewModel.SelectedCompositionSourceId))
            return;

        _viewModel.SelectedCompositionSourceId = NewGameCompositionSources.ScenarioDefaults;
    }

    private IReadOnlyList<NewGameCompositionOptionViewModel> BuildCompositionOptions(
        IReadOnlyList<ContentBundleDefinition> bundles)
    {
        var options = new List<NewGameCompositionOptionViewModel>
        {
            new()
            {
                SourceId = NewGameCompositionSources.ScenarioDefaults,
                Title = "Scenario defaults",
                IsSelected = _viewModel.SelectedCompositionSourceId
                    == NewGameCompositionSources.ScenarioDefaults,
            },
        };

        foreach (var bundle in bundles)
        {
            options.Add(new NewGameCompositionOptionViewModel
            {
                SourceId = bundle.BundleId,
                Title = $"Bundle: {bundle.Title}",
                IsSelected = bundle.BundleId == _viewModel.SelectedCompositionSourceId,
            });
        }

        return options;
    }

    private string BuildCompositionSummary(
        ScenarioModuleDefinition? scenario,
        IReadOnlyList<ContentBundleDefinition> bundles)
    {
        try
        {
            var composition = ResolveComposition(scenario, bundles);
            if (composition is null)
                return "Composition: —";

            return "Composition: "
                + string.Join(", ", composition.UnitsModuleIds)
                + " · "
                + string.Join(", ", composition.BuildingsModuleIds)
                + " · "
                + composition.ThemeModuleId;
        }
        catch (Exception exception)
        {
            return "Composition error: " + exception.Message;
        }
    }

    private MatchContentComposition? ResolveComposition(
        ScenarioModuleDefinition? scenario,
        IReadOnlyList<ContentBundleDefinition> bundles)
    {
        if (scenario is null)
            return null;

        if (_viewModel.SelectedCompositionSourceId == NewGameCompositionSources.ScenarioDefaults)
            return MatchContentComposition.FromScenarioDefaults(scenario);

        var bundle = bundles.FirstOrDefault(
            candidate => candidate.BundleId == _viewModel.SelectedCompositionSourceId);
        if (bundle is null)
            return MatchContentComposition.FromScenarioDefaults(scenario);

        return new MatchContentComposition
        {
            ScenarioModuleId = scenario.ModuleId,
            UnitsModuleIds = bundle.Defaults.UnitsModuleIds,
            BuildingsModuleIds = bundle.Defaults.BuildingsModuleIds,
            ThemeModuleId = bundle.Defaults.ThemeModuleId,
            Replaces = scenario.Replaces,
        };
    }

    private static string FormatModesLabel(IReadOnlyList<string> modes)
    {
        if (modes.Count == 0)
            return string.Empty;

        return string.Join("/", modes);
    }

    private void SelectTab(NewGameTab tab)
    {
        _viewModel.ActiveTab = tab;
        if (tab != NewGameTab.Lobby)
            _viewModel.ShowAddPlayerTypeChooser = false;

        _focusAnchor = tab == NewGameTab.Lobby
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.Auto;
        Refresh(StatusForTab(tab));
    }

    private static string StatusForTab(NewGameTab tab) =>
        tab switch
        {
            NewGameTab.Mode => "Pick Campaign or Skirmish.",
            NewGameTab.Scenario => "Pick a scenario module.",
            NewGameTab.Level => "Pick a level.",
            NewGameTab.Composition => "Choose scenario defaults or a bundle preset.",
            NewGameTab.Lobby => "Review lobby settings, then Start.",
            _ => "New Game",
        };

    private void SelectMode(NewGamePlayMode mode)
    {
        if (_viewModel.SelectedMode != mode)
        {
            _viewModel.SelectedMode = mode;
            _viewModel.SelectedScenarioModuleId = null;
            _viewModel.SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }

        _viewModel.ActiveTab = NewGameTab.Scenario;
        _focusAnchor = NewGameFocusAnchor.SelectedScenario;
        Refresh(mode == NewGamePlayMode.Campaign
            ? "Campaign selected. Pick a scenario."
            : "Skirmish selected. Pick a scenario.");
    }

    private void SelectScenario(string moduleId)
    {
        if (_viewModel.SelectedScenarioModuleId != moduleId)
        {
            _viewModel.SelectedScenarioModuleId = moduleId;
            _viewModel.SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }

        _viewModel.ActiveTab = NewGameTab.Level;
        _focusAnchor = NewGameFocusAnchor.SelectedLevel;
        Refresh("Scenario selected. Pick a level.");
    }

    private void SelectLevel(string levelId)
    {
        var unlocked = ResolveUnlockedLevelIds();
        if (unlocked is not null && !unlocked.Contains(levelId))
        {
            Refresh("That chapter is locked.");
            return;
        }

        if (_viewModel.SelectedLevelId != levelId)
            _lobbyInitializedForLevel = false;

        _viewModel.SelectedLevelId = levelId;
        _viewModel.ActiveTab = NewGameTab.Composition;
        _focusAnchor = NewGameFocusAnchor.SelectedComposition;
        Refresh("Level selected. Choose composition, then lobby or Start.");
    }

    private void SelectComposition(string sourceId)
    {
        _viewModel.SelectedCompositionSourceId = sourceId;
        _viewModel.ActiveTab = NewGameTab.Lobby;
        _focusAnchor = NewGameFocusAnchor.RemovePlayer;
        Refresh("Composition updated. Review lobby, then Start.");
    }

    private void OpenAddPlayerChooser()
    {
        if (!_viewModel.AllowEditPlayerSeats || !_viewModel.CanAddPlayer)
            return;

        _viewModel.ShowAddPlayerTypeChooser = true;
        _focusAnchor = NewGameFocusAnchor.AddPlayerTypeLocal;
        Refresh("Choose player type.");
    }

    private void AddLocalPlayer()
    {
        if (!_viewModel.AllowEditPlayerSeats || !_viewModel.CanAddPlayer)
            return;

        _playerSeats.Add(new MatchPlayerSeat { Kind = MatchPlayerKind.Local });
        _viewModel.ShowAddPlayerTypeChooser = false;
        _focusAnchor = _playerSeats.Count > _viewModel.PlayersMin
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.AddPlayer;
        Refresh("Local player added.");
    }

    private void AddBotPlayer(BotDifficulty difficulty)
    {
        if (!_viewModel.AllowEditPlayerSeats || !_viewModel.CanAddPlayer)
            return;

        _playerSeats.Add(new MatchPlayerSeat
        {
            Kind = MatchPlayerKind.Bot,
            BotDifficulty = difficulty,
        });
        _viewModel.ShowAddPlayerTypeChooser = false;
        _focusAnchor = _playerSeats.Count > _viewModel.PlayersMin
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.AddPlayer;
        Refresh($"Bot ({difficulty}) added.");
    }

    private void CancelAddPlayerChooser()
    {
        if (!_viewModel.ShowAddPlayerTypeChooser)
            return;

        _viewModel.ShowAddPlayerTypeChooser = false;
        _focusAnchor = _playerSeats.Count > _viewModel.PlayersMin
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.AddPlayer;
        Refresh("Add player cancelled.");
    }

    private void RemovePlayerAt(int slotIndex)
    {
        if (!_viewModel.AllowEditPlayerSeats
            || !_viewModel.CanRemovePlayer
            || slotIndex < 0
            || slotIndex >= _playerSeats.Count)
        {
            return;
        }

        _playerSeats.RemoveAt(slotIndex);
        if (_playerSeats.Count < _viewModel.PlayersMin)
        {
            _viewModel.ShowAddPlayerTypeChooser = true;
            _focusAnchor = NewGameFocusAnchor.AddPlayerTypeLocal;
            Refresh("Player removed — add Local or Bot to reach the minimum.");
            return;
        }

        _viewModel.ShowAddPlayerTypeChooser = false;
        _focusAnchor = _playerSeats.Count > _viewModel.PlayersMin
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.AddPlayer;
        Refresh("Player removed.");
    }

    private void ActivatePlayerSlot(int slotIndex)
    {
        if (!_viewModel.AllowEditPlayerSeats
            || slotIndex < 0
            || slotIndex >= _playerSeats.Count)
        {
            return;
        }

        var seat = _playerSeats[slotIndex];
        if (seat.Kind != MatchPlayerKind.Bot)
        {
            _focusAnchor = NewGameFocusAnchor.RemovePlayer;
            Refresh("Local seat — remove (X) then + Bot to change type.");
            return;
        }

        seat.BotDifficulty = seat.BotDifficulty == BotDifficulty.Easy
            ? BotDifficulty.Normal
            : BotDifficulty.Easy;
        _focusAnchor = NewGameFocusAnchor.RemovePlayer;
        Refresh($"Player {slotIndex + 1} → bot {PlayerDisplayNames.FormatBotDifficulty(seat.BotDifficulty)}.");
    }

    private void AdjustGold(int delta)
    {
        if (!_viewModel.ShowSkirmishLobby)
            return;

        _viewModel.StartingGold = Math.Clamp(_viewModel.StartingGold + delta, MinGold, MaxGold);
        _view.SetStatus($"Starting gold: {_viewModel.StartingGold}.");
        _view.SyncLobbyValues(_viewModel.StartingGold, _viewModel.UnitCap);
    }

    private void AdjustUnitCap(int delta)
    {
        if (!_viewModel.ShowSkirmishLobby)
            return;

        _viewModel.UnitCap = Math.Clamp(_viewModel.UnitCap + delta, MinUnitCap, MaxUnitCap);
        _view.SetStatus($"Unit cap: {_viewModel.UnitCap}.");
        _view.SyncLobbyValues(_viewModel.StartingGold, _viewModel.UnitCap);
    }

    private void StartMatch()
    {
        if (!_viewModel.CanStart
            || _viewModel.SelectedMode is null
            || string.IsNullOrWhiteSpace(_viewModel.SelectedScenarioModuleId)
            || string.IsNullOrWhiteSpace(_viewModel.SelectedLevelId))
        {
            _view.SetStatus("Pick mode, scenario, and level before Start.");
            return;
        }

        var unlocked = ResolveUnlockedLevelIds();
        if (unlocked is not null && !unlocked.Contains(_viewModel.SelectedLevelId))
        {
            _view.SetStatus("That chapter is locked.");
            return;
        }

        ArgumentNullException.ThrowIfNull(_moduleLocator);
        ArgumentNullException.ThrowIfNull(_bundleLibrary);

        try
        {
            EnsureMinimumPlayerSeats();

            var scenarioRoot = _moduleLocator.ResolveModuleRoot(_viewModel.SelectedScenarioModuleId);
            var scenario = ScenarioModuleLoader.Load(scenarioRoot, TinyGame.Files);
            var bundles = _bundleLibrary.ListEffectiveBundles();
            var composition = ResolveComposition(scenario, bundles)
                ?? throw new InvalidOperationException("Composition is unavailable.");

            CampaignRunState? campaignRun = null;
            if (_viewModel.SelectedMode == NewGamePlayMode.Campaign
                && _campaignByScenario.TryGetValue(_viewModel.SelectedScenarioModuleId, out var campaign))
            {
                campaignRun = BeginOrResumeCampaignRun(campaign, composition);
            }

            ScreenManager.ReplaceScreen(new LoadingScreen(
                TinyGame,
                _assets,
                new NewGameStartRequest
                {
                    ScenarioModuleId = _viewModel.SelectedScenarioModuleId,
                    LevelId = _viewModel.SelectedLevelId,
                    Composition = composition,
                    PlayerCount = _playerSeats.Count,
                    StartingGold = _viewModel.StartingGold,
                    UnitCap = _viewModel.UnitCap,
                    PlayerSeats = _playerSeats.ToArray(),
                    CampaignRun = campaignRun,
                }));
        }
        catch (Exception exception)
        {
            _view.SetStatus("Cannot start: " + exception.Message);
        }
    }

    private CampaignRunState BeginOrResumeCampaignRun(
        CampaignDefinition campaign,
        MatchContentComposition composition)
    {
        var existing = _campaignProgressStore.TryLoadLatestForCampaign(
            _viewModel.SelectedScenarioModuleId!,
            campaign.CampaignId);

        CampaignProgressDocument progress;
        if (existing is null)
        {
            progress = CampaignProgressFactory.CreateNew(
                campaign,
                _viewModel.SelectedScenarioModuleId!,
                composition,
                _playerSeats.Select(MatchSaveSeatCodec.ToSave).ToList(),
                _viewModel.UnitCap);
        }
        else
        {
            progress = existing;
            // Jump cursor to the selected unlocked chapter.
            progress = new CampaignProgressDocument
            {
                SaveVersion = existing.SaveVersion,
                Kind = CampaignProgressDocument.KindCampaign,
                WrittenAtUtc = DateTimeOffset.UtcNow,
                CampaignId = existing.CampaignId,
                ScenarioModuleId = existing.ScenarioModuleId,
                CampaignTitle = existing.CampaignTitle ?? campaign.Title,
                CurrentLevelId = _viewModel.SelectedLevelId!,
                UnlockedLevelIds = existing.UnlockedLevelIds.ToList(),
                PendingNextLevelId = existing.PendingNextLevelId,
                UnitCap = _viewModel.UnitCap,
                ContentSetup = CampaignProgressFactory.ToContentSetup(composition, null),
                PlayerSeats = _playerSeats.Select(MatchSaveSeatCodec.ToSave).ToList(),
                Extensions = new Dictionary<string, string>(existing.Extensions, StringComparer.Ordinal),
            };
            if (!progress.UnlockedLevelIds.Contains(progress.CurrentLevelId, StringComparer.Ordinal))
                progress.UnlockedLevelIds.Add(progress.CurrentLevelId);
        }

        var path = _campaignProgressStore.Write(progress);
        var run = CampaignRunState.FromProgress(progress, composition, _playerSeats.ToArray());
        run.ProgressFilePath = path;

        if (existing is null)
        {
            try
            {
                var service = new CampaignProgressService(TinyGame.UserDataPaths, TinyGame.Files);
                service.NotifyCampaignStarted(run, campaign);
            }
            catch (Exception)
            {
                // Best-effort.
            }
        }

        return run;
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
