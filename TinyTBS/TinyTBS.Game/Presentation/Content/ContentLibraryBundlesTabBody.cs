using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Bundles tab list body (two-line rows).</summary>
internal static class ContentLibraryBundlesTabBody
{
    public const float RowPitch = 72f;

    public static ContentLibraryTabBodyResult Populate(
        ContentLibraryListBuilder list,
        IReadOnlyList<ContentBundleRowViewModel> bundles,
        Action<ContentBundleRowViewModel> onShowDetail)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(bundles);
        ArgumentNullException.ThrowIfNull(onShowDetail);

        if (bundles.Count == 0)
        {
            list.AddHint("No bundle presets.");
            return new ContentLibraryTabBodyResult { RowCount = 1, RowPitch = RowPitch };
        }

        foreach (var bundle in bundles)
        {
            var captured = bundle;
            list.AddTwoLineRow(
                captured.SummaryLine,
                captured.DetailLine,
                () => onShowDetail(captured));
        }

        return new ContentLibraryTabBodyResult
        {
            RowCount = bundles.Count,
            RowPitch = RowPitch,
        };
    }
}
