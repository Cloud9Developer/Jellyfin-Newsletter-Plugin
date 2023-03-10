#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
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
    private readonly PluginConfiguration config;
    private int fileCount = 0;
    // private List<string> fileList;

    public Scraper()
    {
        config = Plugin.Instance!.Configuration;
        // fileList = new List<string>();
    }

    public Task GetSeriesData(IProgress<double> progress)
    {
        progress.Report(0);

        string filePath = "/media/PlexMedia/Movies"; // Prerolls works. Movies not due to spaces

        List<string> fileList = GetFileList(@filePath);
        fileCount = GetListCount(fileList);
        File.AppendAllText("/ssl/log_files.txt", string.Join(",", fileList));
        File.AppendAllText("/ssl/logs.txt", "Count: " + fileCount + ";\n");
        List<JsonFileObj> fileObjList = ConvertToJsonList(fileList);

        int count = 0;
        foreach (JsonFileObj obj in fileObjList) // obj is treated as a Json object. Call keys as normal
        {
            progress.Report((int)Math.Round((double)(100 * count) / fileCount));
            File.AppendAllText("/ssl/NewPlugin.txt", JsonConvert.SerializeObject(obj) + ";");
            count++;
        }

        // File.WriteAllText("/ssl/test.txt", "I am here");

        // Thread.Sleep(100);
        return Task.CompletedTask;
    }

    private List<JsonFileObj> ConvertToJsonList(List<string> fileList)
    {
        List<JsonFileObj> objList = new List<JsonFileObj>();
        foreach (var file in fileList)
        {
            JsonFileObj obj = new JsonFileObj()
            {
                Filename = file
            };

            objList.Add(obj);
        }

        return objList;
    }

    private List<string> GetFileList(string dirPath)
    {
        File.AppendAllText("/ssl/logs.txt", "DIR: " + dirPath + ";\n");
        List<string> newFileList = new List<string>();
        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(@dirPath);
        foreach (string fileName in @fileEntries)
        {
            File.AppendAllText("/ssl/logs.txt", "FILE: " + fileName + ";\n");
            newFileList.Add(@fileName);
        }

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(@dirPath);
        foreach (string subdirectory in @subdirectoryEntries)
        {
            GetFileList(@subdirectory);
        }

        return newFileList;
    }

    private int GetListCount(List<string> list)
    {
        int count = 0;
        foreach (string file in list)
        {
            count++;
        }

        return count;
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
    }

    public string Filename { get; set; }

    public string Title { get; set; }

    public string Season { get; set; }

    public string Episode { get; set; }

    public string Description { get; set; }
}