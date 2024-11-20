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

namespace Jellyfin.Plugin.Newsletters.Scanner.NLImageHandler;

public class PosterImageHandler
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    private Logger logger;
    private SQLiteDatabase db;

    // Non-readonly
    private List<JsonFileObj> archiveSeriesList;
    // private List<string> fileList;

    public PosterImageHandler()
    {
        logger = new Logger();
        db = new SQLiteDatabase();
        config = Plugin.Instance!.Configuration;

        archiveSeriesList = new List<JsonFileObj>();
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
            case "Embedded":
                return "data:image/jpeg;base64, " + Convert.ToBase64String(File.ReadAllBytes(item.PosterPath));
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
}
