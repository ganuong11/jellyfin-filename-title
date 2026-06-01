using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FilenameTitlePlugin;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool OverwriteExistingTitles { get; set; } = false;
}
