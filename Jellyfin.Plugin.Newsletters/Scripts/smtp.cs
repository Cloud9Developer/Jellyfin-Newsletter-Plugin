#pragma warning disable 1591
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.HTMLBuilder;
using Jellyfin.Plugin.Newsletters.Scripts.SCRAPER;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Scripts;

/// <summary>
/// Interaction logic for SendMail.xaml.
/// </summary>
// [Route("newsletters/[controller]")]
[ApiController]
[Route("Smtp")]
public class Smtp : ControllerBase
{
    private readonly PluginConfiguration config;
    private readonly string newsletterDataFile;
    private Logger logger;

    public Smtp()
    {
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
    }

    [HttpPost("SendSmtp")]
    // [ProducesResponseType(StatusCodes.Status201Created)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void SendEmail()
    {
        if (System.IO.File.Exists(newsletterDataFile))
        {
            // Smtp varsmtp = new Smtp();
            MailMessage mail = new MailMessage();
            string smtpAddress = config.SMTPServer;
            int portNumber = config.SMTPPort;
            bool enableSSL = true;
            string emailFromAddress = config.SMTPUser;
            string password = config.SMTPPass;
            string emailToAddress = config.ToAddr;
            string subject = config.Subject;
            string body;

            HtmlBuilder hb = new HtmlBuilder();

            body = hb.GetDefaultHTMLBody();
            string builtString = hb.BuildDataHtmlStringFromNewsletterData();
            string finalBody = hb.ReplaceBodyWithBuiltString(body, builtString);

            mail.From = new MailAddress(emailFromAddress, emailFromAddress);
            mail.To.Clear();
            string[] emailArr = emailToAddress.Split(',');

            foreach (string email in emailArr)
            {
                mail.Bcc.Add(email.Trim());
            }

            mail.Subject = subject;
            mail.Body = finalBody;
            mail.IsBodyHtml = true;
            // mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment
            SmtpClient smtp = new SmtpClient(smtpAddress, portNumber);
            smtp.Credentials = new NetworkCredential(emailFromAddress, password);
            smtp.EnableSsl = enableSSL;
            smtp.Send(mail);

            hb.CleanUp(finalBody);
        }
        else
        {
            logger.Info("There is no Newsletter data.. Have I scanned or sent out a newsletter recently recently?");
        }
    }

    private void WriteToArchive()
    {
        string test = string.Empty;
    }
}