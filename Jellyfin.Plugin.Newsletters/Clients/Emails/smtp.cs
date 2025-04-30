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
    public Smtp(IServerApplicationHost applicationHost) : base(applicationHost) 
    {
    }

    [HttpPost("SendTestMail")]
    public void SendTestMail()
    {
        MailMessage mail;
        SmtpClient smtp;

        try
        {
            Logger.Debug("Sending out test mail!");
            mail = new MailMessage();

            mail.From = new MailAddress(Config.FromAddr);
            mail.To.Clear();
            mail.Subject = "Jellyfin Newsletters - Test";
            mail.Body = "Success! You have properly configured your email notification settings";
            mail.IsBodyHtml = false;

            foreach (string email in Config.ToAddr.Split(','))
            {
                mail.Bcc.Add(email.Trim());
            }

            smtp = new SmtpClient(Config.SMTPServer, Config.SMTPPort);
            smtp.Credentials = new NetworkCredential(Config.SMTPUser, Config.SMTPPass);
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }
        catch (Exception e)
        {
            Logger.Error("An error has occured: " + e);
        }
    }

    [HttpPost("SendSmtp")]
    // [ProducesResponseType(StatusCodes.Status201Created)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public bool SendEmail()
    {
        bool result = false;
        try
        {
            Db.CreateConnection();

            if (NewsletterDbIsPopulated())
            {
                Logger.Debug("Sending out mail!");
                // Smtp varsmtp = new Smtp();
                MailMessage mail = new MailMessage();
                string smtpAddress = Config.SMTPServer;
                int portNumber = Config.SMTPPort;
                bool enableSSL = true;
                string emailFromAddress = Config.FromAddr;
                string username = Config.SMTPUser;
                string password = Config.SMTPPass;
                string emailToAddress = Config.ToAddr;
                string subject = Config.Subject;
                // string body;

                HtmlBuilder hb = new HtmlBuilder();

                string body = hb.GetDefaultHTMLBody();
                string builtString = hb.BuildDataHtmlStringFromNewsletterData();
                // string finalBody = hb.ReplaceBodyWithBuiltString(body, builtString);
                // string finalBody = hb.TemplateReplace(hb.ReplaceBodyWithBuiltString(body, builtString), "{ServerURL}", Config.Hostname);
                builtString = hb.TemplateReplace(hb.ReplaceBodyWithBuiltString(body, builtString), "{ServerURL}", Config.Hostname);
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
                result = true;

                hb.CleanUp(builtString);
            }
            else
            {
                Logger.Info("There is no Newsletter data.. Have I scanned or sent out an email newsletter recently?");
            }
        }
        catch (Exception e)
        {
            Logger.Error("An error has occured: " + e);
        }
        finally
        {
            Db.CloseConnection();
        }

        return result;
    }

    public bool Send()
    {
        return SendEmail();
    }
}
