#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

namespace Jellyfin.Plugin.Newsletters.Scripts.Scraper;

public class Scraper
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    // private readonly string archiveList;
    // private readonly string currRunScanListDir;
    private readonly string currRunScanList;
    private readonly string currNewsletterDataFile;

    // Non-readonly
    private int fileCount = 0;
    private static string append = "Append";
    private static string write = "Overwrite";
    private IProgress<double> progress;
    private NewsletterDataGenerator ng;
    private Logger logger;

    // private List<string> fileList;

    public Scraper(IProgress<double> passedProgress)
    {
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        progress = passedProgress;
        // archiveList = "/ssl/archive.txt";
        // currRunScanListDir = config.TempDirectory + "/Newsletters/";
        currRunScanList = config.MyDataDir + config.CurrRunListFileName;
        currNewsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
        Directory.CreateDirectory(config.MyDataDir);
        WriteFile(write, currRunScanList, string.Empty); // overwrite currscan data on run (testing purposes)
        ng = new NewsletterDataGenerator(progress);
        // fileList = new List<string>();
        // appPaths = ;
    }

    public Task GetSeriesData()
    {
        progress.Report(0);

        string filePath = config.MediaDir; // Prerolls works. Movies not due to spaces)

        GetFileListToProcess(filePath);

        logger.Info("Scanned " + fileCount + " files" );

        WriteFile(write, "/ssl/testconfigpath.txt", config.PluginsPath);

        progress.Report(50);

        // NewsletterDataGenerator nlDG = new NewsletterDataGenerator(progress, fileCount);
        return ng.GenerateDataForNextNewsletter();
    }

    private JsonFileObj ConvertToJsonObj(string file)
    {
        JsonFileObj obj = new JsonFileObj()
        {
            Filename = file
        };

        return obj;
    }

    private void GetFileListToProcess(string dirPath)
    {
        // File.AppendAllText("/ssl/logs.txt", "DIR: " + dirPath + ";\n");
        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(dirPath);
        // check if already existing before writing to file
        List<JsonFileObj> archiveObj = ng.PopulateFromArchive();

        foreach (string filePath in fileEntries)
        {
            string[] excludedExt = { ".srt", ".txt", ".srt", ".saa", ".ttml", ".sbv", ".dfxp", ".vtt" };
            bool contBool = false;
            foreach (string ext in excludedExt)
            {
                if (filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    contBool = true;
                    break;
                }
            }

            if (contBool)
            {
                continue;
            }

            // File.AppendAllText("/ssl/logs.txt", "FILE: " + filePath + ";\n");
            JsonFileObj currFileObj = ConvertToJsonObj(filePath);

            // get Title, Season, Episode, Description
            try
            {
                if (!AlreadyInArchive(archiveObj, currFileObj) && !AlreadyInCurrNewsletterData(archiveObj, currFileObj))
                {
                    string currFileName = currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 1];
                    currFileObj.Title = string.Join(" ", currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 3].Split('_'));
                    currFileObj.Season = int.Parse(currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[0].Split('S')[1], CultureInfo.CurrentCulture);
                    currFileObj.Episode = int.Parse(currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[1].Split('.')[0].Trim(), CultureInfo.CurrentCulture);
                    currFileObj.ImageURL = SetImageURL(currFileObj);
                    WriteFile(append, currRunScanList, JsonConvert.SerializeObject(currFileObj) + ";;");
                }
                else
                {
                    logger.Debug("\"" + currFileObj.Filename + "\" has already been processed either by Previous or Current Newsletter!");
                }

                fileCount++;
            }
            finally
            {
                fileCount++;
            }
        }

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(@dirPath);
        foreach (string subdirectory in @subdirectoryEntries)
        {
            GetFileListToProcess(@subdirectory);
        }
    }

    private string SetImageURL(JsonFileObj currObj)
    {
        // check if URL for series already exists in CurrNewsletterData from Previous scan
        if (File.Exists(currNewsletterDataFile))
        {
            foreach (string item in File.ReadAllText(currNewsletterDataFile).Split(";;"))
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
        foreach (string item in File.ReadAllText(currRunScanList).Split(";;"))
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
        // return ng.FetchImagePoster(currObj.Title);
        // return string.Empty;
        return "https://m.media-amazon.com/images/W/IMAGERENDERING_521856-T1/images/I/91eNqTeYvzL.jpg";
    }

    private bool AlreadyInCurrNewsletterData(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    {
        List<JsonFileObj> currNLObj = new List<JsonFileObj>();
        if (File.Exists(currNewsletterDataFile))
        {
            StreamReader sr = new StreamReader(currNewsletterDataFile);
            string nlFile = sr.ReadToEnd();
            foreach (string ep in nlFile.Split(";;"))
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
            // WriteFile(append, "/ssl/compareTitle.txt", "element: " + element.Filename + ";; currFileObj: " + currFileObj.Filename + "\n");
            if (element.Filename == currFileObj.Filename)
            {
                WriteFile(append, "/ssl/compareTitle.txt", "element: " + element.Filename + ";; currFileObj: " + currFileObj.Filename + "\n");

                return true;
            }
        }

        return false;
    }

    private bool AlreadyInArchive(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    {
        foreach (JsonFileObj element in archiveObj)
        {
            // WriteFile(append, "/ssl/compareTitle.txt", "element: " + element.Filename + ";; currFileObj: " + currFileObj.Filename + "\n");
            if (element.Filename == currFileObj.Filename)
            {
                WriteFile(append, "/ssl/compareTitle.txt", "element: " + element.Filename + ";; currFileObj: " + currFileObj.Filename + "\n");

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
        Description = string.Empty;
        ImageURL = string.Empty;
    }

    public string Filename { get; set; }

    public string Title { get; set; }

    // public string Season { get; set; }

    public int Season { get; set; }

    public int Episode { get; set; }

    // public string Episode { get; set; }

    public string Description { get; set; }

    public string ImageURL { get; set; }
}