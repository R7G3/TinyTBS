namespace TinyTBS.Game.Editor.Themes;

/// <summary>One sprite remap row in theme <c>module.json</c>.</summary>
public sealed class EditableThemeRemapEntry
{
    public string ContentIdFull { get; set; } = string.Empty;

    public string BasePath { get; set; } = string.Empty;

    public string MaskPath { get; set; } = string.Empty;
}
