#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
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
    // private readonly IApplicationPaths appPaths;
    // private readonly string appPaths;
    // private readonly string archiveList;
    private readonly string currScanList;

    // Non-readonly
    private int fileCount = 0;
    private static string append = "Append";
    private static string write = "Overwrite";
    // private List<string> fileList;

    public Scraper()
    {
        config = Plugin.Instance!.Configuration;
        // archiveList = "/ssl/archive.txt";
        currScanList = config.MediaDir + "/currScan.txt";
        WriteFile(write, currScanList, string.Empty);
        // WriteFile(write, "/ssl/myconfig.txt", config.applicationPaths);
        // fileList = new List<string>();
        // appPaths = ;
    }

    public Task GetSeriesData(IProgress<double> progress)
    {
        progress.Report(0);

        string filePath = config.MediaDir; // Prerolls works. Movies not due to spaces

        List<string> fileList = GetFileList(@filePath);

        List<JsonFileObj> fileObjList = ConvertToJsonList(fileList);

        // google API image search: curl 'https://www.googleapis.com/customsearch/v1?key=AIzaSyBbh1JoIyThpTHa_WT8k1apsMBUC9xUCEs&cx=4688c86980c2f4d18&num=1&searchType=image&fileType=jpg&q=my%hero%academia'

        return Task.CompletedTask;
    }

    private List<JsonFileObj> ConvertToJsonList(List<string> fileList)
    {
        List<JsonFileObj> objList = new List<JsonFileObj>();
        foreach (var file in fileList)
        {
            // JsonFileObj obj = new JsonFileObj()
            // {
            //     Filename = file
            // };
            JsonFileObj obj = ConvertToJsonObj(file);

            objList.Add(obj);
        }

        return objList;
    }

    private JsonFileObj ConvertToJsonObj(string file)
    {
        JsonFileObj obj = new JsonFileObj()
        {
            Filename = file
        };

        return obj;
    }

    private List<string> GetFileList(string dirPath)
    {
        // File.AppendAllText("/ssl/logs.txt", "DIR: " + dirPath + ";\n");
        List<string> newFileList = new List<string>();
        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(@dirPath);
        foreach (string filePath in @fileEntries)
        {
            string[] excludedExt = { ".srt" };
            bool contBool = false;
            foreach (string ext in excludedExt)
            {
                if (filePath.EndsWith(ext, StringComparison.Ordinal))
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
            string currFileName = currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 1];
            currFileObj.Title = string.Join(" ", currFileObj.Filename.Split('/')[currFileObj.Filename.Split('/').Length - 3].Split('_'));
            currFileObj.Season = currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[0];
            currFileObj.Episode = "E" + currFileName.Split('-')[currFileName.Split('-').Length - 1].Split('E')[1].Split('.')[0];

            WriteFile(append, currScanList, JsonConvert.SerializeObject(currFileObj) + ";");
            newFileList.Add(@filePath);
            fileCount++;
        }

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(@dirPath);
        foreach (string subdirectory in @subdirectoryEntries)
        {
            GetFileList(@subdirectory);
        }

        return newFileList;
    }

/*
    private int GetListCount(List<string> list)
    {
        int count = 0;
        foreach (string file in list)
        {
            count++;
        }

        return count;
    }
*/

    private void WriteFile(string method, string path, string value)
    {
        if (method == "Append")
        {
            File.AppendAllText(path, value);
        }
        else if (method == "Overwrite")
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
        Season = string.Empty;
        Episode = string.Empty;
        Description = string.Empty;
        ImageURL = string.Empty;
    }

    public string Filename { get; set; }

    public string Title { get; set; }

    public string Season { get; set; }

    public string Episode { get; set; }

    public string Description { get; set; }

    public string ImageURL { get; set; }
}