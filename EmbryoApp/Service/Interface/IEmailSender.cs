namespace EmbryoApp.Service.Interface;

using System.Net;
using System.Net.Mail;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}