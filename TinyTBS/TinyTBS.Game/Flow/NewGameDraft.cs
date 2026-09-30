using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.NewGame;
using TinyTBS.Game.ViewModels;
using TinyTBS.Rules.Ai;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Flow;

/// <summary>
/// New Game wizard state: which mode, scenario, level and composition are selected, and the lobby seats.
/// The screen only routes input and paints <see cref="NewGameViewModel"/>.
/// </summary>
public sealed class NewGameDraft
{
    private const int GoldStep = 50;
    private const int UnitCapStep = 1;
    private const int MinGold = 0;
    private const int MaxGold = 9999;
    private const int MinUnitCap = 1;
    private const int MaxUnitCap = 99;

    private readonly NewGameSetupService _setup;
    private readonly CampaignFlowService _campaigns;
    private readonly List<MatchPlayerSeat> _playerSeats = [];
    private readonly Dictionary<string, IReadOnlyList<ScenarioLevelInfo>> _levelsByScenario = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CampaignDefinition> _campaignByScenario = new(StringComparer.Ordinal);

    private ContentModuleInfo[] _allScenarios = [];
    private bool _lobbyInitializedForLevel;

    public NewGameDraft(NewGameSetupService setup, CampaignFlowService campaigns)
    {
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        _campaigns = campaigns ?? throw new ArgumentNullException(nameof(campaigns));
    }

    public NewGamePlayMode? SelectedMode { get; private set; }

    public string? SelectedScenarioModuleId { get; private set; }

    public string? SelectedLevelId { get; private set; }

    public string SelectedCompositionSourceId { get; private set; } = NewGameCompositionSources.ScenarioDefaults;

    public bool ShowAddPlayerChooser { get; private set; }

    public int StartingGold { get; private set; }

    public int UnitCap { get; private set; } = MinUnitCap;

    public int PlayersMin { get; private set; } = 2;

    public int PlayersMax { get; private set; } = 2;

    public int SeatCount => _playerSeats.Count;

    public bool AllowEditPlayerSeats { get; private set; }

    public bool CanAddPlayer => AllowEditPlayerSeats && _playerSeats.Count < PlayersMax;

    public bool CanStart =>
        SelectedMode is not null
        && !string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
        && !string.IsNullOrWhiteSpace(SelectedLevelId)
        && _playerSeats.Count >= PlayersMin
        && _playerSeats.Count <= PlayersMax
        && StartingGold >= 0
        && UnitCap >= MinUnitCap;

    public bool IsSelectedLevelLocked => IsLevelLocked(SelectedLevelId);

    public void SelectMode(NewGamePlayMode mode)
    {
        if (SelectedMode != mode)
        {
            SelectedMode = mode;
            SelectedScenarioModuleId = null;
            SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }
    }

    public void SelectScenario(string moduleId)
    {
        if (SelectedScenarioModuleId != moduleId)
        {
            SelectedScenarioModuleId = moduleId;
            SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }
    }

    /// <summary>False when the chapter is still locked.</summary>
    public bool TrySelectLevel(string levelId)
    {
        if (IsLevelLocked(levelId))
            return false;

        if (SelectedLevelId != levelId)
            _lobbyInitializedForLevel = false;

        SelectedLevelId = levelId;
        return true;
    }

    public void SelectComposition(string sourceId) => SelectedCompositionSourceId = sourceId;

    public bool TryOpenAddPlayerChooser()
    {
        if (!CanAddPlayer)
            return false;

        ShowAddPlayerChooser = true;
        return true;
    }

    public void CancelAddPlayerChooser() => ShowAddPlayerChooser = false;

    public bool TryAddLocalPlayer()
    {
        if (!CanAddPlayer)
            return false;

        _playerSeats.Add(new MatchPlayerSeat { Kind = MatchPlayerKind.Local });
        ShowAddPlayerChooser = false;
        return true;
    }

    public bool TryAddBot(BotDifficulty difficulty)
    {
        if (!CanAddPlayer)
            return false;

        _playerSeats.Add(new MatchPlayerSeat
        {
            Kind = MatchPlayerKind.Bot,
            BotDifficulty = difficulty,
        });
        ShowAddPlayerChooser = false;
        return true;
    }

    /// <summary>
    /// Removes a seat. <paramref name="belowMinimum"/> is true when the lobby must ask for another player.
    /// </summary>
    public bool TryRemovePlayer(int slotIndex, out bool belowMinimum)
    {
        belowMinimum = false;
        if (!AllowEditPlayerSeats
            || _playerSeats.Count <= 1
            || slotIndex < 0
            || slotIndex >= _playerSeats.Count)
        {
            return false;
        }

        _playerSeats.RemoveAt(slotIndex);
        belowMinimum = _playerSeats.Count < PlayersMin;
        ShowAddPlayerChooser = belowMinimum;
        return true;
    }

    public bool IsBotSeat(int slotIndex) =>
        AllowEditPlayerSeats
        && slotIndex >= 0
        && slotIndex < _playerSeats.Count
        && _playerSeats[slotIndex].Kind == MatchPlayerKind.Bot;

    public bool CanPressSlot(int slotIndex) =>
        AllowEditPlayerSeats && slotIndex >= 0 && slotIndex < _playerSeats.Count;

    public BotDifficulty CycleBot(int slotIndex)
    {
        var seat = _playerSeats[slotIndex];
        seat.BotDifficulty = seat.BotDifficulty == BotDifficulty.Easy
            ? BotDifficulty.Normal
            : BotDifficulty.Easy;
        return seat.BotDifficulty;
    }

    public bool TryAdjustGold(int delta)
    {
        if (SelectedMode != NewGamePlayMode.Skirmish)
            return false;

        StartingGold = Math.Clamp(StartingGold + delta, MinGold, MaxGold);
        return true;
    }

    public bool TryAdjustUnitCap(int delta)
    {
        if (SelectedMode != NewGamePlayMode.Skirmish)
            return false;

        UnitCap = Math.Clamp(UnitCap + delta, MinUnitCap, MaxUnitCap);
        return true;
    }

    public int GoldStepAmount => GoldStep;

    public int UnitCapStepAmount => UnitCapStep;

    /// <summary>Reloads the catalog and writes lists, lobby and status into <paramref name="viewModel"/>.</summary>
    public void Project(NewGameViewModel viewModel, string statusText)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ReloadCatalog();
        PruneSelection();

        viewModel.SelectedMode = SelectedMode;
        viewModel.SelectedScenarioModuleId = SelectedScenarioModuleId;
        viewModel.SelectedLevelId = SelectedLevelId;
        viewModel.SelectedCompositionSourceId = SelectedCompositionSourceId;
        viewModel.ShowAddPlayerTypeChooser = ShowAddPlayerChooser;

        viewModel.Modes =
        [
            new NewGameModeRowViewModel
            {
                Mode = NewGamePlayMode.Campaign,
                Title = "Campaign",
                DetailLine = "story levels, Local vs Bot",
                IsSelected = SelectedMode == NewGamePlayMode.Campaign,
            },
            new NewGameModeRowViewModel
            {
                Mode = NewGamePlayMode.Skirmish,
                Title = "Skirmish",
                DetailLine = "gold, unit cap, slots",
                IsSelected = SelectedMode == NewGamePlayMode.Skirmish,
            },
        ];

        var filteredScenarios = FilterScenariosForMode(SelectedMode);
        viewModel.Scenarios = filteredScenarios
            .Select(module => new NewGameScenarioRowViewModel
            {
                ModuleId = module.ModuleId,
                Title = module.Title,
                SourceLabel = module.Source == ContentModuleSource.UserLibrary ? "user" : "bundled",
                IsSelected = module.ModuleId == SelectedScenarioModuleId,
            })
            .ToArray();

        var filteredLevels = FilterLevelsForSelection();
        var unlocked = ResolveUnlockedLevelIds();
        viewModel.Levels = filteredLevels
            .Select(level => new NewGameLevelRowViewModel
            {
                LevelId = level.LevelId,
                Title = level.Title,
                ModesLabel = FormatModesLabel(level.Modes),
                IsSelected = level.LevelId == SelectedLevelId,
                IsLocked = SelectedMode == NewGamePlayMode.Campaign
                    && unlocked is not null
                    && !unlocked.Contains(level.LevelId),
            })
            .ToArray();

        var selectedLevel = filteredLevels.FirstOrDefault(level => level.LevelId == SelectedLevelId);
        ApplyLobby(selectedLevel, viewModel);

        ScenarioModuleDefinition? scenarioDefinition = null;
        if (!string.IsNullOrWhiteSpace(SelectedScenarioModuleId))
        {
            try
            {
                scenarioDefinition = _setup.LoadScenario(SelectedScenarioModuleId);
            }
            catch (Exception exception)
            {
                statusText = "Scenario load issue: " + exception.Message;
            }
        }

        var bundles = _setup.ListBundles();
        EnsureCompositionSource(bundles);
        viewModel.SelectedCompositionSourceId = SelectedCompositionSourceId;
        viewModel.CompositionOptions = BuildCompositionOptions(bundles);
        var compositionSummary = BuildCompositionSummary(scenarioDefinition, bundles);
        if (compositionSummary.StartsWith("Composition error:", StringComparison.Ordinal))
            statusText = compositionSummary;

        viewModel.CompositionSummary = compositionSummary;
        viewModel.TabEmptyHint = HintFor(viewModel.ActiveTab);
        viewModel.StatusText = statusText;
    }

    public MatchStartRequest CreateStartRequest()
    {
        if (!CanStart
            || SelectedMode is null
            || string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
            || string.IsNullOrWhiteSpace(SelectedLevelId))
        {
            throw new InvalidOperationException("Pick mode, scenario, and level before Start.");
        }

        if (IsSelectedLevelLocked)
            throw new InvalidOperationException("That chapter is locked.");

        EnsureMinimumPlayerSeats();

        var scenario = _setup.LoadScenario(SelectedScenarioModuleId);
        var bundles = _setup.ListBundles();
        var composition = ResolveComposition(scenario, bundles)
            ?? throw new InvalidOperationException("Composition is unavailable.");

        CampaignRunState? campaignRun = null;
        if (SelectedMode == NewGamePlayMode.Campaign
            && _campaignByScenario.TryGetValue(SelectedScenarioModuleId, out var campaign))
        {
            campaignRun = _campaigns.BeginOrResumeRun(
                campaign,
                SelectedScenarioModuleId,
                SelectedLevelId,
                composition,
                _playerSeats,
                UnitCap);
        }

        return new MatchStartRequest
        {
            ScenarioModuleId = SelectedScenarioModuleId,
            LevelId = SelectedLevelId,
            Composition = composition,
            PlayerCount = _playerSeats.Count,
            StartingGold = StartingGold,
            UnitCap = UnitCap,
            PlayerSeats = _playerSeats.ToArray(),
            CampaignRun = campaignRun,
        };
    }

    private void ReloadCatalog()
    {
        var entries = _setup.ListScenarios();
        _allScenarios = entries.Select(entry => entry.Module).ToArray();
        _levelsByScenario.Clear();
        _campaignByScenario.Clear();
        foreach (var entry in entries)
        {
            _levelsByScenario[entry.Module.ModuleId] = entry.Levels;
            if (entry.Campaign is not null)
                _campaignByScenario[entry.Module.ModuleId] = entry.Campaign;
        }
    }

    private void PruneSelection()
    {
        if (SelectedMode is null)
        {
            SelectedScenarioModuleId = null;
            SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
            return;
        }

        var filteredScenarios = FilterScenariosForMode(SelectedMode);
        if (string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
            || filteredScenarios.All(scenario => scenario.ModuleId != SelectedScenarioModuleId))
        {
            SelectedScenarioModuleId = filteredScenarios.FirstOrDefault()?.ModuleId;
            SelectedLevelId = null;
            _lobbyInitializedForLevel = false;
        }

        var filteredLevels = FilterLevelsForSelection();
        if (string.IsNullOrWhiteSpace(SelectedLevelId)
            || filteredLevels.All(level => level.LevelId != SelectedLevelId)
            || IsSelectedLevelLocked)
        {
            SelectedLevelId = SelectedMode == NewGamePlayMode.Campaign
                ? PreferCampaignLevelId(filteredLevels)
                : PreferDefaultLevelId(filteredLevels);
            _lobbyInitializedForLevel = false;
        }
    }

    private bool IsLevelLocked(string? levelId)
    {
        if (SelectedMode != NewGamePlayMode.Campaign || string.IsNullOrWhiteSpace(levelId))
            return false;

        var unlocked = ResolveUnlockedLevelIds();
        return unlocked is not null && !unlocked.Contains(levelId);
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
        if (SelectedMode is null
            || string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
            || !_levelsByScenario.TryGetValue(SelectedScenarioModuleId, out var levels))
        {
            return [];
        }

        var modeFiltered = levels
            .Where(level => LevelMatchesMode(level, SelectedMode.Value))
            .ToArray();

        if (SelectedMode != NewGamePlayMode.Campaign
            || !_campaignByScenario.TryGetValue(SelectedScenarioModuleId, out var campaign))
        {
            return modeFiltered;
        }

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
        if (SelectedMode != NewGamePlayMode.Campaign
            || string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
            || !_campaignByScenario.TryGetValue(SelectedScenarioModuleId, out var campaign))
        {
            return null;
        }

        return _campaigns.UnlockedChapterIds(SelectedScenarioModuleId, campaign);
    }

    private static string? PreferDefaultLevelId(IReadOnlyList<ScenarioLevelInfo> levels)
    {
        if (levels.Count == 0)
            return null;

        return levels.FirstOrDefault(level => level.LevelId == VanillaContentIds.DefaultSkirmishLevelId)
                   ?.LevelId
               ?? levels[0].LevelId;
    }

    private string? PreferCampaignLevelId(IReadOnlyList<ScenarioLevelInfo> levels)
    {
        if (levels.Count == 0
            || string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
            || !_campaignByScenario.TryGetValue(SelectedScenarioModuleId, out var campaign))
        {
            return PreferDefaultLevelId(levels);
        }

        var preferred = _campaigns.PreferPlayableLevelId(SelectedScenarioModuleId, campaign);
        var unlocked = _campaigns.UnlockedChapterIds(SelectedScenarioModuleId, campaign);

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

    private string HintFor(NewGameTab tab) =>
        tab switch
        {
            NewGameTab.Scenario when SelectedMode is null =>
                "Pick Campaign or Skirmish on the Mode tab first.",
            NewGameTab.Scenario =>
                "No scenario modules have levels for this mode.",
            NewGameTab.Level when SelectedMode is null =>
                "Pick Campaign or Skirmish on the Mode tab first.",
            NewGameTab.Level when string.IsNullOrWhiteSpace(SelectedScenarioModuleId) =>
                "Pick a scenario on the Scenario tab first.",
            NewGameTab.Level =>
                "No levels for this scenario in the selected mode.",
            NewGameTab.Composition when string.IsNullOrWhiteSpace(SelectedScenarioModuleId) =>
                "Pick a scenario first to choose composition.",
            NewGameTab.Lobby when string.IsNullOrWhiteSpace(SelectedLevelId) =>
                "Pick a level first to configure the lobby.",
            _ => string.Empty,
        };

    private void ApplyLobby(ScenarioLevelInfo? level, NewGameViewModel viewModel)
    {
        var showSkirmish = SelectedMode == NewGamePlayMode.Skirmish;
        viewModel.ShowSkirmishLobby = showSkirmish;
        AllowEditPlayerSeats = level is not null;
        viewModel.AllowEditPlayerSeats = AllowEditPlayerSeats;

        if (level is null)
        {
            viewModel.LobbyNote = "Select a level to configure the match.";
            PlayersMin = 2;
            PlayersMax = 2;
            StartingGold = 0;
            UnitCap = MinUnitCap;
            _playerSeats.Clear();
            ShowAddPlayerChooser = false;
            AllowEditPlayerSeats = false;
            viewModel.AllowEditPlayerSeats = false;
            _lobbyInitializedForLevel = false;
            WriteLobbyNumbers(viewModel);
            return;
        }

        PlayersMin = level.PlayersMin;
        PlayersMax = level.PlayersMax;

        if (!_lobbyInitializedForLevel)
        {
            StartingGold = level.DefaultStartingGold;
            UnitCap = level.DefaultUnitCap;
            _playerSeats.Clear();
            var slotCount = Math.Clamp(level.PlayersDefaultSlots, level.PlayersMin, level.PlayersMax);
            for (var index = 0; index < slotCount; index++)
                _playerSeats.Add(CreateDefaultSeat(index));
            _lobbyInitializedForLevel = true;
            ShowAddPlayerChooser = false;
        }

        while (_playerSeats.Count > PlayersMax)
            _playerSeats.RemoveAt(_playerSeats.Count - 1);

        if (_playerSeats.Count >= PlayersMax)
            ShowAddPlayerChooser = false;

        viewModel.LobbyNote = showSkirmish
            ? "Skirmish: Local and Bot (Easy/Normal). Confirm a Bot seat to cycle difficulty. Gold and unit cap below."
            : "Campaign: Player 2 defaults to Bot · Easy. Confirm a Bot seat to cycle Easy/Normal. Gold and unit cap come from the level.";

        if (!showSkirmish)
        {
            StartingGold = level.DefaultStartingGold;
            UnitCap = level.DefaultUnitCap;
        }

        WriteLobbyNumbers(viewModel);
    }

    private void WriteLobbyNumbers(NewGameViewModel viewModel)
    {
        viewModel.PlayersMin = PlayersMin;
        viewModel.PlayersMax = PlayersMax;
        viewModel.StartingGold = StartingGold;
        viewModel.UnitCap = UnitCap;
        viewModel.ShowAddPlayerTypeChooser = ShowAddPlayerChooser;
        viewModel.PlayerSlots = _playerSeats
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
        if (SelectedMode == NewGamePlayMode.Campaign && slotIndex == 1)
        {
            return new MatchPlayerSeat
            {
                Kind = MatchPlayerKind.Bot,
                BotDifficulty = BotDifficulty.Easy,
            };
        }

        return new MatchPlayerSeat { Kind = MatchPlayerKind.Local };
    }

    private void EnsureMinimumPlayerSeats()
    {
        while (_playerSeats.Count < PlayersMin)
            _playerSeats.Add(new MatchPlayerSeat { Kind = MatchPlayerKind.Local });
    }

    private void EnsureCompositionSource(IReadOnlyList<ContentBundleDefinition> bundles)
    {
        if (SelectedCompositionSourceId == NewGameCompositionSources.ScenarioDefaults)
            return;

        if (bundles.Any(bundle => bundle.BundleId == SelectedCompositionSourceId))
            return;

        SelectedCompositionSourceId = NewGameCompositionSources.ScenarioDefaults;
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
                IsSelected = SelectedCompositionSourceId == NewGameCompositionSources.ScenarioDefaults,
            },
        };

        foreach (var bundle in bundles)
        {
            options.Add(new NewGameCompositionOptionViewModel
            {
                SourceId = bundle.BundleId,
                Title = $"Bundle: {bundle.Title}",
                IsSelected = bundle.BundleId == SelectedCompositionSourceId,
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

        return NewGameSetupService.ResolveComposition(scenario, SelectedCompositionSourceId, bundles);
    }

    private static string FormatModesLabel(IReadOnlyList<string> modes)
    {
        if (modes.Count == 0)
            return string.Empty;

        return string.Join("/", modes);
    }
}
