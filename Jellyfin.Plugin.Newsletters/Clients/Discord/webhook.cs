#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Clients.CLIENT;
using Jellyfin.Plugin.Newsletters.Clients.Discord.EMBEDBuilder;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Newsletters.Clients.Discord.WEBHOOK;

[Authorize(Policy = Policies.RequiresElevation)]
[ApiController]
[Route("Discord")]
public class DiscordWebhook : Client, IClient, IDisposable
{
    private readonly HttpClient _httpClient;

    public DiscordWebhook(IServerApplicationHost applicationHost) : base(applicationHost)
    {
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    [HttpPost("SendDiscordTestMessage")]
    public void SendDiscordTestMessage()
    {
        string webhookUrl = config.DiscordWebhookURL;

        if (string.IsNullOrEmpty(webhookUrl))
        {
            logger.Info("Discord webhook URL is not set. Aborting test message.");
        }

        try
        {
            var payload = new
            {
                username = config.DiscordWebhookName,
                content = "✅ Test notification from Jellyfin Newsletter plugin 🚀"
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            logger.Debug("Sending Discord test message: " + jsonPayload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(webhookUrl, content).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                logger.Debug("Discord test message sent successfully");
            }
            else
            {
                var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                logger.Error($"Discord test message failed: {response.StatusCode} - {error}");
            }
        }
        catch (Exception e)
        {
            logger.Error("An error occurred while sending Discord test message: " + e);
        }
    }

    [HttpPost("SendDiscordMessage")]
    public void SendDiscordMessage()
    {
        try
        {
            db.CreateConnection();

            if (NewsletterDbIsPopulated())
            {
                logger.Debug("Sending out Discord message!");

                string webhookUrl = config.DiscordWebhookURL;
                EmbedBuilder builder = new EmbedBuilder();
                List<Embed> embedList = builder.BuildEmbedsFromNewsletterData(_applicationHost.SystemId);

                // Discord webhook does not support more than 10 embeds per message
                // Therefore, we're sending in chunks with atmost 10 embed in a payload
                for (int i = 0; i < embedList.Count; i += 10)
                {
                    var chunk = embedList.Skip(i).Take(10).ToList();

                    var payload = new DiscordPayload
                    {
                        username = config.DiscordWebhookName,
                        embeds = chunk
                    };

                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                    logger.Debug("Sending discord message in chunks: " + jsonPayload);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = _httpClient.PostAsync(webhookUrl, content).GetAwaiter().GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        logger.Debug("Discord message sent successfully");
                    }
                    else
                    {
                        var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        logger.Error($"Discord webhook failed: {response.StatusCode} - {error}");
                    }
                }
            }
            else
            {
                logger.Info("There is no Newsletter data.. Have I scanned or sent out a discord newsletter recently?");
            }
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

    public void Send()
    {
        SendDiscordMessage();
    }
}