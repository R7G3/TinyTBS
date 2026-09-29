using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Undo;
using TinyTBS.Game.Editor.Validation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Paint map with left content / right tools panels; LB/RB cycles focus zones.</summary>
public sealed class EditorMapPaintScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly EditableMapDocument _document;
    private readonly bool _isNewMap;
    private readonly EditorPaintToolState _tool = new();
    private readonly EditorMapPaintHudView _hud = new();
    private readonly EditorMapHistory _history = new();

    private MatchTextureAtlas? _textures;
    private MatchContentCatalog? _catalog;
    private EditorMapBoard? _board;
    private MapDocumentWriter? _mapWriter;
    private LevelStubWriter? _levelWriter;
    private string _statusHint = string.Empty;
    private EditorValidationIndicator _validationIndicator = EditorValidationIndicator.Idle;

    public EditorMapPaintScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        EditableMapDocument document,
        bool isNewMap)
        : base(game)
    {
        _assets = assets;
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNewMap = isNewMap;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        try
        {
            var locator = new ContentModuleLocator(TinyGame.Files, TinyGame.UserDataPaths);
            var scenario = ScenarioModuleLoader.Load(_session.ModuleRootPath, TinyGame.Files);
            var composition = MatchContentComposition.FromScenarioDefaults(scenario);
            composition = new MatchContentComposition
            {
                ScenarioModuleId = _session.ModuleId,
                UnitsModuleIds = composition.UnitsModuleIds,
                BuildingsModuleIds = composition.BuildingsModuleIds,
                ThemeModuleId = composition.ThemeModuleId,
                Replaces = composition.Replaces,
            };

            var loaded = MatchContentCompositionLoader.Load(composition, locator, TinyGame.Files);
            _catalog = loaded.Catalog;
            _textures = MatchTextureAtlas.Load(GraphicsDevice, Content, _assets, loaded.Catalog);
            _board = new EditorMapBoard(_document, GraphicsDevice, TinyGame.SharedSpriteBatch, _textures);
            _mapWriter = new MapDocumentWriter(TinyGame.Files);
            _levelWriter = new LevelStubWriter(TinyGame.Files);
            RebuildHud("LB/RB: Content ↔ Map ↔ Tools. Confirm paints on map.");
        }
        catch (Exception exception)
        {
            _statusHint = "Load failed: " + exception.Message;
            RebuildHud(_statusHint);
        }
    }

    public override void UnloadContent()
    {
        _hud.Clear();
        _board?.Dispose();
        _board = null;
        _textures?.Dispose();
        _textures = null;
        _catalog = null;
        _history.Clear();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (TinyGame.Commands.WasPressed(GameCommand.FocusNextRegion))
            _hud.CycleFocusZone(+1);
        if (TinyGame.Commands.WasPressed(GameCommand.FocusPreviousRegion))
            _hud.CycleFocusZone(-1);

        _hud.HandlePanelInput(TinyGame.Commands, elapsed);

        if (_board is null)
        {
            if (TinyGame.Commands.WasPressed(GameCommand.Back)
                || TinyGame.Commands.WasPressed(GameCommand.Cancel))
            {
                GoToHub();
            }

            return;
        }

        if (TinyGame.Commands.WasPressed(GameCommand.Undo))
            Undo();
        if (TinyGame.Commands.WasPressed(GameCommand.Redo))
            Redo();

        var pointerOverUi = _hud.IsPointerOverUi;
        var pointer = TinyGame.Pointer;

        if (pointer.WasPrimaryPressed && !pointerOverUi)
            MatchCommandApplicator.ArmPrimaryPointerGesture(pointer);

        var cameraEnabled = _hud.IsMapFocused || MatchCommandApplicator.IsPrimaryGestureActive;
        MatchCommandApplicator.ApplyZoom(
            _board.Layout,
            TinyGame.Commands,
            pointer,
            gameTime,
            cameraControlsEnabled: cameraEnabled);
        MatchCommandApplicator.ApplyCameraPan(
            _board.Layout,
            TinyGame.Commands,
            pointer,
            gameTime,
            cameraControlsEnabled: cameraEnabled);

        if (MatchCommandApplicator.TryConsumePrimaryClick(pointer) && !pointerOverUi)
        {
            if (_board.Layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            {
                _hud.SetFocusZone(EditorPaintFocusZone.Map);
                _board.SetCursor(cellX, cellY);
                PaintAtCursor();
            }
        }

        if (_hud.IsMapFocused)
        {
            if (TinyGame.Commands.WasPressed(GameCommand.NavigateUp))
                _board.MoveCursor(0, -1);
            if (TinyGame.Commands.WasPressed(GameCommand.NavigateDown))
                _board.MoveCursor(0, 1);
            if (TinyGame.Commands.WasPressed(GameCommand.NavigateLeft))
                _board.MoveCursor(-1, 0);
            if (TinyGame.Commands.WasPressed(GameCommand.NavigateRight))
                _board.MoveCursor(1, 0);

            if (TinyGame.Commands.WasPressed(GameCommand.Confirm))
                PaintAtCursor();
        }

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel))
        {
            GoToHub();
            return;
        }

        _board.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, gameTime);
        SyncHudStatus();
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));
        _board?.Draw(gameTime, mapFocused: _hud.IsMapFocused);
        GumService.Default.Draw();
    }

    private void PaintAtCursor()
    {
        if (_board is null)
            return;

        _history.RecordBeforeChange(_document);
        if (!_tool.Apply(_document, _board.CursorX, _board.CursorY))
        {
            _history.DiscardLastRecord();
            return;
        }

        _board.NotifyDocumentChanged();
        SyncHudStatus();
    }

    private void Undo()
    {
        if (!_history.Undo(_document))
        {
            _statusHint = "Nothing to undo.";
            SyncHudStatus();
            return;
        }

        _board?.NotifyDocumentChanged();
        _statusHint = "Undo.";
        SyncHudStatus();
    }

    private void Redo()
    {
        if (!_history.Redo(_document))
        {
            _statusHint = "Nothing to redo.";
            SyncHudStatus();
            return;
        }

        _board?.NotifyDocumentChanged();
        _statusHint = "Redo.";
        SyncHudStatus();
    }

    private void RunValidate(bool fromSave)
    {
        if (_catalog is null)
            return;

        var result = EditorMapValidator.Validate(_document, _catalog);
        _validationIndicator = result.Indicator;
        _statusHint = fromSave
            ? result.SummaryLine + " | Saving…"
            : result.SummaryLine;
        SyncHudStatus();
    }

    private void Save()
    {
        if (_mapWriter is null || _levelWriter is null)
            return;

        RunValidate(fromSave: true);

        try
        {
            _mapWriter.Write(_session.ModuleRootPath, _document, MapScriptTemplates.EmptyHooks);
            if (_isNewMap)
            {
                var playerSlots = EditableMapDocument.InferPlayerSlotCount(_document);
                _levelWriter.WriteSkirmishStub(
                    _session.ModuleRootPath,
                    levelId: _document.Id,
                    title: _document.Title,
                    mapId: _document.Id,
                    playersMin: playerSlots,
                    playersMax: playerSlots);
            }

            var playersHint = _isNewMap
                ? " + level stub (" + EditableMapDocument.InferPlayerSlotCount(_document) + "p)."
                : ".";
            _statusHint = "Saved Maps/" + _document.Id + playersHint
                + " (" + _validationIndicator + ")";
            SyncHudStatus();
        }
        catch (Exception exception)
        {
            _statusHint = "Save failed: " + exception.Message;
            SyncHudStatus();
        }
    }

    private void RebuildHud(string hint)
    {
        _statusHint = hint;
        if (_textures is null || _catalog is null)
        {
            _hud.Clear();
            return;
        }

        _hud.Build(
            BuildStatusLine(),
            _tool,
            _textures,
            _catalog,
            onSave: Save,
            onBack: GoToHub,
            onUndo: Undo,
            onRedo: Redo,
            onValidate: () => RunValidate(fromSave: false),
            onToolChanged: SyncHudStatus);
        SyncHudStatus();
    }

    private void SyncHudStatus() =>
        _hud.SyncStatus(BuildStatusLine(), _validationIndicator);

    private string BuildStatusLine()
    {
        var dirty = _document.IsDirty ? " *" : string.Empty;
        var zone = _hud.FocusZone switch
        {
            EditorPaintFocusZone.Content => "Focus: Content",
            EditorPaintFocusZone.Tools => "Focus: Tools",
            _ => "Focus: Map",
        };
        var hint = string.IsNullOrWhiteSpace(_statusHint) ? string.Empty : " | " + _statusHint;
        return $"{_session.ModuleId}/{_document.Id}{dirty} | {zone} | {_tool.StatusLabel}{hint}";
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
