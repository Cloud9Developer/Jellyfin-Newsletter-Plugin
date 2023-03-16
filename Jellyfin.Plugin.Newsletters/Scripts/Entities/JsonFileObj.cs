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
        // Season = string.Empty;
        Season = 0;
        Episode = 0;
        // Episode = string.Empty;
        SeriesOverview = string.Empty;
        ImageURL = string.Empty;
        ItemID = string.Empty;
        PosterPath = string.Empty;
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
            PosterPath = row[7].ToString()
        };

        return obj;
    }
}