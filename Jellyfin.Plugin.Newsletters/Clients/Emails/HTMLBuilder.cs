#pragma warning disable 1591, SYSLIB0014, CA1002, CS0162, SA1005 // remove SA1005 for cleanup
using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Newsletters.Clients.CLIENTBuilder;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using Newtonsoft.Json;
// using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Newsletters.Clients.Emails.HTMLBuilder;

public class HtmlBuilder : ClientBuilder
{
    // Global Vars
    // Readonly
    private readonly string newslettersDir;
    private readonly string newsletterHTMLFile;
    // private readonly string[] itemJsonKeys = 

    private string emailBody;

    // Non-readonly
    private static string append = "Append";
    private static string write = "Overwrite";
    // private List<string> fileList;

    public HtmlBuilder()
    {
        emailBody = config.Body;

        newslettersDir = config.NewsletterDir; // newsletterdir
        Directory.CreateDirectory(newslettersDir);

        // if no newsletter filename is saved or the file doesn't exist
        if (config.NewsletterFileName.Length == 0 || File.Exists(newslettersDir + config.NewsletterFileName))
        {
            // use date to create filename
            string currDate = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            newsletterHTMLFile = newslettersDir + currDate + "_Newsletter.html";
        }
        else
        {
            newsletterHTMLFile = newslettersDir + config.NewsletterFileName;
        }

        logger.Info("Newsletter will be saved to: " + newsletterHTMLFile);
    }

    public string GetDefaultHTMLBody()
    {
        emailBody = config.Body;
        return emailBody;
    }

    public string TemplateReplace(string htmlObj, string replaceKey, object replaceValue, bool finalPass = false)
    {
        logger.Debug("Replacing {} params:\n " + htmlObj);
        if (replaceValue is null)
        {
            logger.Debug($"Replace string is null.. Nothing to replace");
            return htmlObj;
        }

        if (replaceKey == "{RunTime}" && (int)replaceValue == 0)
        {
            logger.Debug($"{replaceKey} == {replaceValue}");
            logger.Debug("Skipping replace..");
            return htmlObj;
        }

        logger.Debug($"Replace Value {replaceKey} with " + replaceValue);

        // Dictionary<string, object> html_params = new Dictionary<string, object>();
        // html_params.Add("{Date}", DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        // html_params.Add(replaceKey, replaceValue);

        htmlObj = htmlObj.Replace(replaceKey, replaceValue.ToString(), StringComparison.Ordinal);
        // logger.Debug("HERE\n " + htmlObj)

        // foreach (KeyValuePair<string, object> param in html_params)
        // {
        //     if (param.Value is not null)
        //     {
        //         htmlObj = htmlObj.Replace(param.Key, param.Value.ToString(), StringComparison.Ordinal);
        //         // logger.Debug("HERE\n " + htmlObj)
        //     }
        // }
        
        logger.Debug("New HTML OBJ: \n" + htmlObj);
        return htmlObj;
    }

    public string BuildDataHtmlStringFromNewsletterData()
    {
        List<string> completed = new List<string>();
        string builtHTMLString = string.Empty;
        // pull data from CurrNewsletterData table

        try
        {
            db.CreateConnection();

            foreach (var row in db.Query("SELECT * FROM CurrNewsletterData;"))
            {
                if (row is not null)
                {
                    JsonFileObj item = jsonHelper.ConvertToObj(row);
                    // scan through all items and get all Season numbers and Episodes
                    // (string seasonInfo, string episodeInfo) = ParseSeriesInfo(obj, readDataFile);
                    if (completed.Contains(item.Title))
                    {
                        continue;
                    }

                    string seaEpsHtml = string.Empty;
                    if (item.Type == "Series")
                    {
                        // for series only
                        List<NlDetailsJson> parsedInfoList = ParseSeriesInfo(item);
                        seaEpsHtml += GetSeasonEpisodeHTML(parsedInfoList);
                    }

                    var tmp_entry = config.Entry;
                    // logger.Debug("TESTING");
                    // logger.Debug(item.GetDict()["Filename"]);

                    foreach (KeyValuePair<string, object?> ele in item.GetReplaceDict())
                    {
                        if (ele.Value is not null)
                        {
                            tmp_entry = this.TemplateReplace(tmp_entry, ele.Key, ele.Value);
                        }
                    }

                    builtHTMLString += tmp_entry.Replace("{SeasonEpsInfo}", seaEpsHtml, StringComparison.Ordinal)
                                                .Replace("{ServerURL}", config.Hostname, StringComparison.Ordinal);
                    completed.Add(item.Title);
                }
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

        return builtHTMLString;
    }

    private string GetSeasonEpisodeHTML(List<NlDetailsJson> list)
    {
        string html = string.Empty;
        foreach (NlDetailsJson obj in list)
        {
            logger.Debug("SNIPPET OBJ: " + JsonConvert.SerializeObject(obj));
            // html += "<div id='SeasonEpisode' class='text' style='color: #FFFFFF;'>Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "</div>";
            html += "Season: " + obj.Season + " - Eps. " + obj.EpisodeRange + "<br>";
        }

        return html;
    }

    public void CleanUp(string htmlBody)
    {
        // save newsletter to file
        logger.Info("Saving HTML file");
        WriteFile(write, newsletterHTMLFile, htmlBody);
    }

    private void WriteFile(string method, string path, string value)
    {
        if (method == append)
        {
            File.AppendAllText(path, value);
        }
        else if (method == write)
        {
            File.WriteAllText(path, value);
        }
    }
}