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
using Jellyfin.Plugin.Newsletters.Scripts.DATA;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
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
    private readonly string currRunScanList;
    private readonly string archiveFile;
    private readonly string currNewsletterDataFile;
    private readonly ILibraryManager libManager;

    // Non-readonly
    private int totalLibCount;
    // private static string append = "Append";
    // private static string write = "Overwrite";
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

        totalLibCount = items.Count;
        logger.Info("Scan Size: " + totalLibCount);

        ScanSeries(items);
    }

    private void ScanSeries(List<BaseItem> items)
    {
        foreach (BaseItem? item in items)
        {
            progress.Report((double)((currCount * 100) / totalLibCount));
            cancelToken.ThrowIfCancellationRequested();
            BaseItem episode = item;
            BaseItem season = item.GetParent();
            BaseItem series = item.GetParent().GetParent();
            // logger.Info("Content Type: " + season.MediaType);

            if (item is not null)
            {
                JsonFileObj currFileObj = new JsonFileObj();
                currFileObj.Filename = episode.PhysicalLocations[0];
                currFileObj.Title = series.Name;
                // if (!AlreadyInArchive(archiveObj, currFileObj) && !AlreadyInCurrNewsletterData(currFileObj) && !AlreadyInCurrScanData(currFileObj))
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
                        logger.Error("Encountered an error parsing: " + currFileObj.Filename);
                        logger.Debug("Series: " + series.Name); // Title
                        logger.Debug("Season: " + season.Name); // Season
                        logger.Debug("Episode Name: " + episode.Name); // episode Name
                        logger.Debug("Episode Number: " + episode.IndexNumber); // episode Name
                        logger.Debug("Series Overview: " + series.Overview); // series overview
                        logger.Debug("ImageInfos: " + series.PrimaryImagePath);
                        logger.Debug(series.Id.ToString("N")); // series ItemId
                        logger.Debug(episode.PhysicalLocations[0]); // Filepath
                        logger.Debug("---------------");
                        logger.Debug("Error message:");
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
        // check if URL for series already exists CurrRunData table
        foreach (var row in db.Query("SELECT * FROM CurrRunData;"))
        {
            if (row is not null)
            {
                JsonFileObj fileObj = jsonHelper.ConvertToObj(row);
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
                JsonFileObj fileObj = jsonHelper.ConvertToObj(row);
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
                JsonFileObj fileObj = jsonHelper.ConvertToObj(row);
                if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
                {
                    logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
                    return fileObj.ImageURL;
                }
            }
        }

        // ----------

        // check if URL for series already exists in CurrList
        // if (File.Exists(currRunScanList)) // Database : If currRunScanList table exists
        // {
        //     foreach (string item in File.ReadAllText(currRunScanList).Split(";;;"))
        //     {
        //         JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
        //         if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
        //         {
        //             logger.Debug("Found Current Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
        //             return fileObj.ImageURL;
        //         }
        //     }
        // }

        // // check if URL for series already exists in CurrNewsletterData from Previous scan
        // if (File.Exists(currNewsletterDataFile)) // Database : If currNewsletterData table exists
        // {
        //     foreach (string item in File.ReadAllText(currNewsletterDataFile).Split(";;;"))
        //     {
        //         JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
        //         if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
        //         {
        //             logger.Debug("Found Previous Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
        //             return fileObj.ImageURL;
        //         }
        //     }
        // }

        // // check if URL for series already exists in Archive from Previous scan
        // if (File.Exists(archiveFile)) // Database : If archiveData table exists
        // {
        //     foreach (string item in File.ReadAllText(archiveFile).Split(";;;"))
        //     {
        //         JsonFileObj? fileObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
        //         if ((fileObj is not null) && (fileObj.Title == currObj.Title) && (fileObj.ImageURL.Length > 0))
        //         {
        //             logger.Debug("Found Previous Scan of URL for " + currObj.Title + " :: " + fileObj.ImageURL);
        //             return fileObj.ImageURL;
        //         }
        //     }
        // }

        // string url = ng.FetchImagePoster(obj.Title);
        logger.Debug("Uploading poster...");
        // return string.Empty;
        return ng.FetchImagePoster(currObj.PosterPath);
    }

    // private bool AlreadyInCurrScanData(JsonFileObj currFileObj)
    // {
    //     List<JsonFileObj> currScanObj;
    //     if (File.Exists(currNewsletterDataFile))
    //     {
    //         currScanObj = new List<JsonFileObj>();
    //         StreamReader sr = new StreamReader(currNewsletterDataFile);
    //         string nlFile = sr.ReadToEnd();
    //         foreach (string ep in nlFile.Split(";;;"))
    //         {
    //             JsonFileObj? currObj = JsonConvert.DeserializeObject<JsonFileObj?>(ep);
    //             if (currObj is not null)
    //             {
    //                 currScanObj.Add(currObj);
    //             }
    //         }

    //         sr.Close();
    //         foreach (JsonFileObj element in currScanObj)
    //         {
    //             if (element.Filename == currFileObj.Filename)
    //             {
    //                 return true;
    //             }
    //         }
    //     }

    //     return false;
    // }

    // private bool AlreadyInCurrNewsletterData(JsonFileObj currFileObj)
    // {
    //     List<JsonFileObj> currNLObj;
    //     if (File.Exists(currNewsletterDataFile))
    //     {
    //         currNLObj = new List<JsonFileObj>();
    //         StreamReader sr = new StreamReader(currNewsletterDataFile);
    //         string nlFile = sr.ReadToEnd();
    //         foreach (string ep in nlFile.Split(";;;"))
    //         {
    //             JsonFileObj? currObj = JsonConvert.DeserializeObject<JsonFileObj?>(ep);
    //             if (currObj is not null)
    //             {
    //                 currNLObj.Add(currObj);
    //             }
    //         }

    //         sr.Close();
    //         foreach (JsonFileObj element in currNLObj)
    //         {
    //             if (element.Filename == currFileObj.Filename)
    //             {
    //                 return true;
    //             }
    //         }
    //     }

    //     return false;
    // }

    // private bool AlreadyInArchive(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    // {
    //     foreach (JsonFileObj element in archiveObj)
    //     {
    //         if (element.Filename == currFileObj.Filename)
    //         {
    //             return true;
    //         }
    //     }

    //     return false;
    // }

    private void CopyCurrRunDataToNewsletterData()
    {
        // -> copy CurrData Table to NewsletterDataTable
        // -> clear CurrData table
        db.ExecuteSQL("INSERT INTO CurrNewsletterData SELECT * FROM CurrRunData;");
        db.ExecuteSQL("DELETE FROM CurrRunData;");

        // if (File.Exists(currRunScanList)) // archiveFile
        // {
        //     Stream input = File.OpenRead(currRunScanList);
        //     Stream output = new FileStream(currNewsletterDataFile, FileMode.Append, FileAccess.Write, FileShare.None);
        //     // Database : copy currRunScanList table entries to currNewsletterData table
        //     input.CopyTo(output);
        //     // Clear/Delete currRunScanList table
        //     File.Delete(currRunScanList);
        // }
    }

    // private void WriteFile(string method, string path, string value)
    // {
    //     if (method == append)
    //     {
    //         File.AppendAllText(path, value);
    //     }
    //     else if (method == write)
    //     {
    //         File.WriteAllText(path, value);
    //     }
    // }
}