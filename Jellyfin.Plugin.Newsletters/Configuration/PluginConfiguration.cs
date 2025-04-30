using System;
using System.IO;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Newsletters.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Console.WriteLine("[NLP] :: Newsletter Plugin Starting..");

        // set default options here
        DebugMode = false;

        // default Server Details
        SMTPServer = "smtp.gmail.com";
        SMTPPort = 587;
        SMTPUser = string.Empty;
        SMTPPass = string.Empty;

        // default Email Details
        ToAddr = string.Empty;
        FromAddr = "JellyfinNewsletter@donotreply.com";
        Subject = "Jellyfin Newsletter";

        // default Discord Webhook Details
        DiscordWebhookURL = string.Empty;
        DiscordWebhookName = "Jellyfin Newsletter";

        // Attempt Dynamic set of Body and Entry HTML, set empty if failure occurs
        Body = string.Empty;
        Entry = string.Empty;
        
        try
        {
            string[] dirs = Directory.GetDirectories(@".", "config/plugins/Newsletters_*.*.*.*", SearchOption.AllDirectories);
            string pluginDir = string.Empty;
            if (dirs.Length > 1)
            {
                Console.WriteLine($"[NLP] :: Found {dirs.Length} matches for plugin directory...");
            }
            else
            {
                foreach (string dir in dirs)
                {
                    Console.WriteLine("[NLP] :: Plugin Directory is: {0}", dir);
                    pluginDir = dir;
                    break;
                }
            }
            
            try
            {
                Body = File.ReadAllText($"{pluginDir}/Templates/template_modern_body.html");
                Console.WriteLine("[NLP] :: Body HTML set from Template file!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[NLP] :: Failed to set default Body HTML from Template file");
                Console.WriteLine(ex);
            }

            try
            {
                Entry = File.ReadAllText($"{pluginDir}/Templates/template_modern_entry.html");
                Console.WriteLine("[NLP] :: Entry HTML set from Template file!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[NLP] :: Failed to set default Entry HTML from Template file");
                Console.WriteLine(ex);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[NLP] :: [ERR] Failed to locate/set html body from template file..");
            Console.WriteLine(e);
        }

        // default Scraper config
        // Deprecating imgur support
        // ApiKey = string.Empty;

        // System Paths
        DataPath = string.Empty;
        TempDirectory = string.Empty;
        PluginsPath = string.Empty;
        ProgramDataPath = string.Empty;
        SystemConfigurationFilePath = string.Empty;
        ProgramSystemPath = string.Empty;
        LogDirectoryPath = string.Empty;

        // default newsletter paths
        NewsletterFileName = string.Empty;
        NewsletterDir = string.Empty;

        // default libraries
        MoviesEnabled = true;
        SeriesEnabled = true;

        // poster hosting
        // Deprecating imgur support
        // PHType = "Imgur";
        Hostname = string.Empty;

        // default discord fields
        DiscordDescriptionEnabled = true;
        DiscordThumbnailEnabled = true;
        DiscordRatingEnabled = true;
        DiscordPGRatingEnabled = true;
        DiscordDurationEnabled = true;
        DiscordEpisodesEnabled = true;

        // default discord embed color
        DiscordSeriesEmbedColor = "#00ffff";
        DiscordMoviesEmbedColor = "#00ff99";
    }

    /// <summary>
    /// Gets or sets a value indicating whether debug mode is enabled..
    /// </summary>
    public bool DebugMode { get; set; }

    // Server Details

    /// <summary>
    /// Gets or sets a value indicating whether some true or false setting is enabled..
    /// </summary>
    public string SMTPServer { get; set; }

    /// <summary>
    /// Gets or sets an integer setting.
    /// </summary>
    public int SMTPPort { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string SMTPUser { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string SMTPPass { get; set; }

    // -----------------------------------

    // Email Details

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string ToAddr { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string FromAddr { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string Body { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string Entry { get; set; }

    // -----------------------------------

    // Discord Webhook Details

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string DiscordWebhookURL { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string DiscordWebhookName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether description in discord embed should be visible.
    /// </summary>
    public bool DiscordDescriptionEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether thumbnail in discord embed should be visible.
    /// </summary>
    public bool DiscordThumbnailEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rating in discord embed should be visible.
    /// </summary>
    public bool DiscordRatingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether PG rating in discord embed should be visible.
    /// </summary>
    public bool DiscordPGRatingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether duration in discord embed should be visible.
    /// </summary>
    public bool DiscordDurationEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether episodes list in discord embed should be visible.
    /// </summary>
    public bool DiscordEpisodesEnabled { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string DiscordMoviesEmbedColor { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string DiscordSeriesEmbedColor { get; set; }

    // -----------------------------------

    // Scraper Config

    // / <summary>
    // / Gets or sets a value indicating hosting type.
    // / </summary>
    // Deprecating imgur support
    // public string PHType { get; set; }

    /// <summary>
    /// Gets or sets a value for JF hostname accessible outside of network.
    /// </summary>
    public string Hostname { get; set; }

    // / <summary>
    // / Gets or sets a string setting.
    // / </summary>
    // Deprecating imgur support
    // public string ApiKey { get; set; }

    // -----------------------------------

    // System Paths

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string PluginsPath { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string TempDirectory { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string DataPath { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string ProgramDataPath { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string SystemConfigurationFilePath { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string ProgramSystemPath { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string LogDirectoryPath { get; set; }

    // -----------------------------------

    // Newsletter Paths

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string NewsletterFileName { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string NewsletterDir { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Series should be scanned.
    /// </summary>
    public bool SeriesEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Movies should be scanned.
    /// </summary>
    public bool MoviesEnabled { get; set; }
}
