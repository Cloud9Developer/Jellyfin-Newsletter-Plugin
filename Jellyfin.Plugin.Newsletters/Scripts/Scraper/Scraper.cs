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
    private readonly string currNewsletterFile;

    // Non-readonly
    private int fileCount = 0;
    private static string append = "Append";
    private static string write = "Overwrite";
    private IProgress<double> progress;
    private NewsletterDataGenerator ng;
    // private List<string> fileList;

    public Scraper(IProgress<double> passedProgress)
    {
        config = Plugin.Instance!.Configuration;
        progress = passedProgress;
        // archiveList = "/ssl/archive.txt";
        // currRunScanListDir = config.TempDirectory + "/Newsletters/";
        currRunScanList = config.MyDataDir + config.CurrRunListFileName;
        currNewsletterFile = config.MyDataDir + config.NewsletterDataFileName;
        Directory.CreateDirectory(config.MyDataDir);
        WriteFile(write, currRunScanList, string.Empty); // overwrite currscan data on run (testing purposes)
        WriteFile(write, "/ssl/myconfig.txt", currRunScanList);
        ng = new NewsletterDataGenerator(progress);
        // fileList = new List<string>();
        // appPaths = ;
    }

    public Task GetSeriesData()
    {
        progress.Report(0);

        string filePath = config.MediaDir; // Prerolls works. Movies not due to spaces)

        GetFileListToProcess(filePath);

        WriteFile(write, "/ssl/testconfigpath.txt", config.PluginsPath);

        progress.Report(50);

        NewsletterDataGenerator nlDG = new NewsletterDataGenerator(progress, fileCount);
        return nlDG.GenerateDataForNextNewsletter();
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
                string currFileName = currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 1];
                currFileObj.Title = string.Join(" ", currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 3].Split('_'));
                // currFileObj.Season = currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[0];
                // currFileObj.Episode = currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[1].Split('.')[0];
                // WriteFile(write, "/ssl/testIntFormat.txt", currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[1].Split('.')[0].Trim());
                currFileObj.Season = int.Parse(currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[0].Split('S')[1], CultureInfo.CurrentCulture);
                currFileObj.Episode = int.Parse(currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[1].Split('.')[0].Trim(), CultureInfo.CurrentCulture);
                // int test = int.Parse("003", CultureInfo.CurrentCulture);

                if (!AlreadyInArchive(archiveObj, currFileObj) && !AlreadyInCurrNewsletterData(archiveObj, currFileObj))
                {
                    WriteFile(append, currRunScanList, JsonConvert.SerializeObject(currFileObj) + ";;");
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

    private bool AlreadyInCurrNewsletterData(List<JsonFileObj> archiveObj, JsonFileObj currFileObj)
    {
        List<JsonFileObj> currNLObj = new List<JsonFileObj>();
        if (File.Exists(currNewsletterFile))
        {
            StreamReader sr = new StreamReader(currNewsletterFile);
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