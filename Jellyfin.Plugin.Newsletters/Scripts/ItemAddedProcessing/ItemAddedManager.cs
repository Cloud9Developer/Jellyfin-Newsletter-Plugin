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
        // myDataDir = config.TempDirectory + "/Newsletters";

        // archiveFile = config.MyDataDir + config.ArchiveFileName; // curlist/archive
        // currRunList = config.MyDataDir + config.CurrRunListFileName;
        // newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;

        // newslettersDir = config.NewsletterDir; // newsletterdir

        logger.Info("ItemAddedManager");
        // User user = new User();
        // InternalItemsQuery query = new InternalItemsQuery();
        // string[] mediaTypes = { "Series" };
        // // query.MediaTypes = mediaTypes;
        // query.IncludeItemTypes = new[] { BaseItemKind.Episode };
        // // query.IsSeries = true;
        // // Console.WriteLine(string.Join("\n\n", _libraryManager.GetItemList(query))); // gets list of BaseItems
        // List<BaseItem> items = _libraryManager.GetItemList(query);

        // config.LibManager = _libraryManager;

        // foreach (BaseItem item in _libraryManager.GetItemList(query))
        // {
        //     // Console.WriteLine(item.Id);
        //     Console.WriteLine("Name: " + item.Name);
        //     Console.WriteLine(item.ParentId.ToString("N"));
        //     Console.WriteLine(item.PhysicalLocations[0]);
        //     Console.WriteLine("Still works");
        //     Console.WriteLine("---------------");
        // }

        // Console.WriteLine("NLP: Search Size: " + items.Count);
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
