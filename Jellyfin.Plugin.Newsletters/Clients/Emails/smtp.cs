#pragma warning disable 1591
using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Newsletters.Clients.CLIENT;
using Jellyfin.Plugin.Newsletters.Clients.Emails.HTMLBuilder;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Clients.Emails.EMAIL;

/// <summary>
/// Interaction logic for SendMail.xaml.
/// </summary>
// [Route("newsletters/[controller]")]
[Authorize(Policy = Policies.RequiresElevation)]
[ApiController]
[Route("Smtp")]
public class Smtp : Client, IClient
{
    public Smtp(IServerApplicationHost applicationHost) : base(applicationHost) {}

    [HttpPost("SendTestMail")]
    public void SendTestMail()
    {
        MailMessage mail;
        SmtpClient smtp;

        try
        {
            logger.Debug("Sending out test mail!");
            mail = new MailMessage();

            mail.From = new MailAddress(config.FromAddr);
            mail.To.Clear();
            mail.Subject = "Jellyfin Newsletters - Test";
            mail.Body = "Success! You have properly configured your email notification settings";
            mail.IsBodyHtml = false;

            foreach (string email in config.ToAddr.Split(','))
            {
                mail.Bcc.Add(email.Trim());
            }

            smtp = new SmtpClient(config.SMTPServer, config.SMTPPort);
            smtp.Credentials = new NetworkCredential(config.SMTPUser, config.SMTPPass);
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }
        catch (Exception e)
        {
            logger.Error("An error has occured: " + e);
        }
    }

    [HttpPost("SendSmtp")]
    // [ProducesResponseType(StatusCodes.Status201Created)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void SendEmail()
    {
        try
        {
            db.CreateConnection();

            if (NewsletterDbIsPopulated())
            {
                logger.Debug("Sending out mail!");
                // Smtp varsmtp = new Smtp();
                MailMessage mail = new MailMessage();
                string smtpAddress = config.SMTPServer;
                int portNumber = config.SMTPPort;
                bool enableSSL = true;
                string emailFromAddress = config.FromAddr;
                string username = config.SMTPUser;
                string password = config.SMTPPass;
                string emailToAddress = config.ToAddr;
                string subject = config.Subject;
                // string body;

                HtmlBuilder hb = new HtmlBuilder();

                string body = hb.GetDefaultHTMLBody();
                string builtString = hb.BuildDataHtmlStringFromNewsletterData();
                // string finalBody = hb.ReplaceBodyWithBuiltString(body, builtString);
                // string finalBody = hb.TemplateReplace(hb.ReplaceBodyWithBuiltString(body, builtString), "{ServerURL}", config.Hostname);
                builtString = hb.TemplateReplace(hb.ReplaceBodyWithBuiltString(body, builtString), "{ServerURL}", config.Hostname);
                string currDate = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                builtString = builtString.Replace("{Date}", currDate, StringComparison.Ordinal);

                mail.From = new MailAddress(emailFromAddress, emailFromAddress);
                mail.To.Clear();
                mail.Subject = subject;
                mail.Body = Regex.Replace(builtString, "{[A-za-z]*}", " "); // Final cleanup
                mail.IsBodyHtml = true;

                foreach (string email in emailToAddress.Split(','))
                {
                    mail.Bcc.Add(email.Trim());
                }

                // mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment
                SmtpClient smtp = new SmtpClient(smtpAddress, portNumber);
                smtp.Credentials = new NetworkCredential(username, password);
                smtp.EnableSsl = enableSSL;
                smtp.Send(mail);

                hb.CleanUp(builtString);
            }
            else
            {
                logger.Info("There is no Newsletter data.. Have I scanned or sent out an email newsletter recently?");
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
        SendEmail();
    }
}
