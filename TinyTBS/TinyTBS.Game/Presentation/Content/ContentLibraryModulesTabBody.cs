using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Modules tab list body.</summary>
internal static class ContentLibraryModulesTabBody
{
    public const float RowPitch = 48f;

    public static ContentLibraryTabBodyResult Populate(
        ContentLibraryListBuilder list,
        IReadOnlyList<ContentModuleRowViewModel> modules,
        Action<ContentModuleRowViewModel> onShowDetail)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(onShowDetail);

        if (modules.Count == 0)
        {
            list.AddHint("No modules in the library.");
            return new ContentLibraryTabBodyResult { RowCount = 1, RowPitch = RowPitch };
        }

        foreach (var module in modules)
        {
            var captured = module;
            list.AddRow(captured.SummaryLine, () => onShowDetail(captured));
        }

        return new ContentLibraryTabBodyResult
        {
            RowCount = modules.Count,
            RowPitch = RowPitch,
        };
    }
}
