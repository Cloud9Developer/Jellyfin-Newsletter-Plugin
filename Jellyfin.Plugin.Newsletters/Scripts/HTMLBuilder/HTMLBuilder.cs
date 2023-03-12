#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
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
    private readonly string newsletterHTMLFile;
    private readonly string newsletterDataFile;

    private string emailBody;
    private Logger logger;

    // Non-readonly
    private static string append = "Append";
    private static string write = "Overwrite";
    // private List<string> fileList;

    public HtmlBuilder()
    {
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
        emailBody = config.Body;

        newslettersDir = config.NewsletterDir; // newsletterdir
        Directory.CreateDirectory(newslettersDir);

        // if no newsletter filename is saved or the file doesn't exist
        if (config.NewsletterFileName.Length == 0 || File.Exists(newslettersDir + config.NewsletterFileName))
        {
            // use date to create filename
            string currDate = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            newsletterHTMLFile = newslettersDir + currDate + "_Newsletter.html";
        }
        else
        {
            newsletterHTMLFile = newslettersDir + config.NewsletterFileName;
        }

        logger.Info("Newsletter will be saved to: " + newsletterHTMLFile);

        // WriteFile(write, "/ssl/htmlbuilder.log", newslettersDir); // testing
    }

    public string GetDefaultHTMLBody()
    {
        emailBody = "<html> <div> <table style='margin-left: auto; margin-right: auto;'> <tr> <td width='100%' height='100%' style='vertical-align: top; background-color: #000000;'> <table id='InsertHere' name='MainTable' style='margin-left: auto; margin-right: auto; border-spacing: 0 5px; padding-left: 2%; padding-right: 2%; padding-bottom: 1%;'> <tr style='text-align: center;'> <td colspan='2'> <span><h1 id='Title' style='color:#FFFFFF;'>Jellyfin Newsletter</h1></span> </td> </tr> <!-- Fill this in from code --> REPLACEME <!-- Fill that in from code --> </table> </td> </tr> </table> </div> </html>";
        return emailBody;
    }

    public string BuildDataHtmlStringFromNewsletterData()
    {
        string builtHTMLString = string.Empty;
        List<string> completed = new List<string>();
        StreamReader sr = new StreamReader(newsletterDataFile);
        string readDataFile = sr.ReadToEnd();
        // WriteFile(write, "/ssl/mystreamreader.txt", readScrapeFile);
        foreach (string? item in readDataFile.Split(";;"))
        {
            JsonFileObj? obj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
            if (obj is not null)
            {
                // scan through all items and get all Season numbers and Episodes
                // (string seasonInfo, string episodeInfo) = ParseSeriesInfo(obj, readDataFile);
                if (completed.Contains(obj.Title))
                {
                    continue;
                }

                string seaEpsHtml = string.Empty;
                List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(obj, readDataFile);
                seaEpsHtml += GetSeasonEpisodeHTML(parsedInfoList);

                builtHTMLString += "<tr class='boxed' style='outline: thin solid #D3D3D3;'> <td class='lefttable' style='padding-right: 5%; padding-left: 2%; padding-top: 2%; padding-bottom: 2%;'> <img style='width: 200px; height: 300px;' src='" + obj.ImageURL + "'> </td> <td class='righttable' style='vertical-align: top; padding-left: 5%; padding-right: 2%; padding-top: 2%; padding-bottom: 2%;'> <p><div id='SeriesTitle' class='text' style='color: #FFFFFF; text-align: center;'><h3>" + obj.Title + "</h3></div>" + seaEpsHtml + " <hr> <div id='Description' class='text' style='color: #FFFFFF;'>" + "Descriptions not yet available with the Jellyfin Newsletter Plugin..." + "</div> </p> </td> </tr>";
                completed.Add(obj.Title);
            }
        }

        sr.Close();

        return builtHTMLString;
    }

    private string GetSeasonEpisodeHTML(List<NlDetailsJson> list)
    {
        string html = string.Empty;
        foreach (NlDetailsJson obj in list)
        {
            logger.Debug("SNIPPET OBJ: " + JsonConvert.SerializeObject(obj));
            // WriteFile(append, "/ssl/getseasonepshtml", obj.Season + " : " + obj.EpisodeRange);
            logger.Debug("Generate HTML snippet: Season-" + obj.Season + " EpR-" + obj.EpisodeRange + " Ep-" + obj.Episode);
            html += "<div id='SeasonEpisode' class='text' style='color: #FFFFFF;'>Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "</div>";
        }

        return html;
    }

    private List<NlDetailsJson> ParseSeriesInfo(JsonFileObj currObj, string nlData)
    {
        List<NlDetailsJson> compiledList = new List<NlDetailsJson>();
        List<NlDetailsJson> finalList = new List<NlDetailsJson>();

        foreach (string? item in nlData.Split(";;"))
        {
            JsonFileObj? itemObj = JsonConvert.DeserializeObject<JsonFileObj?>(item);
            if (itemObj is not null)
            {
                if (itemObj.Title == currObj.Title)
                {
                    NlDetailsJson tempVar = new NlDetailsJson();
                    tempVar.Season = itemObj.Season;
                    tempVar.Episode = itemObj.Episode;
                    logger.Debug("tempVar.Season: " + tempVar.Season + " : tempVar.Episode: " + tempVar.Episode);
                    compiledList.Add(tempVar);
                }
            }
        }

        List<int> tempEpsList = new List<int>();
        NlDetailsJson currSeriesDetailsObj = new NlDetailsJson();

        int currSeason = -1;
        bool newSeason = true;
        int list_len = compiledList.Count;
        int count = 1;
        foreach (NlDetailsJson item in SortListBySeason(SortListByEpisode(compiledList)))
        {
            logger.Debug("After Sort in foreach: Season::" + item.Season + "; Episode::" + item.Episode);
            logger.Debug("Count/list_len: " + count + "/" + list_len);

            NlDetailsJson CopyJsonFromExisting(NlDetailsJson obj)
            {
                NlDetailsJson newJson = new NlDetailsJson();
                newJson.Season = obj.Season;
                newJson.EpisodeRange = obj.EpisodeRange;
                return newJson;
            }

            void AddNewSeason()
            {
                // logger.Debug("AddNewSeason()");
                currSeriesDetailsObj.Season = currSeason = item.Season;
                newSeason = false;
                tempEpsList.Add(item.Episode);
            }

            void AddCurrentSeason()
            {
                // logger.Debug("AddCurrentSeason()");
                logger.Debug("Seasons Match " + currSeason + "::" + item.Season);
                tempEpsList.Add(item.Episode);
            }

            void EndOfSeason()
            {
                // process season, them increment
                logger.Debug("EndOfSeason()");
                tempEpsList.Sort();
                if (IsIncremental(tempEpsList))
                {
                    currSeriesDetailsObj.EpisodeRange = tempEpsList.First() + " - " + tempEpsList.Last();
                }
                else
                {
                    currSeriesDetailsObj.EpisodeRange = string.Join(", ", tempEpsList);
                }

                logger.Debug("Adding to finalListObj: " + JsonConvert.SerializeObject(currSeriesDetailsObj));
                // finalList.Add(currSeriesDetailsObj);
                finalList.Add(CopyJsonFromExisting(currSeriesDetailsObj));

                // increment season
                currSeriesDetailsObj.Season = currSeason = item.Season;
                currSeriesDetailsObj.EpisodeRange = string.Empty;

                // currSeason = item.Season;
                tempEpsList.Clear();
                newSeason = true;
            }

            logger.Debug("CurrItem Season/Episode number: " + item.Season + "/" + item.Episode);
            if (newSeason)
            {
                AddNewSeason();
            }
            else if (currSeason == item.Season) // && (count < list_len))
            {
                AddCurrentSeason();
            }
            else if (count < list_len)
            {
                EndOfSeason();
                AddNewSeason();
            }
            else if (count == list_len)
            {
                EndOfSeason();
            }
            else
            {
                EndOfSeason();
            }

            if (count == list_len)
            {
                EndOfSeason();
            }

            count++;
        }

        logger.Debug("FinalList Length: " + finalList.Count);

        foreach (NlDetailsJson item in finalList)
        {
            logger.Debug("FinalListObjs: " + JsonConvert.SerializeObject(item));
        }

        return finalList;
    }

    private bool IsIncremental(List<int> values)
    {
        return values.Skip(1).Select((v, i) => v == (values[i] + 1)).All(v => v);
    }

    private List<NlDetailsJson> SortListBySeason(List<NlDetailsJson> list)
    {
        return list.OrderBy(x => x.Season).ToList();
    }

    private List<NlDetailsJson> SortListByEpisode(List<NlDetailsJson> list)
    {
        return list.OrderBy(x => x.Episode).ToList();
    }

    public string ReplaceBodyWithBuiltString(string body, string nlData)
    {
        return body.Replace("REPLACEME", nlData, StringComparison.Ordinal);
    }

    public void CleanUp(string htmlBody)
    {
        // create html from body of email
        logger.Info("Saving HTML file");
        WriteFile(write, newsletterHTMLFile, htmlBody);

        // append newsletter cycle data to Archive.txt
        CopyNewsletterDataToArchive();

        // remove newsletter cycle data
        File.Delete(newsletterDataFile);
    }

    private void CopyNewsletterDataToArchive()
    {
        string archiveFile = config.MyDataDir + config.ArchiveFileName;
        logger.Info("Appending NewsletterData for Current Newsletter Cycle to Archive file: " + archiveFile);

        Stream input = File.OpenRead(newsletterDataFile);
        Stream output = new FileStream(archiveFile, FileMode.Append, FileAccess.Write, FileShare.None);
        input.CopyTo(output);
        // File.Delete(currRunList);
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

public class NlDetailsJson
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NlDetailsJson"/> class.
    /// </summary>
    public NlDetailsJson()
    {
        Season = 0;
        Episode = 0;
        EpisodeRange = string.Empty;
    }

    public int Season { get; set; }

    public int Episode { get; set; }

    public string EpisodeRange { get; set; }
}