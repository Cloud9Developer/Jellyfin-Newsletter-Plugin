#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.NLDataGenerator;
using Jellyfin.Plugin.Newsletters.Scripts.SCRAPER;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Newsletters.Scripts.ItemAddedProcessing;

/// <inheritdoc />
public class ItemLibrary : IServerEntryPoint
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    // private readonly string newslettersDir;
    // private readonly string newsletterDataFile;

    // private readonly string currRunList;
    // private readonly string archiveFile;
    // private readonly string myDataDir;
    private readonly ILibraryManager _libraryManager;
    // private readonly IServerApplicationHost _applicationHost;
    private Logger logger;
    // private readonly IWebhookSender _webhookSender;
    // private readonly ConcurrentDictionary<Guid, QueuedItemContainer> _itemProcessQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemLibrary"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public ItemLibrary(
        ILibraryManager libraryManager)
    {
        logger = new Logger();
        _libraryManager = libraryManager;
        // _applicationHost = applicationHost;

        config = Plugin.Instance!.Configuration;

        logger.Info("ItemAddedManager");
    }

    public Task RunAsync()
    {
        logger.Info("[NLP] RUNNING SCAN..");
        Scraper myScraper = new Scraper(_libraryManager);
        return myScraper.GetSeriesData(); // .ConfigureAwait(false);
        // return Task.CompletedTask;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources or <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool dispose)
    {
        if (dispose)
        {
            logger.Info("Disposing");
        }
    }
}
