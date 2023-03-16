#pragma warning disable 1591, SA1005 // remove SA1005 to clean code
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scanner.NLDataGenerator;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
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
    private readonly string currRunScanList;
    private readonly string archiveFile;
    private readonly string currNewsletterDataFile;
    private readonly ILibraryManager libManager;

    // Non-readonly
    private int totalLibCount;
    private int currCount;
    private NewsletterDataGenerator ng;
    private SQLiteDatabase db;
    private JsonFileObj jsonHelper;
    private Logger logger;
    private IProgress<double> progress;
    private CancellationToken cancelToken;
    private List<JsonFileObj>? archiveObj;

    public Scraper(ILibraryManager libraryManager, IProgress<double> passedProgress, CancellationToken cancellationToken)
    {
        logger = new Logger();
        jsonHelper = new JsonFileObj();
        progress = passedProgress;
        cancelToken = cancellationToken;
        config = Plugin.Instance!.Configuration;
        libManager = libraryManager;

        totalLibCount = currCount = 0;
        // currRunScanListDir = config.TempDirectory + "/Newsletters/";
        archiveFile = config.MyDataDir + config.ArchiveFileName;
        currRunScanList = config.MyDataDir + config.CurrRunListFileName;
        currNewsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
        Directory.CreateDirectory(config.MyDataDir);

        ng = new NewsletterDataGenerator();
        db = new SQLiteDatabase();

        logger.Debug("Setting Config Paths: ");
        logger.Debug("\n  DataPath: " + config.DataPath +
                    "\n  TempDirectory: " + config.TempDirectory +
                    "\n  PluginsPath: " + config.PluginsPath +
                    "\n  MyDataDir: " + config.MyDataDir +
                    "\n  NewsletterDir: " + config.NewsletterDir +
                    "\n  ProgramDataPath: " + config.ProgramDataPath +
                    "\n  ProgramSystemPath: " + config.ProgramSystemPath +
                    "\n  SystemConfigurationFilePath: " + config.SystemConfigurationFilePath +
                    "\n  LogDirectoryPath: " + config.LogDirectoryPath );
    }

    // This is the main function
    public Task GetSeriesData()
    {
        logger.Info("Gathering Data...");
        try
        {
            db.CreateConnection();
            archiveObj = ng.PopulateFromArchive(db);
            BuildJsonObjsToCurrScanfile();
            CopyCurrRunDataToNewsletterData();
        }
        catch (Exception e)
        {
            logger.Error("An error has occured: " + e);
        }
        finally
        {
            db.CloseConnection();
        }

        return Task.CompletedTask;
    }

    private void BuildJsonObjsToCurrScanfile()
    {
        // List<JsonFileObj> archiveObj = ng.PopulateFromArchive();

        InternalItemsQuery query = new InternalItemsQuery();
        string[] mediaTypes = { "Series" };
        query.IncludeItemTypes = new[] { BaseItemKind.Episode };
        List<BaseItem> items = libManager.GetItemList(query);
        BaseItem episode, season, series;
        logger.Info($"Scan Size: {items.Count}");

        foreach (BaseItem? item in items)
        {
            episode = item;
            season = item.GetParent();
            series = item.GetParent().GetParent();

            logger.Debug($"Series: {series.Name}"); // Title
            logger.Debug($"Season: {season.Name}"); // Season
            logger.Debug($"Episode Name: {episode.Name}"); // episode Name
            logger.Debug($"Episode Number: {episode.IndexNumber}"); // episode Name
            logger.Debug($"Series Overview: {series.Overview}"); // series overview
            logger.Debug($"ImageInfos: {series.PrimaryImagePath}");
            logger.Debug(series.Id.ToString("N")); // series ItemId
            logger.Debug(episode.PhysicalLocations[0]); // Filepath
            logger.Debug("---------------");

            if (item is not null)
            {
                JsonFileObj currFileObj = new JsonFileObj();
                currFileObj.Filename = episode.PhysicalLocations[0];
                currFileObj.Title = series.Name;
                if (!InDatabase("CurrRunData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)) && !InDatabase("CurrNewsletterData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)) && !InDatabase("ArchiveData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)))
                {
                    try
                    {
                        if (episode.IndexNumber is int && episode.IndexNumber is not null)
                        {
                            currFileObj.Episode = (int)episode.IndexNumber;
                        }

                        currFileObj.SeriesOverview = series.Overview;
                        currFileObj.ItemID = series.Id.ToString("N");
                        currFileObj.PosterPath = series.PrimaryImagePath;
                        string url = SetImageURL(currFileObj);

                        if ((url == "429") || (url == "ERR"))
                        {
                            logger.Debug("URL is not attainable at this time. Stopping scan.. Will resume during next scan.");
                            logger.Debug("Not processing current file: " + currFileObj.Filename);
                            break;
                        }

                        currFileObj.ImageURL = url;
                        currFileObj.Season = int.Parse(season.Name.Split(' ')[1], CultureInfo.CurrentCulture);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Encountered an error parsing: {currFileObj.Filename}");
                        logger.Error(e);
                    }
                    finally
                    {
                        // save to "database" : Table currRunScanList
                        // WriteFile(append, currRunScanList, JsonConvert.SerializeObject(currFileObj) + ";;;");
                        db.ExecuteSQL("INSERT INTO CurrRunData (Filename, Title, Season, Episode, SeriesOverview, ImageURL, ItemID, PosterPath) " +
                                "VALUES (" +
                                "'" + currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal) + "'," +
                                "'" + currFileObj.Title.Replace("'", string.Empty, StringComparison.Ordinal) + "'," +
                                currFileObj.Season + "," +
                                currFileObj.Episode + "," +
                                "'" + currFileObj.SeriesOverview.Replace("'", string.Empty, StringComparison.Ordinal) + "'," +
                                "'" + currFileObj.ImageURL.Replace("'", string.Empty, StringComparison.Ordinal) + "'," +
                                "'" + currFileObj.ItemID.Replace("'", string.Empty, StringComparison.Ordinal) + "'," +
                                "'" + currFileObj.PosterPath.Replace("'", string.Empty, StringComparison.Ordinal) + "'" +
                                ");");
                    }
                }
                else
                {
                    logger.Debug("\"" + currFileObj.Filename + "\" has already been processed either by Previous or Current Newsletter!");
                }
            }

            currCount++;
        }
    }

    private bool InDatabase(string tableName, string fileName)
    {
        foreach (var row in db.Query("SELECT COUNT(*) FROM " + tableName + " WHERE Filename='" + fileName + "';"))
        {
            if (row is not null)
            {
                if (int.Parse(row[0].ToString(), CultureInfo.CurrentCulture) > 0)
                {
                    logger.Debug(tableName + " Size: " + row[0].ToString());
                    return true;
                }
            }
        }

        return false;
    }

    private string SetImageURL(JsonFileObj currObj)
    {
        JsonFileObj fileObj;

        // check if URL for series already exists CurrRunData table
        foreach (var row in db.Query("SELECT * FROM CurrRunData;"))
        {
            if (row is not null)
            {
                fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // check if URL for series already exists CurrNewsletterData table
        foreach (var row in db.Query("SELECT * FROM CurrNewsletterData;"))
        {
            if (row is not null)
            {
                fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // check if URL for series already exists ArchiveData table
        foreach (var row in db.Query("SELECT * FROM ArchiveData;"))
        {
            if (row is not null)
            {
                fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // string url = ng.FetchImagePoster(obj.Title);
        logger.Debug("Uploading poster...");
        // return string.Empty;
        return ng.FetchImagePoster(currObj.PosterPath);
    }

    private void CopyCurrRunDataToNewsletterData()
    {
        // -> copy CurrData Table to NewsletterDataTable
        // -> clear CurrData table
        db.ExecuteSQL("INSERT INTO CurrNewsletterData SELECT * FROM CurrRunData;");
        db.ExecuteSQL("DELETE FROM CurrRunData;");
    }
}