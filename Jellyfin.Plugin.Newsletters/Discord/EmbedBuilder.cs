#pragma warning disable 1591, SYSLIB0014, CA1002, CA2227, CS0162, SA1005, SA1300 // remove SA1005 for cleanup
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.Emails.HTMLBuilder;
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

namespace Jellyfin.Plugin.Newsletters.Discord.EMBEDBuilder;

public class EmbedBuilder
{
    // Global Vars
    // Readonly
    private readonly PluginConfiguration config;
    
    private Logger logger;
    private SQLiteDatabase db;
    private JsonFileObj jsonHelper;

    public EmbedBuilder()
    {
        logger = new Logger();
        jsonHelper = new JsonFileObj();
        db = new SQLiteDatabase();
        config = Plugin.Instance!.Configuration;
    }

    public List<Embed> BuildEmbedsFromNewsletterData()
    {
        List<string> completed = new List<string>();
        List<Embed> embeds = new List<Embed>();

        // Reusing components from HtmlBuilder
        // It'll be better if we create a common class for the reusable functions
        HtmlBuilder hb = new HtmlBuilder();

        try
        {
            db.CreateConnection();

            foreach (var row in db.Query("SELECT * FROM CurrNewsletterData;"))
            {
                if (row is not null)
                {
                    JsonFileObj item = jsonHelper.ConvertToObj(row);

                    if (completed.Contains(item.Title))
                    {
                        continue;
                    }

                    string seaEps = string.Empty;
                    if (item.Type == "Series")
                    {
                        // for series only
                        // List<NlDetailsJson> parsedInfoList = hb.ParseSeriesInfo(item);
                        List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(item);
                        seaEps += GetSeasonEpisode(parsedInfoList);
                    }

                    // string communityRating = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A";

                    var fieldsList = new List<EmbedField>
                    {
                        new EmbedField { name = "Rating", value = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A", inline = true },
                        new EmbedField { name = "PG rating", value = item.OfficialRating, inline = true },
                        new EmbedField { name = "Duration", value = item.RunTime.ToString(CultureInfo.InvariantCulture) + " min.", inline = true },
                    };
                    
                    if (!string.IsNullOrWhiteSpace(seaEps))
                    {
                        fieldsList.Add(new EmbedField { name = "Season", value = seaEps, inline = false });
                    }

                    var embed = new Embed
                    {
                        title = item.Title,
                        color = 16776960, // yellow
                        description = item.SeriesOverview,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = fieldsList
                    };
                    completed.Add(item.Title);
                    embeds.Add(embed);
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

        return embeds;
    }

    private string GetSeasonEpisode(List<NlDetailsJson> list)
    {
        string seaEps = string.Empty;
        foreach (NlDetailsJson obj in list)
        {
            logger.Debug("SNIPPET OBJ: " + JsonConvert.SerializeObject(obj));
            seaEps += "Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "\n";
        }

        return seaEps;
    }

    public List<NlDetailsJson> ParseSeriesInfo(JsonFileObj currObj)
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

    private void CopyNewsletterDataToArchive()
    {
        logger.Info("Appending NewsletterData for Current Newsletter Cycle to Archive Database..");

        // copy tables
        db.ExecuteSQL("INSERT INTO ArchiveData SELECT * FROM CurrNewsletterData;");
        db.ExecuteSQL("DELETE FROM CurrNewsletterData;");
    }
}

public class EmbedField
{
    public string? name { get; set; }

    public string? value { get; set; }

    public bool inline { get; set; }
}

public class Embed
{
    public string? title { get; set; }

    public string? url { get; set; }

    public int color { get; set; }

    public string? timestamp { get; set; }

    public string? description { get; set; }

    public List<EmbedField>? fields { get; set; }
}

public class DiscordPayload
{
    public string? username { get; set; }

    public List<Embed>? embeds { get; set; }
}