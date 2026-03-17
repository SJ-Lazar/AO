using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AO.Core.Shared.Notifications;

public sealed class Message
{
    public const string GmailHost = "smtp.gmail.com";
    public const int GmailPort = 587;
    public const string UsernameConfigurationKey = "Notifications:Gmail:Username";
    public const string PasswordConfigurationKey = "Notifications:Gmail:AppPassword";
    public const string FromAddressConfigurationKey = "Notifications:Gmail:FromAddress";
    public const string FromNameConfigurationKey = "Notifications:Gmail:FromName";

    private readonly List<string> _to = [];
    private readonly IConfiguration? _configuration;

    public Message()
    {
    }

    public Message(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyCollection<string> To => _to.AsReadOnly();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsBodyHtml { get; set; }

    public void AddRecipient(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        _to.Add(email);
    }

    public Task SendWithGmailAsync(CancellationToken cancellationToken = default)
    {
        var username = GetRequiredConfigurationValue(UsernameConfigurationKey);
        var password = GetRequiredConfigurationValue(PasswordConfigurationKey);
        var fromAddress = _configuration?[FromAddressConfigurationKey] ?? username;
        var fromName = _configuration?[FromNameConfigurationKey];

        return SendWithGmailAsync(username, password, fromAddress, fromName, cancellationToken);
    }

    public async Task SendWithGmailAsync(
        string username,
        string appPassword,
        string fromAddress,
        string? fromName = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCanSend();
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(appPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromAddress);

        using var mailMessage = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(fromName)
                ? new MailAddress(fromAddress)
                : new MailAddress(fromAddress, fromName),
            Subject = Subject,
            Body = Body,
            IsBodyHtml = IsBodyHtml
        };

        foreach (var recipient in _to)
        {
            mailMessage.To.Add(recipient);
        }

        using var smtpClient = new SmtpClient(GmailHost, GmailPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, appPassword)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }

    private void ValidateCanSend()
    {
        if (_to.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient is required.");
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            throw new InvalidOperationException("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(Body))
        {
            throw new InvalidOperationException("Body is required.");
        }
    }

    private string GetRequiredConfigurationValue(string key)
    {
        var value = _configuration?[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Notification setting '{key}' is not configured.");
        }

        return value;
    }
}
