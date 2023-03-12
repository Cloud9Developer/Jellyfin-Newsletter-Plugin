#pragma warning disable 1591, SYSLIB0014, CA1002
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

namespace Jellyfin.Plugin.Newsletters.Scripts.NLDataGenerator;

public class NewsletterDataGenerator
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    private readonly string newslettersDir;
    private readonly string newsletterDataFile;

    private readonly string currRunList;
    private readonly string archiveFile;
    private readonly string myDataDir;

    // Non-readonly
    private static string append = "Append";
    private static string write = "Overwrite";
    private static int totalFileCount;
    private IProgress<double> progress;
    private List<JsonFileObj> archiveSeriesList;
    // private List<string> fileList;

    public NewsletterDataGenerator(IProgress<double> passedProgress)
    {
        config = Plugin.Instance!.Configuration;
        progress = passedProgress;
        myDataDir = config.TempDirectory + "/Newsletters";

        archiveFile = config.MyDataDir + config.ArchiveFileName; // curlist/archive
        currRunList = config.MyDataDir + config.CurrRunListFileName;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;

        archiveSeriesList = new List<JsonFileObj>();
        newslettersDir = config.NewsletterDir; // newsletterdir
        Directory.CreateDirectory(newslettersDir);

        // WriteFile(write, "/ssl/htmlbuilder.log", newslettersDir); // testing
    }

    public NewsletterDataGenerator(IProgress<double> passedProgress, int fileCount)
    {
        config = Plugin.Instance!.Configuration;
        progress = passedProgress;
        totalFileCount = fileCount;
        myDataDir = config.TempDirectory + "/Newsletters";

        archiveFile = config.MyDataDir + config.ArchiveFileName; // curlist/archive
        currRunList = config.MyDataDir + config.CurrRunListFileName;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;

        archiveSeriesList = new List<JsonFileObj>();
        newslettersDir = config.NewsletterDir; // newsletterdir
        Directory.CreateDirectory(newslettersDir);

        // WriteFile(write, "/ssl/htmlbuilder.log", newslettersDir); // Testing
    }

    public Task GenerateDataForNextNewsletter()
    {
        archiveSeriesList = PopulateFromArchive();
        string entries = GenerateData();
        CopyCurrRunDataToNewsletterData();

        return Task.CompletedTask;
    }

    public List<JsonFileObj> PopulateFromArchive()
    {
        List<JsonFileObj> myObj = new List<JsonFileObj>();
        if (File.Exists(archiveFile))
        {
            StreamReader sr = new StreamReader(archiveFile);
            string arFile = sr.ReadToEnd();
            foreach (string series in arFile.Split(";;"))
            {
                JsonFileObj? currObj = JsonConvert.DeserializeObject<JsonFileObj?>(series);
                if (currObj is not null)
                {
                    myObj.Add(currObj);
                }
            }

            sr.Close();
        }

        return myObj;
    }

    private string GenerateData()
    {
        StreamReader sr = new StreamReader(currRunList); // curlist/archive
        string readScrapeFile = sr.ReadToEnd();
        // WriteFile(write, "/ssl/mystreamreader.txt", readScrapeFile);
        foreach (string? ep in readScrapeFile.Split(";;"))
        {
            WriteFile(append, "/ssl/logs.txt", "Looping - Episode: " + ep + "\n");
            JsonFileObj? obj = JsonConvert.DeserializeObject<JsonFileObj?>(ep);
            if (obj is not null)
            {
                // WriteFile(append, "/ssl/mystreamreader_single.txt", "\n" + obj.Title);

                JsonFileObj currObj = new JsonFileObj();
                currObj.Title = obj.Title;
                archiveSeriesList.Add(currObj);

                // string imgUrl = FetchImagePoster(obj);
                // WriteFile(append, "/ssl/myparsedURL.txt", imgUrl);
                WriteFile(append, "/ssl/looplogger.log", obj.Title + "\n");
            }

            break;
        }

        sr.Close();

        return string.Empty;
    }

    private string FetchImagePoster(JsonFileObj obj)
    {
        string url = "https://www.googleapis.com/customsearch/v1?key=" + config.ApiKey + "&cx=" + config.CXKey + "&num=1&searchType=image&fileType=jpg&q=" + string.Join("%", obj.Title.Split(" "));
        // google API image search: curl 'https://www.googleapis.com/customsearch/v1?key=AIzaSyBbh1JoIyThpTHa_WT8k1apsMBUC9xUCEs&cx=4688c86980c2f4d18&num=1&searchType=image&fileType=jpg&q=my%hero%academia'

        // HttpClient hc = new HttpClient();
        // string res = await hc.GetStringAsync(url).ConfigureAwait(false);
        WebClient wc = new WebClient();
        string res = wc.DownloadString(url);
        string urlResFile = myDataDir + "/.lasturlresponse";

        WriteFile(write, urlResFile, res);

        bool testForItems = false;

        foreach (string line in File.ReadAllLines(urlResFile))
        {
            WriteFile(write, "/ssl/testUrlReader.txt", line); // testing
            if (testForItems)
            {
                if (line.Contains("\"link\":", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Split("\"")[3];
                }
            }
            else
            {
                if (line.Contains("\"items\":", StringComparison.OrdinalIgnoreCase))
                {
                    testForItems = true;
                }
            }
        }

        return string.Empty;
    }

    private void CopyCurrRunDataToNewsletterData()
    {
        if (File.Exists(currRunList)) // archiveFile
        {
            Stream input = File.OpenRead(currRunList);
            Stream output = new FileStream(newsletterDataFile, FileMode.Append, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
            File.Delete(currRunList);
        }
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