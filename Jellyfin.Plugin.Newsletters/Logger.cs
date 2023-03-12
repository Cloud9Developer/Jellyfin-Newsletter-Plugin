#pragma warning disable SA1611, CS0162
using System;
using Jellyfin.Plugin.Newsletters.Configuration;

namespace Jellyfin.Plugin.Newsletters.LOGGER;

/// <summary>
/// Initializes a new instance of the <see cref="Logger"/> class.
/// </summary>
public class Logger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Debug"/> class.
    /// </summary>
    public void Debug(object msg)
    {
        PluginConfiguration config = Plugin.Instance!.Configuration;
        if (config.DebugMode)
        {
            Console.WriteLine("[NLP]: " + GetDateTime() + " - [DEBUG] " + msg);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Info"/> class.
    /// </summary>
    public void Info(object msg)
    {
        Console.WriteLine("[NLP]: " + GetDateTime() + " - [INFO] " + msg);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Warn"/> class.
    /// </summary>
    public void Warn(object msg)
    {
        Console.WriteLine("[NLP]: " + GetDateTime() + " - [WARN] " + msg);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    public void Error(object msg)
    {
        Console.WriteLine("[NLP]: " + GetDateTime() + " - [ERR] " + msg);
    }

    private string GetDateTime()
    {
        return DateTime.Now.ToString("[yyyy-MM-dd] :: [HH:mm:ss]", System.Globalization.CultureInfo.CurrentCulture);
    }
}