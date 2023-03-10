#pragma warning disable 1591
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.Scripts.Scraper;
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

    public Smtp()
    {
        config = Plugin.Instance!.Configuration;
    }

    [HttpPost("SendSmtp")]
    // [ProducesResponseType(StatusCodes.Status201Created)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void SendEmail()
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

        if (config.Body.Trim().Length != 0)
        {
            body = config.Body;
        }
        else
        {
            body = "Default Value";
        }

        mail.From = new MailAddress(emailFromAddress, emailFromAddress);
        // mail.To.Add(emailToAddress);
        mail.To.Clear();
        string[] emailArr = emailToAddress.Split(',');

        foreach (string email in emailArr)
        {
            mail.Bcc.Add(email.Trim());
        }

        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;
        // mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment
        SmtpClient smtp = new SmtpClient(smtpAddress, portNumber);
        smtp.Credentials = new NetworkCredential(emailFromAddress, password);
        smtp.EnableSsl = enableSSL;
        smtp.Send(mail);
    }
}