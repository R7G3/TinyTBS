namespace TinyTBS.Core.Input;

/// <summary>
/// Read-only pointer state for screens/logic. Device polling stays in the engine.
/// </summary>
public interface IPointerSource
{
    bool IsPrimaryDown { get; }

    /// <summary>True only on the frame the primary button became pressed.</summary>
    bool WasPrimaryPressed { get; }

    ScreenPoint Position { get; }
}
