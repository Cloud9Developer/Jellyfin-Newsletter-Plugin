#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Newsletters.Clients.Discord.WEBHOOK;
using Jellyfin.Plugin.Newsletters.Clients.Emails.EMAIL;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Mvc;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Clients.CLIENT;

public class Client : ControllerBase
{
    public Client(IServerApplicationHost applicationHost)
    {
        Db = new SQLiteDatabase();
        Logger = new Logger();
        Config = Plugin.Instance!.Configuration;
        ApplicationHost = applicationHost;
    }

    protected IServerApplicationHost ApplicationHost { get; }

    protected PluginConfiguration Config { get; }

    protected SQLiteDatabase Db { get; set; }

    protected Logger Logger { get; set; }

    private void CopyNewsletterDataToArchive()
    {
        Logger.Info("Appending NewsletterData for Current Newsletter Cycle to Archive Database..");

        try
        {
            Db.CreateConnection();

            // copy tables
            Db.ExecuteSQL("INSERT INTO ArchiveData SELECT * FROM CurrNewsletterData;");
            Db.ExecuteSQL("DELETE FROM CurrNewsletterData;");
        }
        catch (Exception e)
        {
            Logger.Error("An error has occured: " + e);
        }
        finally
        {
            Db.CloseConnection();
        }
    }

    protected bool NewsletterDbIsPopulated()
    {
        foreach (var row in Db.Query("SELECT COUNT(*) FROM CurrNewsletterData;"))
        {
            if (row is not null)
            {
                if (int.Parse(row[0].ToString(), CultureInfo.CurrentCulture) > 0)
                {
                    Db.CloseConnection();
                    return true;
                }
            }
        }

        Db.CloseConnection();
        return false;
    }

    public void NotifyAll()
    {
        var clients = new List<IClient>
        {
            new DiscordWebhook(ApplicationHost),
            new Smtp(ApplicationHost)
            // Scope to add other clients in future
        };

        bool result = false;
        foreach (var client in clients)
        {
            Logger.Debug($"Send triggered for the {client}");
            result |= client.Send();
        }

        // If we the result is True i.e. even if any one client was successful
        // to send the newsletter we'll move the current database
        if (result)
        {
            Logger.Debug("Atleast one of the client sent the newsletter. Proceeding forward...");
            CopyNewsletterDataToArchive();
        }
        else
        {
            // There could be a case when there is no newsletter to be send. So marking this as Info rather an Error
            // for now.
            Logger.Info("None of the client were able to send the newsletter. Please check the plugin configuration.");
        }
    }
}

public interface IClient
{
    bool Send();
}