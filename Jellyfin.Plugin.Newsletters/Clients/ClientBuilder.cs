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

namespace Jellyfin.Plugin.Newsletters.Clients.CLIENTBuilder;

public class ClientBuilder
{
    // Global Vars
    // Readonly
    protected readonly PluginConfiguration config;

    protected Logger logger;
    protected SQLiteDatabase db;
    protected JsonFileObj jsonHelper;

    public ClientBuilder()
    {
        logger = new Logger();
        jsonHelper = new JsonFileObj();
        db = new SQLiteDatabase();
        config = Plugin.Instance!.Configuration;
    }

    protected List<NlDetailsJson> ParseSeriesInfo(JsonFileObj currObj)
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
}

