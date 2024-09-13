
namespace CheckIn.API.Helpers
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using System.Web.Configuration;

    public class MailHelper
    {
        public static async Task SendMail(string to, string subject, string body)
        {
            var message = new MailMessage();
            message.To.Add(new MailAddress(to));
            message.From = new MailAddress(WebConfigurationManager.AppSettings["SmtpUserName"]);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = WebConfigurationManager.AppSettings["SmtpUserName"],
                    Password = WebConfigurationManager.AppSettings["SmtpPassword"]
                };

                smtp.Credentials = credential;
                smtp.Host = WebConfigurationManager.AppSettings["SmtpHostname"];
                smtp.Port = int.Parse(WebConfigurationManager.AppSettings["SmtpPort"]);
                smtp.EnableSsl = bool.Parse(WebConfigurationManager.AppSettings["SmtpSsl"]);
                await smtp.SendMailAsync(message);
            }
        }

        public static async Task SendMail(List<string> mails, string subject, string body)
        {
            var message = new MailMessage();

            foreach (var to in mails)
            {
                if(!string.IsNullOrWhiteSpace(to))
                    message.To.Add(new MailAddress(to.Trim()));
            }

            message.From = new MailAddress(WebConfigurationManager.AppSettings["SmtpEmail"]);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = WebConfigurationManager.AppSettings["SmtpEmail"],
                    Password = WebConfigurationManager.AppSettings["SmtpPassword"]
                };

                smtp.Credentials = credential;
                smtp.Host = WebConfigurationManager.AppSettings["SmtpHostName"];
                smtp.Port = int.Parse(WebConfigurationManager.AppSettings["SmtpPort"]);
                smtp.EnableSsl = bool.Parse(WebConfigurationManager.AppSettings["SmtpUseSSL"]);
                await smtp.SendMailAsync(message);
            }
        }
    }
}