using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Template.Configuration;

/// <summary>
/// The configuration options.
/// </summary>
public enum SomeOptions
{
    /// <summary>
    /// Option one.
    /// </summary>
    OneOption,

    /// <summary>
    /// Second option.
    /// </summary>
    AnotherOption
}

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
/*
        Options = SomeOptions.AnotherOption;
        TrueFalseSetting = true;
        AnInteger = 2;
        AString = "string";
*/
        SMTPServer = "smtp.gmail.com";
        SMTPPort = 587;
        SMTPUser = string.Empty;
        SMTPPass = string.Empty;
    }

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
    /// Gets or sets an enum option.
    /// </summary>
    public string SMTPPass { get; set; }
}
