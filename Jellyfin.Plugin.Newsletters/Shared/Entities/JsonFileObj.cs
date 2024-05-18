#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.Newsletters.LOGGER;
using SQLitePCL;
using SQLitePCL.pretty;

namespace Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;

public class JsonFileObj
{
    private Logger? logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileObj"/> class.
    /// </summary>
    public JsonFileObj()
    {
        Filename = string.Empty;
        Title = string.Empty;
        Season = 0;
        Episode = 0;
        SeriesOverview = string.Empty;
        ImageURL = string.Empty;
        ItemID = string.Empty;
        PosterPath = string.Empty;
        Type = string.Empty;
        PremiereYear = string.Empty;
        RunTime = 0;
        OfficialRating = string.Empty;
        CommunityRating = 0.0f;
    }

    public string Filename { get; set; }

    public string Title { get; set; }

    // public string Season { get; set; }

    public int Season { get; set; }

    public int Episode { get; set; }

    // public string Episode { get; set; }

    public string SeriesOverview { get; set; }

    public string ImageURL { get; set; }

    public string ItemID { get; set; }

    public string PosterPath { get; set; }

    public string Type { get; set; }

    public string PremiereYear { get; set; }

    public int RunTime { get; set; }

    public string OfficialRating { get; set; }

    public float? CommunityRating { get; set; }

    public JsonFileObj ConvertToObj(IReadOnlyList<ResultSetValue> row)
    {
        // Filename = string.Empty; 0
        // Title = string.Empty; 1
        // Season = 0; 2
        // Episode = 0; 3
        // SeriesOverview = string.Empty; 4
        // ImageURL = string.Empty; 5
        // ItemID = string.Empty; 6
        // PosterPath = string.Empty; 7

        logger = new Logger();
        JsonFileObj obj = new JsonFileObj()
        {
            Filename = row[0].ToString(),
            Title = row[1].ToString(),
            Season = int.Parse(row[2].ToString(), CultureInfo.CurrentCulture),
            Episode = int.Parse(row[3].ToString(), CultureInfo.CurrentCulture),
            SeriesOverview = row[4].ToString(),
            ImageURL = row[5].ToString(),
            ItemID = row[6].ToString(),
            PosterPath = row[7].ToString(),
            Type = row[8].ToString(),
            PremiereYear = row[9].ToString(),
            RunTime = string.IsNullOrEmpty(row[10].ToString()) ? 0 : int.Parse(row[10].ToString(), CultureInfo.CurrentCulture),
            OfficialRating = row[11].ToString(),
            CommunityRating = string.IsNullOrEmpty(row[12].ToString()) ? 0.0f : float.Parse(row[12].ToString(), CultureInfo.CurrentCulture)
        };

        return obj;
    }

    public Dictionary<string, object?> GetReplaceDict()
    {
        Dictionary<string, object?> item_dict = new Dictionary<string, object?>();
        item_dict.Add("{Filename}", this.Filename);
        item_dict.Add("{Title}", this.Title);
        item_dict.Add("{Season}", this.Season);
        item_dict.Add("{Episode}", this.Episode);
        item_dict.Add("{SeriesOverview}", this.SeriesOverview);
        item_dict.Add("{ImageURL}", this.ImageURL);
        item_dict.Add("{ItemID}", this.ItemID);
        item_dict.Add("{PosterPath}", this.PosterPath);
        item_dict.Add("{Type}", this.Type);
        item_dict.Add("{PremiereYear}", this.PremiereYear);
        item_dict.Add("{RunTime}", this.RunTime);
        item_dict.Add("{OfficialRating}", this.OfficialRating);
        item_dict!.Add("{CommunityRating}", this.CommunityRating);

        return item_dict;        
    }
}