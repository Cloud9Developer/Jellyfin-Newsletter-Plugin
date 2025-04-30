#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
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
    private static readonly object RateLimitLock = new object();
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(25);
    private static DateTime lastRequestTime = DateTime.MinValue;

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
        WebClient wc = new();
        string apiKey = "d63d13c187e20a4d436a9fd842e7e39c";

        try
        {
            foreach (var kvp in item.ExternalIds)
            {
                var externalIdName = kvp.Key;
                var externalIdValue = kvp.Value;
                string url = string.Empty;

                logger.Debug($"Trying to fetch TMDB poster path using {externalIdName} => {externalIdValue}");

                if (externalIdName == "tmdb")
                {
                    if (item.Type == "Series")
                    {
                        url = $"https://api.themoviedb.org/3/tv/{externalIdValue}?api_key={apiKey}";
                    }
                    else if (item.Type == "Movie")
                    {
                        url = $"https://api.themoviedb.org/3/movie/{externalIdValue}?api_key={apiKey}";
                    }
                }
                else
                {
                    url = $"https://api.themoviedb.org/3/find/{externalIdValue}?external_source={externalIdName}&api_key={apiKey}";
                }

                // TMDB api has rate-limiting which is 50 requests/second (https://developer.themoviedb.org/docs/rate-limiting)
                // This is a very poor attempt for now, we can add a better rate limit logic in future :)
                // Rate-limiting logic
                lock (RateLimitLock)
                {
                    var now = DateTime.UtcNow;
                    var timeSinceLast = now - lastRequestTime;
                    if (timeSinceLast < MinInterval)
                    {
                        logger.Debug($"Sleeping for {MinInterval - timeSinceLast}");
                        Thread.Sleep(MinInterval - timeSinceLast);
                    }

                    lastRequestTime = DateTime.UtcNow;
                }

                string response = wc.DownloadString(url);
                logger.Debug("TMDB Response: " + response);

                JObject json = JObject.Parse(response);

                JToken? posterPathToken = null;

                if (externalIdName == "tmdb")
                {
                    posterPathToken = json["poster_path"];
                }
                else
                {
                    if (item.Type == "Series")
                    {
                        posterPathToken = json["tv_results"]?.FirstOrDefault()?["poster_path"];
                    }
                    else if (item.Type == "Movie")
                    {
                        posterPathToken = json["movie_results"]?.FirstOrDefault()?["poster_path"];
                    }
                }

                if (posterPathToken != null)
                {
                    string posterPath = posterPathToken.ToString();
                    logger.Debug("TMDB Poster Path: " + posterPath);
                    return "https://image.tmdb.org/t/p/original" + posterPath;
                }
                else
                {
                    logger.Debug($"TMDB Poster path not found for {externalIdName} => {externalIdValue}");
                    logger.Debug("Trying the next...");
                }
            }

            return string.Empty;
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

    // private string UploadToImgur(string posterFilePath)
    // {
    //     WebClient wc = new();

    //     NameValueCollection values = new()
    //     {
    //         { "image", Convert.ToBase64String(File.ReadAllBytes(posterFilePath)) }
    //     };

    //     wc.Headers.Add("Authorization", "Client-ID " + config.ApiKey);

    //     try
    //     {
    //         byte[] response = wc.UploadValues("https://api.imgur.com/3/upload.xml", values);

    //         string res = System.Text.Encoding.Default.GetString(response);

    //         logger.Debug("Imgur Response: " + res);

    //         logger.Info("Imgur Uploaded! Link:");
    //         logger.Info(res.Split("<link>")[1].Split("</link>")[0]);

    //         return res.Split("<link>")[1].Split("</link>")[0];
    //     }
    //     catch (WebException e)
    //     {
    //         logger.Debug("WebClient Return STATUS: " + e.Status);
    //         logger.Debug(e.ToString().Split(")")[0].Split("(")[1]);
    //         try
    //         {
    //             return e.ToString().Split(")")[0].Split("(")[1];
    //         }
    //         catch (Exception ex)
    //         {
    //             logger.Error("Error caught while trying to parse webException error: " + ex);
    //             return "ERR";
    //         }
    //     }
    // }
}