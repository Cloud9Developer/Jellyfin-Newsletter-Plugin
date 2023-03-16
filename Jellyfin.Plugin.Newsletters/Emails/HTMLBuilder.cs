#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162, SA1005 // remove SA1005 for cleanup
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

namespace Jellyfin.Plugin.Newsletters.Emails.HTMLBuilder;

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
    private SQLiteDatabase db;
    private JsonFileObj jsonHelper;

    // Non-readonly
    private static string append = "Append";
    private static string write = "Overwrite";
    // private List<string> fileList;

    public HtmlBuilder()
    {
        logger = new Logger();
        jsonHelper = new JsonFileObj();
        db = new SQLiteDatabase();
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
    }

    public string GetDefaultHTMLBody()
    {
        emailBody = "<html> <div> <table style='margin-left: auto; margin-right: auto;'> <tr> <td width='100%' height='100%' style='vertical-align: top; background-color: #000000;'> <table id='InsertHere' name='MainTable' style='margin-left: auto; margin-right: auto; border-spacing: 0 5px; padding-left: 2%; padding-right: 2%; padding-bottom: 1%;'> <tr style='text-align: center;'> <td colspan='2'> <span><h1 id='Title' style='color:#FFFFFF;'>Jellyfin Newsletter</h1><h3 id='Date' style='color:#FFFFFF;'>" + DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "</h3></span> </td> </tr> <!-- Fill this in from code --> REPLACEME <!-- Fill that in from code --> </table> </td> </tr> </table> </div> </html>";
        return emailBody;
    }

    public string BuildDataHtmlStringFromNewsletterData()
    {
        List<string> completed = new List<string>();
        string builtHTMLString = string.Empty;
        // pull data from CurrNewsletterData table

        try
        {
            db.CreateConnection();

            foreach (var row in db.Query("SELECT * FROM CurrNewsletterData;"))
            {
                if (row is not null)
                {
                    JsonFileObj obj = jsonHelper.ConvertToObj(row);
                    // scan through all items and get all Season numbers and Episodes
                    // (string seasonInfo, string episodeInfo) = ParseSeriesInfo(obj, readDataFile);
                    if (completed.Contains(obj.Title))
                    {
                        continue;
                    }

                    string seaEpsHtml = string.Empty;
                    List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(obj); // , readDataFile); readin archive in ParseSeriesInfo for nlData
                    seaEpsHtml += GetSeasonEpisodeHTML(parsedInfoList);

                    builtHTMLString += "<tr class='boxed' style='outline: thin solid #D3D3D3;'> <td class='lefttable' style='padding-right: 5%; padding-left: 2%; padding-top: 2%; padding-bottom: 2%;'> <img style='width: 200px; height: 300px;' src='" + obj.ImageURL + "'> </td> <td class='righttable' style='vertical-align: top; padding-left: 5%; padding-right: 2%; padding-top: 2%; padding-bottom: 2%;'> <p><div id='SeriesTitle' class='text' style='color: #FFFFFF; text-align: center;'><h3>" + obj.Title + "</h3></div>" + seaEpsHtml + " <hr> <div id='Description' class='text' style='color: #FFFFFF;'>" + obj.SeriesOverview + "</div> </p> </td> </tr>";
                    completed.Add(obj.Title);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error("An error has occured: " + e);
        }
        finally
        {
            db.CloseConnection();
        }

        return builtHTMLString;
    }

    private string GetSeasonEpisodeHTML(List<NlDetailsJson> list)
    {
        string html = string.Empty;
        foreach (NlDetailsJson obj in list)
        {
            logger.Debug("SNIPPET OBJ: " + JsonConvert.SerializeObject(obj));
            html += "<div id='SeasonEpisode' class='text' style='color: #FFFFFF;'>Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "</div>";
        }

        return html;
    }

    private List<NlDetailsJson> ParseSeriesInfo(JsonFileObj currObj)
    {
        List<NlDetailsJson> compiledList = new List<NlDetailsJson>();
        List<NlDetailsJson> finalList = new List<NlDetailsJson>();

        foreach (var row in db.Query("SELECT * FROM CurrNewsletterData WHERE Title='" + currObj.Title + "';"))
        {
            if (row is not null)
            {
                JsonFileObj helper = new JsonFileObj();
                JsonFileObj itemObj = helper.ConvertToObj(row);

                NlDetailsJson tempVar = new NlDetailsJson()
                {
                    Title = itemObj.Title,
                    Season = itemObj.Season,
                    Episode = itemObj.Episode
                };

                logger.Debug("tempVar.Season: " + tempVar.Season + " : tempVar.Episode: " + tempVar.Episode);
                compiledList.Add(tempVar);
            }
        }

        // Old File fetcher

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
                else if (tempEpsList.First() == tempEpsList.Last())
                {
                    currSeriesDetailsObj.EpisodeRange = tempEpsList.First().ToString(System.Globalization.CultureInfo.CurrentCulture);
                }
                else
                {
                    string epList = string.Empty;
                    int firstRangeEp, prevEp;
                    firstRangeEp = prevEp = -1;

                    bool IsNext(int prev, int curr)
                    {
                        logger.Debug("Checking Prev and Curr..");
                        logger.Debug($"prev: {prev} :: curr: {curr}");
                        logger.Debug(prev + 1);
                        if (curr == prev + 1)
                        {
                            return true;
                        }

                        return false;
                    }

                    string ProcessEpString(int firstRangeEp, int prevEp)
                    {
                        if (firstRangeEp == prevEp)
                        {
                            epList += firstRangeEp + ",";
                        }
                        else
                        {
                            epList += firstRangeEp + "-" + prevEp + ",";
                        }

                        return epList;
                    }

                    foreach (int ep in tempEpsList)
                    {
                        logger.Debug("-------------------");
                        logger.Debug($"FOREACH firstRangeEp :: prevEp :: ep = {firstRangeEp} :: {prevEp} :: {ep} ");
                        logger.Debug(ep == tempEpsList.Last());
                        // if first passthrough
                        if (firstRangeEp == -1)
                        {
                            logger.Debug("First pass of episode list");
                            firstRangeEp = prevEp = ep;
                            continue;
                        }

                        // If incremental
                        if (IsNext(prevEp, ep) && (ep != tempEpsList.Last()))
                        {
                            logger.Debug("Is Next and Isn't last");
                            prevEp = ep;
                            continue;
                        }
                        else if (IsNext(prevEp, ep) && (ep == tempEpsList.Last()))
                        {
                            logger.Debug("Is Next and Is last");
                            prevEp = ep;
                            ProcessEpString(firstRangeEp, prevEp);
                        }
                        else if (!IsNext(prevEp, ep) && (ep == tempEpsList.Last()))
                        {
                            logger.Debug("Isn't Next and Is last");
                            // process previous
                            ProcessEpString(firstRangeEp, prevEp);
                            // process last episode
                            epList += ep;
                            continue;
                        }
                        else
                        {
                            logger.Debug("Isn't Next and Isn't last");
                            ProcessEpString(firstRangeEp, prevEp);
                            firstRangeEp = prevEp = ep;
                        }
                    }

                    // better numbering here
                    logger.Debug($"epList: {epList}");
                    currSeriesDetailsObj.EpisodeRange = epList.TrimEnd(',');
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
        // save newsletter to file
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
        logger.Info("Appending NewsletterData for Current Newsletter Cycle to Archive Database..");

        // copy tables
        db.ExecuteSQL("INSERT INTO ArchiveData SELECT * FROM CurrNewsletterData;");
        db.ExecuteSQL("DELETE FROM CurrNewsletterData;");
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