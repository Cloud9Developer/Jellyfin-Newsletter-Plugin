#pragma warning disable SA1611, CS0162
using System;
using System.IO;
using Jellyfin.Plugin.Newsletters.Configuration;

namespace Jellyfin.Plugin.Newsletters.LOGGER;

/// <summary>
/// Initializes a new instance of the <see cref="Logger"/> class.
/// </summary>
public class Logger
{
    private readonly PluginConfiguration config;
    private readonly string logFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class.
    /// </summary>
    public Logger()
    {
        config = Plugin.Instance!.Configuration;
        logFile = $"{config.LogDirectoryPath}/{GetDate()}_Newsletter.log";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Debug"/> class.
    /// </summary>
    public void Debug(object msg)
    {
        PluginConfiguration config = Plugin.Instance!.Configuration;
        if (config.DebugMode)
        {
            Info(msg);
        }
    }

    /// <summary>
    /// Inform info into the logs.
    /// </summary>
    public void Info(object msg)
    {
        Inform(msg, "INFO");
    }

    /// <summary>
    /// Inform warn into the logs.
    /// </summary>
    public void Warn(object msg)
    {
        Inform(msg, "WARN");
    }

    /// <summary>
    /// Inform error into the logs.
    /// </summary>
    public void Error(object msg)
    {
        Inform(msg, "ERR");
    }

    /// <summary>
    /// Inform specific type of warning into the logs.
    /// </summary>
    /// <param name="msg">The message to infrom into the logs.</param>
    /// <param name="type">Type of warning ("ERR", "WARN", "INFO").</param>
    private void Inform(object msg, string type)
    {
        string logMsgPrefix = $"[NLP]: {GetDateTime()} - [{type}] ";
        Console.WriteLine($"{logMsgPrefix}{msg}");
        File.AppendAllText(logFile, $"{logMsgPrefix}{msg}\n");
    }

    private string GetDateTime()
    {
        return DateTime.Now.ToString("[yyyy-MM-dd] :: [HH:mm:ss]", System.Globalization.CultureInfo.CurrentCulture);
    }

    private string GetDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
    }
}