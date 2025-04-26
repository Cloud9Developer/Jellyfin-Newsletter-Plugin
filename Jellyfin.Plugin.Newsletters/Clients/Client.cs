#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Newsletters.Clients.Discord.WEBHOOK;
using Jellyfin.Plugin.Newsletters.Clients.Emails.EMAIL;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Controller;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Clients.CLIENT;

public class Client : ControllerBase
{
    protected readonly IServerApplicationHost _applicationHost;
    protected readonly PluginConfiguration config;
    protected SQLiteDatabase db;
    protected Logger logger;

    public Client(IServerApplicationHost applicationHost)
    {
        db = new SQLiteDatabase();
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        _applicationHost = applicationHost;
    }

    private void CopyNewsletterDataToArchive()
    {
        logger.Info("Appending NewsletterData for Current Newsletter Cycle to Archive Database..");

        try
        {
            db.CreateConnection();

            // copy tables
            db.ExecuteSQL("INSERT INTO ArchiveData SELECT * FROM CurrNewsletterData;");
            db.ExecuteSQL("DELETE FROM CurrNewsletterData;");
        }
        catch (Exception e)
        {
            logger.Error("An error has occured: " + e);
        }
        finally
        {
            db.CloseConnection();
        }
    }

    protected bool NewsletterDbIsPopulated()
    {
        foreach (var row in db.Query("SELECT COUNT(*) FROM CurrNewsletterData;"))
        {
            if (row is not null)
            {
                if (int.Parse(row[0].ToString(), CultureInfo.CurrentCulture) > 0)
                {
                    db.CloseConnection();
                    return true;
                }
            }
        }

        db.CloseConnection();
        return false;
    }

    public void NotifyAll()
    {
        var clients = new List<IClient>
        {
            new DiscordWebhook(_applicationHost),
            new Smtp(_applicationHost)
            // Scope to add other clients in future
        };

        foreach (var client in clients)
        {
            logger.Debug($"Send triggered for the {client}");
            client.Send();
        }

        // Move the db once every client has sent the message
        CopyNewsletterDataToArchive();
    }
}

public interface IClient
{
    void Send();
}