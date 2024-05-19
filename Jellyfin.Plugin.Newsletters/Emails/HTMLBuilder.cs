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
    // private readonly string[] itemJsonKeys = 

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
        emailBody = config.Body;
        return emailBody;
    }

    public string TemplateReplace(string htmlObj, string replaceKey, object replaceValue, bool finalPass = false)
    {
        logger.Debug("Replacing {} params:\n " + htmlObj);
        if (replaceValue is null)
        {
            logger.Debug($"Replace string is null.. Nothing to replace");
            return htmlObj;
        }

        if (replaceKey == "{RunTime}" && (int)replaceValue == 0)
        {
            logger.Debug($"{replaceKey} == {replaceValue}");
            logger.Debug("Skipping replace..");
            return htmlObj;
        }

        logger.Debug($"Replace Value {replaceKey} with " + replaceValue);

        // Dictionary<string, object> html_params = new Dictionary<string, object>();
        // html_params.Add("{Date}", DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        // html_params.Add(replaceKey, replaceValue);

        htmlObj = htmlObj.Replace(replaceKey, replaceValue.ToString(), StringComparison.Ordinal);
        // logger.Debug("HERE\n " + htmlObj)

        // foreach (KeyValuePair<string, object> param in html_params)
        // {
        //     if (param.Value is not null)
        //     {
        //         htmlObj = htmlObj.Replace(param.Key, param.Value.ToString(), StringComparison.Ordinal);
        //         // logger.Debug("HERE\n " + htmlObj)
        //     }
        // }
        
        logger.Debug("New HTML OBJ: \n" + htmlObj);
        return htmlObj;
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
                    JsonFileObj item = jsonHelper.ConvertToObj(row);
                    // scan through all items and get all Season numbers and Episodes
                    // (string seasonInfo, string episodeInfo) = ParseSeriesInfo(obj, readDataFile);
                    if (completed.Contains(item.Title))
                    {
                        continue;
                    }

                    string seaEpsHtml = string.Empty;
                    if (item.Type == "Series")
                    {
                        // for series only
                        List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(item);
                        seaEpsHtml += GetSeasonEpisodeHTML(parsedInfoList);
                    }

                    var tmp_entry = config.Entry;
                    // logger.Debug("TESTING");
                    // logger.Debug(item.GetDict()["Filename"]);

                    foreach (KeyValuePair<string, object?> ele in item.GetReplaceDict())
                    {
                        if (ele.Value is not null)
                        {
                            tmp_entry = this.TemplateReplace(tmp_entry, ele.Key, ele.Value);
                        }
                    }

                    builtHTMLString += tmp_entry.Replace("{SeasonEpsInfo}", seaEpsHtml, StringComparison.Ordinal)
                                                .Replace("{ServerURL}", config.Hostname, StringComparison.Ordinal);
                    completed.Add(item.Title);
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
            // html += "<div id='SeasonEpisode' class='text' style='color: #FFFFFF;'>Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "</div>";
            html += "Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "<br>";
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
                logger.Debug($"tempEpsList Size: {tempEpsList.Count}");
                if (tempEpsList.Count != 0)
                {
                    logger.Debug("tempEpsList is populated");
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
        return body.Replace("{EntryData}", nlData, StringComparison.Ordinal);
    }

    public void CleanUp(string htmlBody)
    {
        // save newsletter to file
        logger.Info("Saving HTML file");
        WriteFile(write, newsletterHTMLFile, htmlBody);

        // append newsletter cycle data to Archive.txt
        CopyNewsletterDataToArchive();
    }

    private void CopyNewsletterDataToArchive()
    {
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