using OrderFlow.NotificationService.Application.Interfaces.Services;
using OrderFlow.NotificationService.Infrastructure.Configuration;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace OrderFlow.NotificationService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;

    public EmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port);
        client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        client.EnableSsl = true;
        client.Timeout = 5000; // 5 seconds — fail fast when SMTP is unreachable

        using var message = new MailMessage();
        message.From = new MailAddress(_options.From);
        message.To.Add(to);
        message.Subject = subject;
        message.Body = body;

        await client.SendMailAsync(message, cancellationToken);
    }
}
