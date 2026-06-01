using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FilenameTitlePlugin;

public class TitleUpdaterTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly FilenameCleanerService _cleaner;
    private readonly ILogger<TitleUpdaterTask> _logger;
    private readonly Plugin _plugin;

    public TitleUpdaterTask(
        ILibraryManager libraryManager,
        FilenameCleanerService cleaner,
        ILogger<TitleUpdaterTask> logger,
        Plugin plugin)
    {
        _libraryManager = libraryManager;
        _cleaner = cleaner;
        _logger = logger;
        _plugin = plugin;
    }

    public string Name => "Update Titles from Filenames";
    public string Key => "FilenameTitleUpdater";
    public string Description => "Updates media item titles to cleaned versions of their source filenames.";
    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IsFolder = false,
            Recursive = true
        });

        var total = items.Count;
        if (total == 0)
        {
            progress.Report(100);
            return;
        }

        for (int i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = items[i];

            try
            {
                if (!string.IsNullOrEmpty(item.Path))
                {
                    var rawName = Path.GetFileNameWithoutExtension(item.Path);

                    // Safety rule: only update items whose title is still the raw filename
                    // Unless OverwriteExistingTitles is enabled
                    var shouldUpdate = _plugin.Configuration.OverwriteExistingTitles
                        || string.Equals(item.Name, rawName, StringComparison.OrdinalIgnoreCase);

                    if (shouldUpdate)
                    {
                        var cleanTitle = _cleaner.Clean(item.Path);

                        if (!string.IsNullOrEmpty(cleanTitle))
                        {
                            _logger.LogInformation(
                                "[FilenameTitlePlugin] \"{OldTitle}\" → \"{NewTitle}\" ({File})",
                                item.Name,
                                cleanTitle,
                                Path.GetFileName(item.Path));

                            item.Name = cleanTitle;
                            await _libraryManager
                                .UpdateItemAsync(item, item.GetParent(), ItemUpdateType.MetadataEdit, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FilenameTitlePlugin] Failed to update title for item {ItemId}", item.Id);
            }

            progress.Report((double)(i + 1) / total * 100);
        }
    }
}
