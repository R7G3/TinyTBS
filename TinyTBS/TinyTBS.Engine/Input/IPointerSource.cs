namespace TinyTBS.Engine.Input;

/// <summary>
/// Read-only pointer state for screens/logic. Device polling stays in the engine.
/// </summary>
public interface IPointerSource
{
    bool IsPrimaryDown { get; }

    /// <summary>True only on the frame the primary button became pressed.</summary>
    bool WasPrimaryPressed { get; }

    /// <summary>True only on the frame the secondary button became pressed (e.g. right mouse).</summary>
    bool WasSecondaryPressed { get; }

    /// <summary>True only on the frame the middle button became pressed.</summary>
    bool WasMiddlePressed { get; }

    /// <summary>True if primary, secondary, or middle became pressed this frame.</summary>
    bool WasAnyButtonPressed { get; }

    ScreenPoint Position { get; }
}
