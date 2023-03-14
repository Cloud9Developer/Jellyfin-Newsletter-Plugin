#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.NLDataGenerator;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Newsletters.Scripts.SCRAPER;

public class Scraper
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    // private readonly string archiveList;
    // private readonly string currRunScanListDir;
    private readonly string currRunScanList;
    private readonly string archiveFile;
    private readonly string currNewsletterDataFile;
    private readonly ILibraryManager libManager;

    // Non-readonly
    // private int EpisodeCount = 0;
    private static string append = "Append";
    private static string write = "Overwrite";
    private NewsletterDataGenerator ng;
    private Logger logger;

    // private List<string> fileList;

    public Scraper(ILibraryManager libraryManager)
    {
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        libManager = libraryManager;
        // currRunScanListDir = config.TempDirectory + "/Newsletters/";
        archiveFile = config.MyDataDir + config.ArchiveFileName;
        currRunScanList = config.MyDataDir + config.CurrRunListFileName;
        currNewsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
        Directory.CreateDirectory(config.MyDataDir);

        // empty currrun database if it exists
        WriteFile(write, currRunScanList, string.Empty); // overwrite currscan data on run (testing purposes)
        ng = new NewsletterDataGenerator();

        logger.Info("Setting Config Paths: ");
        logger.Info("\n  DataPath: " + config.DataPath +
                    "\n  TempDirectory: " + config.TempDirectory +
                    "\n  PluginsPath: " + config.PluginsPath +
                    "\n  MyDataDir: " + config.MyDataDir +
                    "\n  NewsletterDir: " + config.NewsletterDir +
                    "\n  ProgramDataPath: " + config.ProgramDataPath +
                    "\n  ProgramSystemPath: " + config.ProgramSystemPath +
                    "\n  SystemConfigurationFilePath: " + config.SystemConfigurationFilePath);
    }

    public Task GetSeriesData()
    {
        // string filePath = config.MediaDir; // Prerolls works. Movies not due to spaces)

        // logger.Info("Scanned " + fileCount + " files" );
        List<JsonFileObj> archiveObj = ng.PopulateFromArchive();

        InternalItemsQuery query = new InternalItemsQuery();
        string[] mediaTypes = { "Series" };
        query.IncludeItemTypes = new[] { BaseItemKind.Episode };
        List<BaseItem> items = libManager.GetItemList(query);

        foreach (BaseItem item in libManager.GetItemList(query))
        {
            logger.Debug("Series: " + item.GetParent().GetParent().Name); // Title
            logger.Debug("Season: " + item.GetParent().Name); // Season
            logger.Debug("Episode Name: " + item.Name); // episode Name
            logger.Debug("Episode Number: " + item.IndexNumber); // episode Name
            logger.Debug("Series Overview: " + item.GetParent().GetParent().Overview); // series overview
            logger.Debug(item.ParentId.ToString("N")); // series ItemId
            logger.Debug(item.PhysicalLocations[0]); // Filepath
            logger.Debug("---------------");

            if (item is null)
            {
                continue;
            }

            JsonFileObj currFileObj = new JsonFileObj();
            currFileObj.Filename = item.PhysicalLocations[0];
            currFileObj.Title = item.GetParent().GetParent().Name;
            if (!AlreadyInArchive(archiveObj, currFileObj) && !AlreadyInCurrNewsletterData(archiveObj, currFileObj))
            {
                try
                {
                    currFileObj.Filename = item.PhysicalLocations[0];
                    currFileObj.Title = item.GetParent().GetParent().Name;
                    if (item.IndexNumber is int && item.IndexNumber is not null)
                    {
                        currFileObj.Episode = (int)item.IndexNumber;
                    }

                    currFileObj.SeriesOverview = item.GetParent().GetParent().Overview;
                    currFileObj.ItemID = item.ParentId.ToString("N");
                    currFileObj.ImageURL = "testing"; // SetImageURL(currFileObj);
                    currFileObj.Season = int.Parse(item.GetParent().Name.Split(' ')[1], CultureInfo.CurrentCulture);
                }
                catch (Exception e)
                {
                    logger.Error("Encountered an error parsing: " + currFileObj.Filename);
                    logger.Error(e);
                }
                finally
                {
                    // save to "database"
                    WriteFile(append, currRunScanList, JsonConvert.SerializeObject(currFileObj) + ";;;");
                }
            }
            else
            {
                logger.Debug("\"" + currFileObj.Filename + "\" has already been processed either by Previous or Current Newsletter!");
            }
        }

        logger.Info("NLP: Search Size: " + items.Count);

        logger.Info("Gathering Data...");

        // NewsletterDataGenerator nlDG = new NewsletterDataGenerator(progress, fileCount);
        // return ng.GenerateDataForNextNewsletter();
        return Task.CompletedTask;
    }

    private string SetImageURL(JsonFileObj currObj)
    {
        // check if URL for series already exists in CurrNewsletterData from Previous scan
        if (File.Exists(currNewsletterDataFile))
        {
            foreach (string item in File.ReadAllText(currNewsletterDataFile).Split(";;;"))
            {
                JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Previous Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // check if URL for series already exists in Archive from Previous scan
        if (File.Exists(archiveFile))
        {
            foreach (string item in File.ReadAllText(archiveFile).Split(";;;"))
            {
                JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Previous Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // check if URL for series already exists in CurrList
        foreach (string item in File.ReadAllText(currRunScanList).Split(";;;"))
        {
            JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
            if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
            {
                logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                return fileObj.ImageURL;
            }
        }

        // string url = ng.FetchImagePoster(obj.Title);
        logger.Debug("Fetching URL from google...");
        return ng.FetchImagePoster(currObj.Title);
        // return string.Empty;
        // return "https://m.media-amazon.com/images/W/IMAGERENDERING_521856-T1/images/I/91eNqTeYvzL.jpg";
    }

    private bool AlreadyInCurrNewsletterData(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    {
        List<JsonFileObj> currNLObj = new List<JsonFileObj>();
        if (File.Exists(currNewsletterDataFile))
        {
            StreamReader sr = new StreamReader(currNewsletterDataFile);
            string nlFile = sr.ReadToEnd();
            foreach (string ep in nlFile.Split(";;;"))
            {
                JsonFileObj? currObj = JsonConvert.DeserializeObject<JsonFileObj?>(ep);
                if (currObj is not null)
                {
                    currNLObj.Add(currObj);
                }
            }

            sr.Close();
        }

        foreach (JsonFileObj element in currNLObj)
        {
            if (element.Filename == currFileObj.Filename)
            {
                return true;
            }
        }

        return false;
    }

    private bool AlreadyInArchive(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    {
        foreach (JsonFileObj element in archiveObj)
        {
            if (element.Filename == currFileObj.Filename)
            {
                return true;
            }
        }

        return false;
    }

    private void WriteFile(string method, string path, string value)
    {
        if (method == append)
        {
            File.AppendAllText(path, value);
        }
        else if (method == write)
        {
            File.WriteAllText(path, value);
        }
    }
}

public class JsonFileObj
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileObj"/> class.
    /// </summary>
    public JsonFileObj()
    {
        Filename = string.Empty;
        Title = string.Empty;
        // Season = string.Empty;
        Season = 0;
        Episode = 0;
        // Episode = string.Empty;
        SeriesOverview = string.Empty;
        ImageURL = string.Empty;
        ItemID = string.Empty;
    }

    public string Filename { get; set; }

    public string Title { get; set; }

    // public string Season { get; set; }

    public int Season { get; set; }

    public int Episode { get; set; }

    // public string Episode { get; set; }

    public string SeriesOverview { get; set; }

    public string ImageURL { get; set; }

    public string ItemID { get; set; }
}