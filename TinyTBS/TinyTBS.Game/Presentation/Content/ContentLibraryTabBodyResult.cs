namespace TinyTBS.Game.Presentation.Content;

/// <summary>Layout metrics returned after a tab fills the content list.</summary>
internal sealed class ContentLibraryTabBodyResult
{
    public required int RowCount { get; init; }

    public required float RowPitch { get; init; }
}
