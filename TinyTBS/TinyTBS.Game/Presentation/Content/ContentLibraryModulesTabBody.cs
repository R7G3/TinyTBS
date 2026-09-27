using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Modules tab list body.</summary>
internal static class ContentLibraryModulesTabBody
{
    public static void Populate(
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
            return;
        }

        foreach (var module in modules)
        {
            var captured = module;
            list.AddRow(captured.SummaryLine, () => onShowDetail(captured));
        }
    }
}
