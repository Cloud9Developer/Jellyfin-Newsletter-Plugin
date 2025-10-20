#pragma warning disable 1591, CA1002, SA1005 // remove SA1005 to clean code
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
using Jellyfin.Plugin.Newsletters.Scanner.NLImageHandler;
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
    // private readonly string currRunScanList;
    // private readonly string archiveFile;
    // private readonly string currNewsletterDataFile;
    private readonly ILibraryManager libManager;

    // Non-readonly
    private int totalLibCount;
    private int currCount;
    private PosterImageHandler imageHandler;
    private SQLiteDatabase db;
    private JsonFileObj jsonHelper;
    private Logger logger;
    private IProgress<double> progress;
    private CancellationToken cancelToken;
    // private List<JsonFileObj> archiveObj;

    public Scraper(ILibraryManager libraryManager, IProgress<double> passedProgress, CancellationToken cancellationToken)
    {
        logger = new Logger();
        jsonHelper = new JsonFileObj();
        progress = passedProgress;
        cancelToken = cancellationToken;
        config = Plugin.Instance!.Configuration;
        libManager = libraryManager;

        totalLibCount = currCount = 0;

        imageHandler = new PosterImageHandler();
        db = new SQLiteDatabase();

        logger.Debug("Setting Config Paths: ");
        logger.Debug("\n  DataPath: " + config.DataPath +
                     "\n  TempDirectory: " + config.TempDirectory +
                     "\n  PluginsPath: " + config.PluginsPath +
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
        if (!config.SeriesEnabled && !config.MoviesEnabled)
        {
            logger.Info("No Libraries Enabled In Config!");
        }

        if (config.SeriesEnabled)
        {
            InternalItemsQuery series = new InternalItemsQuery();
            series.IncludeItemTypes = new[] { BaseItemKind.Episode };
            BuildObjs(libManager.GetItemList(series).ToList(), "Series"); // populate series

            // BuildObjs(
            //     libManager.GetItemList(new InternalItemsQuery
            //     {
            //         IncludeItemTypes = new[] { BaseItemKind.Episode }
            //     }),
            //     "Series"); 
        }

        if (config.MoviesEnabled)
        {
            InternalItemsQuery movie = new InternalItemsQuery();
            movie.IncludeItemTypes = new[] { BaseItemKind.Movie };
            BuildObjs(libManager.GetItemList(movie).ToList(), "Movie"); // populate movies

            // BuildObjs(
            //     libManager.GetItemList(new InternalItemsQuery
            //     {
            //         IncludeItemTypes = new[] { BaseItemKind.Movie }
            //     }),
            //     "Movie");
        }
    }

    public void BuildObjs(List<BaseItem> items, string type)
    {
        logger.Info($"Parsing {type}..");
        BaseItem episode, season, series;
        totalLibCount = items.Count;
        logger.Info($"Scan Size: {totalLibCount}");
        logger.Info($"Scanning '{type}'");
        foreach (BaseItem item in items)
        {
            logger.Debug("---------------");
            currCount++;
            progress.Report((double)currCount / (double)totalLibCount * 100);
            if (item is not null)
            {
                try
                {
                    if (type == "Series")
                    {
                        episode = item;
                        season = item.GetParent();
                        series = item.GetParent().GetParent();
                    }
                    else if (type == "Movie")
                    {
                        episode = season = series = item;
                    }
                    else
                    {
                        logger.Error("Something went wrong..");
                        continue;
                    }

                    logger.Debug($"ItemId: " + series.Id.ToString("N")); // series ItemId
                    logger.Debug($"{type}: {series.Name}"); // Title
                    logger.Debug($"LocationType: " + episode.LocationType.ToString());
                    if (episode.LocationType.ToString() == "Virtual")
                    {
                        logger.Debug($"No physical path.. Skipping...");
                        continue;
                    }

                    logger.Debug($"Season: {season.Name}"); // Season Name
                    logger.Debug($"Episode Name: {episode.Name}"); // episode Name
                    logger.Debug($"Episode Number: {episode.IndexNumber}"); // episode Name
                    logger.Debug($"Overview: {series.Overview}"); // series overview
                    logger.Debug($"ImageInfo: {series.PrimaryImagePath}");
                    logger.Debug($"Filepath: " + episode.Path); // Filepath, episode.Path is cleaner, but may be empty

                    // NEW PARAMS
                    logger.Debug($"PremiereDate: {series.PremiereDate}"); // series PremiereDate
                    logger.Debug($"OfficialRating: " + series.OfficialRating); // TV-14, TV-PG, etc
                    // logger.Info($"CriticRating: " + series.CriticRating);
                    // logger.Info($"CustomRating: " + series.CustomRating);
                    logger.Debug($"CommunityRating: " + series.CommunityRating); // 8.5, 9.2, etc
                    logger.Debug($"RunTime: " + (int)((float)episode.RunTimeTicks! / 10000 / 60000) + " minutes");
                }
                catch (Exception e)
                {
                    logger.Error("Error processing item..");
                    logger.Error(e);
                    continue;
                }

                JsonFileObj currFileObj = new JsonFileObj();
                currFileObj.Filename = episode.Path;
                currFileObj.Title = series.Name;
                currFileObj.Type = type;

                if (series.PremiereDate is not null)
                {
                    currFileObj.PremiereYear = series.PremiereDate.ToString()!.Split(' ')[0].Split('/')[2]; // NEW {PremierYear}
                    logger.Debug($"PremiereYear: {currFileObj.PremiereYear}");
                }

                currFileObj.RunTime = (int)((float)episode.RunTimeTicks / 10000 / 60000);
                currFileObj.OfficialRating = series.OfficialRating;
                currFileObj.CommunityRating = series.CommunityRating;

                if (!InDatabase("CurrRunData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)) && 
                    !InDatabase("CurrNewsletterData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)) && 
                    !InDatabase("ArchiveData", currFileObj.Filename.Replace("'", string.Empty, StringComparison.Ordinal)))
                {
                    try
                    {
                        if (episode.IndexNumber is int && episode.IndexNumber is not null)
                        {
                            currFileObj.Episode = (int)episode.IndexNumber;
                        }

                        currFileObj.SeriesOverview = series.Overview;
                        currFileObj.ItemID = series.Id.ToString("N");

                        logger.Debug("Checking if Primary Image Exists for series");
                        if (series.PrimaryImagePath != null)
                        {
                            logger.Debug("Primary Image series found!");
                            currFileObj.PosterPath = series.PrimaryImagePath;
                        }
                        else if (episode.PrimaryImagePath != null)
                        {
                            logger.Debug("Primary Image series not found. Pulling from Episode");
                            currFileObj.PosterPath = episode.PrimaryImagePath;
                        }
                        else
                        {
                            logger.Warn("Primary Poster not found..");
                            logger.Warn("This may be due to filesystem not being formatted properly.");
                            logger.Warn($"Make sure {currFileObj.Filename} follows the correct formatting below:");
                            logger.Warn(".../MyLibraryName/Series_Name/Season#_or_Specials/Episode.{ext}");
                        }

                        logger.Debug("Checking if PosterPath Exists");
                        if ((currFileObj.PosterPath != null) && (currFileObj.PosterPath.Length > 0))
                        {
                            string url = SetImageURL(currFileObj);

                            if ((url == "429") || (url == "ERR"))
                            {
                                logger.Debug("URL is not attainable at this time. Stopping scan.. Will resume during next scan.");
                                logger.Debug("Not processing current file: " + currFileObj.Filename);
                                break;
                            }

                            currFileObj.ImageURL = url;
                        }

                        logger.Debug("Parsing Season Number");
                        try
                        {
                            currFileObj.Season = int.Parse(season.Name.Split(' ')[1], CultureInfo.CurrentCulture);
                        }
                        catch (Exception e)
                        {
                            logger.Warn($"Encountered an error parsing Season Number for: {currFileObj.Filename}");
                            logger.Debug(e);
                            logger.Warn("Setting Season number to 0 (SPECIALS)");
                            currFileObj.Season = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Encountered an error parsing: {currFileObj.Filename}");
                        logger.Error(e);
                    }
                    finally
                    {
                        // save to "database" : Table currRunScanList
                        logger.Debug("Adding to CurrRunData DB...");
                        currFileObj = NoNull(currFileObj);
                        db.ExecuteSQL("INSERT INTO CurrRunData (Filename, Title, Season, Episode, SeriesOverview, ImageURL, ItemID, PosterPath, Type, PremiereYear, RunTime, OfficialRating, CommunityRating) " +
                                "VALUES (" +
                                    SanitizeDbItem(currFileObj.Filename) +
                                    "," + SanitizeDbItem(currFileObj!.Title) +
                                    "," + ((currFileObj?.Season is null) ? -1 : currFileObj.Season) +
                                    "," + ((currFileObj?.Episode is null) ? -1 : currFileObj.Episode) +
                                    "," + SanitizeDbItem(currFileObj!.SeriesOverview) +
                                    "," + SanitizeDbItem(currFileObj!.ImageURL) +
                                    "," + SanitizeDbItem(currFileObj.ItemID) +
                                    "," + SanitizeDbItem(currFileObj!.PosterPath) +
                                    "," + SanitizeDbItem(currFileObj.Type) +
                                    "," + SanitizeDbItem(currFileObj!.PremiereYear) + 
                                    "," + ((currFileObj?.RunTime is null) ? -1 : currFileObj.RunTime) +
                                    "," + SanitizeDbItem(currFileObj!.OfficialRating) +
                                    "," + ((currFileObj?.CommunityRating is null) ? -1 : currFileObj.CommunityRating) +
                                ");");
                        logger.Debug("Complete!");
                    }
                }
                else
                {
                    logger.Debug("\"" + currFileObj.Filename + "\" has already been processed either by Previous or Current Newsletter!");
                }
            }
        }
    }

    private JsonFileObj NoNull(JsonFileObj currFileObj)
    {
        if (currFileObj.Filename == null)
        {
            currFileObj.Filename = string.Empty;
        }

        if (currFileObj.Title == null)
        {
            currFileObj.Title = string.Empty;
        }

        if (currFileObj.SeriesOverview == null)
        {
            currFileObj.SeriesOverview = string.Empty;
        }

        if (currFileObj.ImageURL == null)
        {
            currFileObj.ImageURL = string.Empty;
        }

        if (currFileObj.ItemID == null)
        {
            currFileObj.Filename = string.Empty;
        }

        if (currFileObj.PosterPath == null)
        {
            currFileObj.PosterPath = string.Empty;
        }

        return currFileObj;
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
        string currTitle = currObj.Title.Replace("'", string.Empty, StringComparison.Ordinal);

        // check if URL for series already exists CurrRunData table
        foreach (var row in db.Query("SELECT * FROM CurrRunData;"))
        {
            if (row is not null)
            {
                fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currTitle) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currTitle + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // check if URL for series already exists CurrNewsletterData table
        logger.Debug("Checking if exists in CurrNewsletterData");
        foreach (var row in db.Query("SELECT * FROM CurrNewsletterData;"))
        {
            if (row is not null)
            {
                fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currTitle) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currTitle + " :: " + fileObj.ImageURL);
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
                if ((fileObj is not null) && (fileObj.Title == currTitle) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currTitle + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        logger.Debug("Uploading poster...");
        logger.Debug(currObj.ItemID);
        logger.Debug(currObj.PosterPath);
        // return string.Empty;
        return imageHandler.FetchImagePoster(currObj);
    }

    private void CopyCurrRunDataToNewsletterData()
    {
        // -> copy CurrData Table to NewsletterDataTable
        // -> clear CurrData table
        db.ExecuteSQL("INSERT INTO CurrNewsletterData SELECT * FROM CurrRunData;");
        db.ExecuteSQL("DELETE FROM CurrRunData;");
    }

    private string SanitizeDbItem(string unsanitized_string)
    {
        // string sanitize_string = string.Empty;
        if (unsanitized_string is null)
        {
            unsanitized_string = string.Empty;
        }

        return "'" + unsanitized_string.Replace("'", string.Empty, StringComparison.Ordinal) + "'";
    }
}