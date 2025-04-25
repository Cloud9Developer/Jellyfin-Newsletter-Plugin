#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.Discord.EMBEDBuilder;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Discord.CLIENT;

/// <summary>
/// Interaction logic for SendMail.xaml.
/// </summary>
[Authorize(Policy = Policies.RequiresElevation)]
[ApiController]
[Route("Discord")]
public class DiscordClient : ControllerBase, IDisposable
{
    private readonly PluginConfiguration config;
    private readonly HttpClient _httpClient;
    private SQLiteDatabase db;
    private Logger logger;

    public DiscordClient()
    {
        db = new SQLiteDatabase();
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
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
    public async Task<IActionResult> SendDiscordTestMessage()
    {       
        // var payload = new DiscordPayload
        // {
        //     username = "Newsletter Bot",
        //     embeds = new List<Embed>
        //     {
        //         new Embed
        //         {
        //             title = "Sample Title",
        //             url = "http://192.168.1.63:8265",
        //             color = 16776960,
        //             timestamp = DateTime.UtcNow.ToString("o"),
        //             fields = new List<EmbedField>
        //             {
        //                 new EmbedField { name = "Name", value = "TestFile", inline = true },
        //                 // Add other fields...
        //             }
        //         },
        //         new Embed
        //         {
        //             title = "ssss",
        //             url = "http://192.168.1.63:5645",
        //             color = 16776960,
        //             timestamp = DateTime.UtcNow.ToString("o"),
        //             fields = new List<EmbedField>
        //             {
        //                 new EmbedField { name = "Name", value = "TestFile", inline = true },
        //                 // Add other fields...
        //             }
        //         }
        //     }
        // };

        // var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        // logger.Info("gladiator jsonPayload is: " + jsonPayload);
        // var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // var response = await _httpClient.PostAsync(webhookUrl, content).ConfigureAwait(false);

        // if (response.IsSuccessStatusCode)
        // {
        //     return Ok("Message sent to Discord successfully!");
        // }
        // else
        // {
        //     var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //     return StatusCode((int)response.StatusCode, $"Error sending to Discord: {error}");
        // }

        // string webhookUrl = config.DiscordWebhookURL;
        string webhookUrl = config.DiscordWebhookURL;
        
        EmbedBuilder builder = new EmbedBuilder();
        List<Embed> embedList = builder.BuildEmbedsFromNewsletterData();

        for (int i = 0; i < embedList.Count; i += 10)
        {
            var chunk = embedList.Skip(i).Take(10).ToList();

            var payload = new DiscordPayload
            {
                username = "Newsletter Bot",
                embeds = chunk
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            logger.Info("gladiator jsonPayload is: " + jsonPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content).ConfigureAwait(false);

            // if (response.IsSuccessStatusCode)
            // {
            //     return Ok("Message sent to Discord successfully!");
            // }
            // else
            // {
            //     var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //     return StatusCode((int)response.StatusCode, $"Error sending to Discord: {error}");
            // }
        }

        return Ok("Test Done");
    }

    [HttpPost("SendDiscordMessage")]
    public async Task<IActionResult> SendDiscordMessage()
    {
        try
        {
            db.CreateConnection();

            if (NewsletterDbIsPopulated())
            {
                logger.Debug("Sending out Discord message!");

                string webhookUrl = config.DiscordWebhookURL;
                EmbedBuilder builder = new EmbedBuilder();
                List<Embed> embedList = builder.BuildEmbedsFromNewsletterData();

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

                    var response = await _httpClient.PostAsync(webhookUrl, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        logger.Debug("Discord message sent successfully");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new HttpRequestException($"Discord webhook failed: {response.StatusCode} - {error}");
                    }
                }

                return Ok("Discord message(s) sent successfully.");
            }
            else
            {
                logger.Info("There is no Newsletter data.. Have I scanned or sent out a newsletter recently?");
                return NoContent(); // 204 No Content
            }
        }
        catch (Exception e)
        {
            logger.Error("An error has occured: " + e);
            return StatusCode(500, "An unexpected error occurred.");
        }
        finally
        {
            db.CloseConnection();
        }
    }

    private bool NewsletterDbIsPopulated()
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
}