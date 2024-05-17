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
        // Body = string.Empty;
        // {EntryData}
        Body = @"<html>
    <div>
        <table style='margin-left: auto; margin-right: auto;'>
            <tr> 
                <td width='100%' height='100%' style='vertical-align: top; background-color: #000000;'> 
                    <table id='InsertHere' name='MainTable' style='margin-left: auto; margin-right: auto; border-spacing: 0 5px; padding-left: 2%; padding-right: 2%; padding-bottom: 1%;'> 
                        <tr style='text-align: center;'> 
                            <td colspan='2'> 
                                <span>
                                    <h1 id='Title' style='color:#FFFFFF;'>Jellyfin Newsletter</h1>
                                    <h3 id='Date' style='color:#FFFFFF;'>2023-03-14</h3>
                                </span> 
                            </td> 
                        </tr> 
                        <!-- Fill this in from code --> 
                        {EntryData}
                        <!-- Fill that in from code --> 
                    </table> 
                </td> 
            </tr> 
        </table> 
    </div> 
</html>";

        // Entry = string.Empty;
        // {ImageURL}
        // {Title}
        // {SeasonEpsInfo}
        // {SeriesOverview}
        Entry = @"<tr class='boxed' style='outline: thin solid #D3D3D3;'> 
    <td class='lefttable' style='padding-right: 5%; padding-left: 2%; padding-top: 2%; padding-bottom: 2%;'> 
        <img style='width: 200px; height: 300px;' src='{ImageURL}'> 
    </td> 
    <td class='righttable' style='vertical-align: top; padding-left: 5%; padding-right: 2%; padding-top: 2%; padding-bottom: 2%;'> 
        <p>
            <div id='SeriesTitle' class='text' style='color: #FFFFFF; text-align: center;'>
                <h3>
                    {Title} 
                </h3>
            </div>
            <div class='text' style='color: #FFFFFF;'>
                {SeasonEpsInfo}
            </div>
            <hr> 
                <div id='Description' class='text' style='color: #FFFFFF;'>
                {SeriesOverview}
                </div> 
            </hr>
        </p> 
    </td> 
</tr>";

        // default Scraper config
        ApiKey = string.Empty;

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
        PHType = "Imgur";
        Hostname = string.Empty;
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

    // Scraper Config

    /// <summary>
    /// Gets or sets a value indicating hosting type.
    /// </summary>
    public string PHType { get; set; }

    /// <summary>
    /// Gets or sets a value for JF hostname accessible outside of network.
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string ApiKey { get; set; }

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
