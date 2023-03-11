#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.Scripts.Scraper;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Newsletters.Scripts.HTMLBuilder;

public class HtmlBuilder
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    private readonly string newslettersDir;
    private readonly string newsletterFile;

    private readonly string archiveFile;

    // Non-readonly
    private static string append = "Append";
    private static string write = "Overwrite";
    private static int totalFileCount;
    private IProgress<double> progress;
    private List<JsonSeriesObj> seriesList;
    // private List<string> fileList;

    public HtmlBuilder(IProgress<double> passedProgress, int fileCount)
    {
        config = Plugin.Instance!.Configuration;
        progress = passedProgress;
        totalFileCount = fileCount;
        archiveFile = config.TempDirectory + "/Newsletters/Archive.txt";
        seriesList = new List<JsonSeriesObj>();
        newslettersDir = config.TempDirectory + "/Newsletters/myNewsletters/";
        Directory.CreateDirectory(newslettersDir);

        // if no newsletter filename is saved or the file doesn't exist
        if (config.NewsletterFileName.Length == 0 || File.Exists(newslettersDir + config.NewsletterFileName))
        {
            // use date to create filename
            string currDate = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            newsletterFile = newslettersDir + currDate + "newsletter.html";
        }
        else
        {
            newsletterFile = newslettersDir + config.NewsletterFileName;
        }

        WriteFile(write, "/ssl/htmlbuilder.log", newslettersDir);
    }

    public Task GenerateHTMLfromTemplate()
    {
        PopulateFromArchive();
        string entries = CreateHTMLEntries();

        return Task.CompletedTask;
    }

    private void PopulateFromArchive()
    {
        if (File.Exists(archiveFile))
        {
            StreamReader sr = new StreamReader(archiveFile);
            string arFile = sr.ReadToEnd();
            foreach (string series in arFile.Split(";;"))
            {
                JsonSeriesObj currObj = new JsonSeriesObj();
                currObj.Title = series;
                seriesList.Add(currObj);
            }
        }
    }

    private string CreateHTMLEntries()
    {
        StreamReader sr = new StreamReader(config.TempDirectory + "/Newsletters/currList.txt");
        string readScrapeFile = sr.ReadToEnd();
        // WriteFile(write, "/ssl/mystreamreader.txt", readScrapeFile);
        foreach (string? ep in readScrapeFile.Split(";;"))
        {
            WriteFile(append, "/ssl/logs.txt", "Looping - Episode: " + ep + "\n");
            JsonFileObj? obj = JsonConvert.DeserializeObject<JsonFileObj?>(ep);
            if (obj is not null)
            {
                // WriteFile(append, "/ssl/mystreamreader_single.txt", "\n" + obj.Title);
                if (!IsExisting(obj))
                {
                    JsonSeriesObj currObj = new JsonSeriesObj();
                    currObj.Title = obj.Title;
                    seriesList.Add(currObj);

                    // var imgUrl = FetchImagePoster(obj);
                    WriteFile(append, "/ssl/looplogger.log", obj.Title + "\n");
                }
            }

            // break;
        }

        return string.Empty;
    }

    private bool IsExisting(JsonFileObj obj)
    {
        bool isExisting = false;
        foreach (JsonSeriesObj item in seriesList)
        {
            if (obj.Title == item.Title)
            {
                isExisting = true;
                break;
            }
        }

        return isExisting;
    }

    private async Task<string> FetchImagePoster(JsonFileObj obj)
    {
        HttpClient hc = new HttpClient();

        string url = "https://www.googleapis.com/customsearch/v1?key=" + config.ApiKey + "&cx=" + config.CXKey + "&num=1&searchType=image&fileType=jpg&q=" + string.Join("%", obj.Title.Split(" "));
        // google API image search: curl 'https://www.googleapis.com/customsearch/v1?key=AIzaSyBbh1JoIyThpTHa_WT8k1apsMBUC9xUCEs&cx=4688c86980c2f4d18&num=1&searchType=image&fileType=jpg&q=my%hero%academia'

        string res = await hc.GetStringAsync(url).ConfigureAwait(false);

        WriteFile(write, "/ssl/url_response.log", res);

        return string.Empty;
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

public class JsonSeriesObj
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSeriesObj"/> class.
    /// </summary>
    public JsonSeriesObj()
    {
        Title = string.Empty;
    }

    public string Title { get; set; }
}