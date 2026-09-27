using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>
/// Lobby gold / unit-cap ±: Confirm hold-repeat and mouse press hold-repeat
/// (Gum Click is release-only and must not activate steppers).
/// </summary>
internal sealed class NewGameLobbyStepperInput
{
    private NewGameValueStepperWidgets? _gold;
    private NewGameValueStepperWidgets? _unitCap;
    private float _confirmRepeatTimer;
    private float _pointerRepeatTimer;
    private int _pointerHoldDelta;
    private NewGameLobbyStepperKind _pointerHoldKind;

    public void Bind(NewGameLobbyTabBodyResult? lobbyResult)
    {
        Clear();
        if (lobbyResult is null)
            return;

        _gold = lobbyResult.GoldStepper;
        _unitCap = lobbyResult.UnitCapStepper;
    }

    public void Clear()
    {
        _gold = null;
        _unitCap = null;
        _confirmRepeatTimer = 0f;
        ClearPointerHold();
    }

    public void SyncValues(int startingGold, int unitCap)
    {
        if (_gold is not null)
            _gold.ValueLabel.Text = $"Gold: {startingGold}";
        if (_unitCap is not null)
            _unitCap.ValueLabel.Text = $"Unit cap: {unitCap}";
    }

    public void ClearPointerHold()
    {
        _pointerHoldKind = NewGameLobbyStepperKind.None;
        _pointerHoldDelta = 0;
        _pointerRepeatTimer = 0f;
    }

    public bool TryHandleConfirmRepeat(
        IGameCommandSource commands,
        float elapsedSeconds,
        int focusIndex,
        IReadOnlyList<(Button Button, Action Activate)> focusableEntries)
    {
        if (!TryGetFocusedStepper(focusIndex, out _, out _))
        {
            _confirmRepeatTimer = 0f;
            return false;
        }

        if (!commands.IsPressed(GameCommand.Confirm))
        {
            _confirmRepeatTimer = 0f;
            return false;
        }

        if (HeldCommandRepeat.TryTick(
                commands,
                GameCommand.Confirm,
                elapsedSeconds,
                ref _confirmRepeatTimer))
        {
            focusableEntries[focusIndex].Activate();
        }

        return true;
    }

    public void TryBeginPointerHold(
        IPointerSource pointer,
        ref int focusIndex,
        Action applyFocusIndex,
        IReadOnlyList<(Button Button, Action Activate)> focusableEntries)
    {
        if (!pointer.WasPrimaryPressed)
            return;

        if (!TryResolveStepperUnderCursor(out var kind, out var delta, out var targetFocusIndex))
            return;

        focusIndex = targetFocusIndex;
        applyFocusIndex();
        focusableEntries[targetFocusIndex].Activate();

        _pointerHoldKind = kind;
        _pointerHoldDelta = delta;
        _pointerRepeatTimer = HeldCommandRepeat.DefaultInitialDelaySeconds;
    }

    public void TickPointerHold(
        IPointerSource pointer,
        float elapsedSeconds,
        ref int focusIndex,
        Action applyFocusIndex,
        IReadOnlyList<(Button Button, Action Activate)> focusableEntries)
    {
        if (_pointerHoldKind == NewGameLobbyStepperKind.None || _pointerHoldDelta == 0)
            return;

        if (!pointer.IsPrimaryDown)
        {
            ClearPointerHold();
            return;
        }

        var targetIndex = ResolveStepperFocusIndex(_pointerHoldKind, _pointerHoldDelta);
        if (targetIndex >= 0 && focusIndex != targetIndex)
        {
            focusIndex = targetIndex;
            applyFocusIndex();
        }

        _pointerRepeatTimer -= elapsedSeconds;
        if (_pointerRepeatTimer > 0f)
            return;

        _pointerRepeatTimer = HeldCommandRepeat.DefaultIntervalSeconds;
        if (focusIndex >= 0 && focusIndex < focusableEntries.Count)
            focusableEntries[focusIndex].Activate();
    }

    private bool TryGetFocusedStepper(int focusIndex, out NewGameLobbyStepperKind kind, out int delta)
    {
        kind = NewGameLobbyStepperKind.None;
        delta = 0;

        if (_gold is not null)
        {
            if (focusIndex == _gold.DecreaseFocusIndex)
            {
                kind = NewGameLobbyStepperKind.Gold;
                delta = -1;
                return true;
            }

            if (focusIndex == _gold.IncreaseFocusIndex)
            {
                kind = NewGameLobbyStepperKind.Gold;
                delta = 1;
                return true;
            }
        }

        if (_unitCap is not null)
        {
            if (focusIndex == _unitCap.DecreaseFocusIndex)
            {
                kind = NewGameLobbyStepperKind.UnitCap;
                delta = -1;
                return true;
            }

            if (focusIndex == _unitCap.IncreaseFocusIndex)
            {
                kind = NewGameLobbyStepperKind.UnitCap;
                delta = 1;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveStepperUnderCursor(
        out NewGameLobbyStepperKind kind,
        out int delta,
        out int focusIndex)
    {
        kind = NewGameLobbyStepperKind.None;
        delta = 0;
        focusIndex = -1;

        var over = GumService.Default.Cursor.FrameworkElementOver;
        if (over is null)
            return false;

        if (_gold is not null
            && (IsPointerOverStepper(over, _gold.DecreaseButton, _gold.DecreaseFocusIndex, NewGameLobbyStepperKind.Gold, -1, out kind, out delta, out focusIndex)
                || IsPointerOverStepper(over, _gold.IncreaseButton, _gold.IncreaseFocusIndex, NewGameLobbyStepperKind.Gold, 1, out kind, out delta, out focusIndex)))
        {
            return true;
        }

        if (_unitCap is not null
            && (IsPointerOverStepper(over, _unitCap.DecreaseButton, _unitCap.DecreaseFocusIndex, NewGameLobbyStepperKind.UnitCap, -1, out kind, out delta, out focusIndex)
                || IsPointerOverStepper(over, _unitCap.IncreaseButton, _unitCap.IncreaseFocusIndex, NewGameLobbyStepperKind.UnitCap, 1, out kind, out delta, out focusIndex)))
        {
            return true;
        }

        return false;
    }

    private static bool IsPointerOverStepper(
        object over,
        Button button,
        int registeredFocusIndex,
        NewGameLobbyStepperKind kind,
        int delta,
        out NewGameLobbyStepperKind resolvedKind,
        out int resolvedDelta,
        out int focusIndex)
    {
        resolvedKind = NewGameLobbyStepperKind.None;
        resolvedDelta = 0;
        focusIndex = -1;

        if (registeredFocusIndex < 0)
            return false;

        if (!ReferenceEquals(over, button)
            && !ReferenceEquals(over, button.Visual)
            && (over is not GraphicalUiElement visual || !GumScrollListLayout.IsDescendantOf(visual, button.Visual)))
        {
            return false;
        }

        resolvedKind = kind;
        resolvedDelta = delta;
        focusIndex = registeredFocusIndex;
        return true;
    }

    private int ResolveStepperFocusIndex(NewGameLobbyStepperKind kind, int delta) =>
        kind switch
        {
            NewGameLobbyStepperKind.Gold when delta < 0 => _gold?.DecreaseFocusIndex ?? -1,
            NewGameLobbyStepperKind.Gold => _gold?.IncreaseFocusIndex ?? -1,
            NewGameLobbyStepperKind.UnitCap when delta < 0 => _unitCap?.DecreaseFocusIndex ?? -1,
            NewGameLobbyStepperKind.UnitCap => _unitCap?.IncreaseFocusIndex ?? -1,
            _ => -1,
        };
}
