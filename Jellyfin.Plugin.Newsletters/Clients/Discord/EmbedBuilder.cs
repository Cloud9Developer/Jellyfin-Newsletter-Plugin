#pragma warning disable 1591, SYSLIB0014, CA1002, CA2227, CS0162, SA1005, SA1300 // remove SA1005 for cleanup
using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Newsletters.Clients.CLIENTBuilder;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Newsletters.Clients.Discord.EMBEDBuilder;

public class EmbedBuilder : ClientBuilder
{

    public List<Embed> BuildEmbedsFromNewsletterData(string ServerId)
    {
        List<string> completed = new List<string>();
        List<Embed> embeds = new List<Embed>();

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
                        List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(item);
                        seaEps += GetSeasonEpisode(parsedInfoList);
                    }

                    // string communityRating = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A";

                    var fieldsList = new List<EmbedField>
                    {
                        new EmbedField { name = "Rating", value = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A", inline = true },
                        new EmbedField { name = "PG rating", value = item.OfficialRating, inline = true },
                        new EmbedField { name = "Duration", value = item.RunTime.ToString(CultureInfo.InvariantCulture) + " min", inline = true },
                    };
                    
                    if (!string.IsNullOrWhiteSpace(seaEps))
                    {
                        fieldsList.Add(new EmbedField { name = "Episodes", value = seaEps, inline = false });
                    }

                    var embed = new Embed
                    {
                        title = item.Title,
                        url = $"{config.Hostname}/web/index.html#/details?id={item.ItemID}&serverId={ServerId}",
                        color = 16776960,
                        description = item.SeriesOverview,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = fieldsList,
                        thumbnail = new Thumbnail
                        {
                            url = item.ImageURL
                        }
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

    public Thumbnail? thumbnail { get; set; }
}

public class DiscordPayload
{
    public string? username { get; set; }

    public List<Embed>? embeds { get; set; }
}

public class Thumbnail
{
    public string? url { get; set; }
}
