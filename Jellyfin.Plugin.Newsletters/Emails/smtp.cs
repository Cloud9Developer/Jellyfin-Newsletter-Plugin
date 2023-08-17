#pragma warning disable 1591
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Shared.DATA;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// using System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Emails.EMAIL;

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
    private SQLiteDatabase db;
    private Logger logger;

    public Smtp()
    {
        db = new SQLiteDatabase();
        logger = new Logger();
        config = Plugin.Instance!.Configuration;
        newsletterDataFile = config.MyDataDir + config.NewsletterDataFileName;
    }

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

            foreach (var email in config.ToAddr.Split(','))
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
                var mail = new MailMessage();
                var smtpAddress = config.SMTPServer;
                var portNumber = config.SMTPPort;
                var enableSSL = true;
                var emailFromAddress = config.FromAddr;
                var username = config.SMTPUser;
                var password = config.SMTPPass;
                var emailToAddress = config.ToAddr;
                var subject = config.Subject;

                var hb = new HtmlBuilder();
                var builtString = hb.BuildDataHtmlStringFromNewsletterData();

                mail.From = new MailAddress(emailFromAddress, emailFromAddress);
                mail.To.Clear();
                mail.Subject = subject;
                mail.Body = builtString;
                mail.IsBodyHtml = true;

                foreach (var email in emailToAddress.Split(','))
                {
                    mail.Bcc.Add(email.Trim());
                }

                // mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment
                var smtp = new SmtpClient(smtpAddress, portNumber);
                smtp.Credentials = new NetworkCredential(username, password);
                smtp.EnableSsl = enableSSL;
                smtp.Send(mail);

                hb.CleanUp(builtString);
            }
            else
            {
                logger.Info("There is no Newsletter data.. Have I scanned or sent out a newsletter recently?");
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

    public bool NewsletterDbIsPopulated()
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

    private void WriteToArchive()
    {
        var test = string.Empty;
    }
}
