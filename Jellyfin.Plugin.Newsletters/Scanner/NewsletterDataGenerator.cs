#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using Jellyfin.Plugin.Newsletters.Scripts.SCRAPER;
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

namespace Jellyfin.Plugin.Newsletters.Scanner.NLDataGenerator;

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
    private Logger logger;
    private SQLiteDatabase db;

    // Non-readonly
    private List<JsonFileObj> archiveSeriesList;
    // private List<string> fileList;

    public NewsletterDataGenerator()
    {
        logger = new Logger();
        db = new SQLiteDatabase();
        config = Plugin.Instance!.Configuration;
        myDataDir = config.TempDirectory + "/Newsletters";

        archiveFile = config.MyDataDir + config.ArchiveFileName; // curlist/archive
        currRunList = config.MyDataDir + config.CurrRunListFileName;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;

        archiveSeriesList = new List<JsonFileObj>();
        newslettersDir = config.NewsletterDir; // newsletterdir
        Directory.CreateDirectory(newslettersDir);
    }

    public Task GenerateDataForNextNewsletter()
    {
        try
        {
            db.CreateConnection();
            archiveSeriesList = PopulateFromArchive(db); // Files that shouldn't be processed again
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

    public List<JsonFileObj> PopulateFromArchive(SQLiteDatabase database)
    {
        List<JsonFileObj> myObj = new List<JsonFileObj>();
        JsonFileObj helper, currArcObj;

        foreach (var row in database.Query("SELECT * FROM ArchiveData;"))
        {
            logger.Debug("Inside of foreach..");
            if (row is not null)
            {
                helper = new JsonFileObj();
                currArcObj = helper.ConvertToObj(row);
                myObj.Add(currArcObj);
            }
        }

        logger.Debug("Returning ArchObj");

        return myObj;
    }

    public string FetchImagePoster(JsonFileObj item)
    {
        // Check which config option for posters are selected
        logger.Debug($"HOSTING TYPE: {config.PHType}");
        switch (config.PHType)
        {
            case "Imgur":
                return UploadToImgur(item.PosterPath);
                break;
            case "JfHosting":
                return $"{config.Hostname}/Items/{item.ItemID}/Images/Primary";
                break;
            default:
                return "Something Went Wrong";
                break;
        }

        // return UploadToImgur(posterFilePath);
    }

    private string UploadToImgur(string posterFilePath)
    {
        WebClient wc = new();

        NameValueCollection values = new()
        {
            { "image", Convert.ToBase64String(File.ReadAllBytes(posterFilePath)) }
        };

        wc.Headers.Add("Authorization", "Client-ID " + config.ApiKey);

        try
        {
            byte[] response = wc.UploadValues("https://api.imgur.com/3/upload.xml", values);

            string res = System.Text.Encoding.Default.GetString(response);

            logger.Debug("Imgur Response: " + res);

            logger.Info("Imgur Uploaded! Link:");
            logger.Info(res.Split("<link>")[1].Split("</link>")[0]);

            return res.Split("<link>")[1].Split("</link>")[0];
        }
        catch (WebException e)
        {
            logger.Debug("WebClient Return STATUS: " + e.Status);
            logger.Debug(e.ToString().Split(")")[0].Split("(")[1]);
            try
            {
                return e.ToString().Split(")")[0].Split("(")[1];
            }
            catch (Exception ex)
            {
                logger.Error("Error caught while trying to parse webException error: " + ex);
                return "ERR";
            }
        }
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
}