using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Install tab list body: from device / catalog / queued Downloads.</summary>
internal static class ContentLibraryInstallTabBody
{
    public static void Populate(
        ContentLibraryListBuilder list,
        ContentLibraryViewModel viewModel,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onPickInstallFromDevice);
        ArgumentNullException.ThrowIfNull(onInstallArchive);

        list.AddRow("From device…", onPickInstallFromDevice);
        list.AddRow(
            "From catalog… (soon)",
            onActivate: () => { },
            isEnabled: viewModel.CanInstallFromCatalog);

        var pending = viewModel.PendingInstalls;
        if (pending.Count > 0)
        {
            list.AddHint("Queued in app Downloads (confirm to install):");
            foreach (var item in pending)
            {
                var path = item.FullPath;
                list.AddRow(item.FileName, () => onInstallArchive(path));
            }

            return;
        }

        list.AddHint(
            "Pick a .tinymod.zip from your device, or use the catalog when it is available.");
    }
}
