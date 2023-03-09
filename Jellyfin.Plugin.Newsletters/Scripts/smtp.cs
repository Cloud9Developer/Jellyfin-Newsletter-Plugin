using System;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Net.NetworkCredential;

namespace Jellyfin.Plugin.NewsLetters.Scripts
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
        private void SendSmtp(string recipient, string sender)
        {
            // created object of SmtpClient details and provides server details
            System.Console.WriteLine("Running SMTP C# File");
            SmtpClient smtp = new SmtpClient();
            smtp.Host = string.Empty;
            smtp.Port = 81;

            // Server Credentials
            NetworkCredential nc = new NetworkCredential();
            nc.UserName = string.Empty;
            nc.Password = string.Empty;
            // assigned credetial details to server
            smtp.Credentials = nc;

            // create sender address
            MailAddress from = new MailAddress("Sender Address", "Name want to display");

            // create receiver address
            MailAddress receiver = new MailAddress(recipient, "Name want to display");

            MailMessage mymessage = new MailMessage(from, receiver);
            mymessage.Subject = string.Empty;
            mymessage.Body = string.Empty;
            // sends the email
            smtp.Send(mymessage);
        }
    }
}
