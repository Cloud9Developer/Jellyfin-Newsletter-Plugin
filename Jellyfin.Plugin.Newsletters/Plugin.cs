using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Newsletters;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private Logger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        logger = new Logger();

        void SetConfigPaths(IApplicationPaths dataPaths)
        {
            // custom code
            // IApplication Paths
            PluginConfiguration config = Plugin.Instance!.Configuration;
            config.DataPath = dataPaths.DataPath;
            config.TempDirectory = dataPaths.TempDirectory;
            config.PluginsPath = dataPaths.PluginsPath;
            config.ProgramDataPath = dataPaths.ProgramDataPath;
            config.ProgramSystemPath = dataPaths.ProgramSystemPath;
            config.SystemConfigurationFilePath = dataPaths.SystemConfigurationFilePath;
            config.LogDirectoryPath = dataPaths.LogDirectoryPath;

            // Custom Paths
            config.NewsletterDir = $"{config.TempDirectory}/Newsletters/";
        }

        SetConfigPaths(applicationPaths);
    }

    /// <inheritdoc />
    public override string Name => "Newsletters";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("60f478ab-2dd6-4ea0-af10-04d033f75979");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}
