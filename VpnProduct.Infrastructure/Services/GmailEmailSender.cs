using System.Net;
using System.Net.Mail;

using Microsoft.Extensions.Configuration;

using VpnProduct.Application.Interfaces;

namespace VpnProduct.Infrastructure.Services;

public class GmailEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public GmailEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "";
        var smtpPortText = _configuration["Email:SmtpPort"] ?? "587";
        var smtpUser = _configuration["Email:SmtpUser"] ?? "";
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
        var fromName = _configuration["Email:FromName"] ?? "VpnProduct";

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpUser) ||
            string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new InvalidOperationException("Email SMTP setting is missing.");
        }

        var smtpPort = int.Parse(smtpPortText);

        using var message = new MailMessage();

        message.From = new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPassword)
        };

        await client.SendMailAsync(message);
    }
}