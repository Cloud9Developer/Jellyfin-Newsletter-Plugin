#pragma warning disable 1591, SYSLIB0014, CA1002, CA2227, CS0162, SA1005, SA1300 // remove SA1005 for cleanup
using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Newsletters.Clients.CLIENTBuilder;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using MediaBrowser.Controller.Library;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Newsletters.Clients.Discord.EMBEDBuilder;

public class EmbedBuilder : ClientBuilder
{
    public List<Embed> BuildEmbedsFromNewsletterData(string serverId)
    {
        List<string> completed = new List<string>();
        List<Embed> embeds = new List<Embed>();

        try
        {
            Db.CreateConnection();

            foreach (var row in Db.Query("SELECT * FROM CurrNewsletterData;"))
            {
                if (row is not null)
                {
                    JsonFileObj item = JsonHelper.ConvertToObj(row);

                    if (completed.Contains(item.Title))
                    {
                        continue;
                    }

                    int embedColor = Convert.ToInt32(Config.DiscordMoviesEmbedColor.Replace("#", string.Empty, StringComparison.Ordinal), 16);
                    string seaEps = string.Empty;
                    if (item.Type == "Series")
                    {
                        // for series only
                        List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(item);
                        seaEps += GetSeasonEpisode(parsedInfoList);
                        embedColor = Convert.ToInt32(Config.DiscordMoviesEmbedColor.Replace("#", string.Empty, StringComparison.Ordinal), 16);
                    }

                    // string communityRating = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A";

                    var fieldsList = new List<EmbedField>();

                    // Check if DiscordPGRatingEnabled is true
                    if (Config.DiscordPGRatingEnabled)
                    {
                        fieldsList.Add(new EmbedField
                        {
                            name = "PG rating",
                            value = item.OfficialRating,
                            inline = true
                        });
                    }

                    // Check if DiscordRatingEnabled is true
                    if (Config.DiscordRatingEnabled)
                    {
                        fieldsList.Add(new EmbedField
                        {
                            name = "Rating",
                            value = item.CommunityRating.HasValue ? item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture) : "N/A",
                            inline = true
                        });
                    }

                    // Check if DiscordDurationEnabled is true
                    if (Config.DiscordDurationEnabled)
                    {
                        fieldsList.Add(new EmbedField
                        {
                            name = "Duration",
                            value = item.RunTime.ToString(CultureInfo.InvariantCulture) + " min",
                            inline = true
                        });
                    }

                    // Check if DiscordEpisodesEnabled is true and seaEps is not null/empty
                    if (Config.DiscordEpisodesEnabled && !string.IsNullOrWhiteSpace(seaEps))
                    {
                        fieldsList.Add(new EmbedField
                        {
                            name = "Episodes",
                            value = seaEps,
                            inline = false
                        });
                    }

                    var embed = new Embed
                    {
                        title = item.Title,
                        url = $"{Config.Hostname}/web/index.html#/details?id={item.ItemID}&serverId={serverId}",
                        color = embedColor,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = fieldsList,
                    };

                    // Check if DiscordDescriptionEnabled is true
                    if (Config.DiscordDescriptionEnabled)
                    {
                        embed.description = item.SeriesOverview;
                    }

                    // Check if DiscordThumbnailEnabled is true
                    if (Config.DiscordThumbnailEnabled && IsValidUrl(item.ImageURL))
                    {
                        embed.thumbnail = new Thumbnail
                        {
                            url = item.ImageURL
                        };
                    }

                    completed.Add(item.Title);
                    embeds.Add(embed);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error("An error has occured: " + e);
        }
        finally
        {
            Db.CloseConnection();
        }

        return embeds;
    }

    public List<Embed> BuildEmbedForTest()
    {
        List<Embed> embeds = new List<Embed>();

        try
        {
            // Populating embed with reference to a Series, as it'll will cover all the cases
            int embedColor = Convert.ToInt32(Config.DiscordSeriesEmbedColor.Replace("#", string.Empty, StringComparison.Ordinal), 16);
            string seaEps = "Season: 1 - Eps. 1 - 10\nSeason: 2 - Eps. 1 - 10\nSeason: 3 - Eps. 1 - 10 (Test)";

            var fieldsList = new List<EmbedField>();

            // Check if DiscordPGRatingEnabled is true
            if (Config.DiscordPGRatingEnabled)
            {
                fieldsList.Add(new EmbedField
                {
                    name = "PG rating",
                    value = "TV-14 (Test)",
                    inline = true
                });
            }

            // Check if DiscordRatingEnabled is true
            if (Config.DiscordRatingEnabled)
            {
                fieldsList.Add(new EmbedField
                {
                    name = "Rating",
                    value = "8.4 (Test)",
                    inline = true
                });
            }

            // Check if DiscordDurationEnabled is true
            if (Config.DiscordDurationEnabled)
            {
                fieldsList.Add(new EmbedField
                {
                    name = "Duration",
                    value = "45 min (Test)",
                    inline = true
                });
            }

            // Check if DiscordEpisodesEnabled is true and seaEps is not null/empty
            if (Config.DiscordEpisodesEnabled && !string.IsNullOrWhiteSpace(seaEps))
            {
                fieldsList.Add(new EmbedField
                {
                    name = "Episodes",
                    value = seaEps,
                    inline = false
                });
            }

            var embed = new Embed
            {
                title = "Newsletter Test Title",
                url = Config.Hostname,
                color = embedColor,
                timestamp = DateTime.UtcNow.ToString("o"),
                fields = fieldsList,
            };

            // Check if DiscordDescriptionEnabled is true
            if (Config.DiscordDescriptionEnabled)
            {
                embed.description = "This is a test embed from Newsletter plugin";
            }

            // Check if DiscordThumbnailEnabled is true
            if (Config.DiscordThumbnailEnabled)
            {
                embed.thumbnail = new Thumbnail
                {
                    url = "https://raw.githubusercontent.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/refs/heads/master/logo.png"
                };
            }

            embeds.Add(embed);
        }
        catch (Exception e)
        {
            Logger.Error("An error has occured: " + e);
        }

        return embeds;
    }

    private string GetSeasonEpisode(List<NlDetailsJson> list)
    {
        string seaEps = string.Empty;
        foreach (NlDetailsJson obj in list)
        {
            Logger.Debug("SNIPPET OBJ: " + JsonConvert.SerializeObject(obj));
            seaEps += "Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "\n";
        }

        return seaEps;
    }

    private bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
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
