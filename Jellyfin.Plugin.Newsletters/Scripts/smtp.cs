#pragma warning disable 1591
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Net.NetworkCredential;

namespace Jellyfin.Plugin.Newsletters.Scripts
{
    /// <summary>
    /// Interaction logic for SendMail.xaml.
    /// </summary>
    // [Route("newsletters/[controller]")]
    [ApiController]
    [Route("[controller]")]
    public class Smtp : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static void SendSmtp(string recipient, string sender)
        {
            // created object of SmtpClient details and provides server details
            System.Console.WriteLine("Running SMTP C# File");
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;

            // Server Credentials
            NetworkCredential nc = new NetworkCredential();
            nc.UserName = "chris.hebert.jesusfreak09@gmail.com";
            nc.Password = "hmctnexjirhhphms";
            // assigned credetial details to server
            smtp.Credentials = nc;

            // create sender address
            MailAddress from = new MailAddress(sender, sender);

            // create receiver address
            MailAddress receiver = new MailAddress(recipient, recipient);

            MailMessage mymessage = new MailMessage(from, receiver);
            mymessage.Subject = "this is a test message";
            mymessage.Body = string.Empty;
            // sends the email
            smtp.Send(mymessage);
        }

/*
        public Task SendSmtpTask(IProgress<double> progress, CancellationToken cancellationToken)
        {
            try {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine("I made it!!!");
                progress.Report(100);
            }
            catch (Exception e) {
                Console.WriteLine("{0} Exception Caught.", e);
            }
            return Task.CompletedTask;
        }
*/
    }
}