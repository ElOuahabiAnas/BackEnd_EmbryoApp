using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Identity;

namespace EmbryoApp.Service.Implementation;

using System.Net;
using System.Net.Mail;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Smtp:Host"];
        var port = int.Parse(_config["Smtp:Port"]);
        var user = _config["Smtp:User"];
        var pass = _config["Smtp:Pass"];

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mail = new MailMessage(user, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mail);
    }
}