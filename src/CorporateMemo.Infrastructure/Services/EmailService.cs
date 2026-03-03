using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CorporateMemo.Infrastructure.Services;

/// <summary>
/// MailKit/MimeKit implementation of the <see cref="IEmailService"/> interface.
/// Sends HTML emails via SMTP for approval requests, approval decisions, and memo publications.
///
/// MailKit is the recommended cross-platform SMTP library for .NET. It supports:
/// - SSL/TLS and STARTTLS
/// - SMTP authentication
/// - HTML and plain-text email bodies
///
/// Configuration is read from SmtpSettings (appsettings.json section: "SmtpSettings").
/// </summary>
public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes the email service with SMTP configuration.
    /// </summary>
    /// <param name="smtpSettings">SMTP server configuration injected via IOptions pattern.</param>
    /// <param name="logger">Logger for recording email send attempts and failures.</param>
    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SendApprovalRequestAsync(Memo memo, string baseUrl, CancellationToken ct = default)
    {
        // Send an approval request email to each assigned approver
        foreach (var step in memo.ApprovalSteps)
        {
            var subject = $"Action Required: Approval requested for memo {memo.MemoNumber}";

            // Build the HTML email body with action links
            var approveLink = $"{baseUrl}/memo/{memo.Id}/approve";
            var rejectLink = $"{baseUrl}/memo/{memo.Id}/reject";
            var viewLink = $"{baseUrl}/memo/{memo.Id}";

            var htmlBody = $@"
<html><body style='font-family: Arial, sans-serif; color: #333;'>
<h2>Approval Request</h2>
<p>Dear {step.ApproverName},</p>
<p>You have been asked to review and approve the following memo:</p>
<table style='border-collapse: collapse; width: 100%;'>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Memo Number</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{memo.MemoNumber}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Title</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(memo.Title)}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Author</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(memo.AuthorName)}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Date</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{memo.DateCreated:yyyy-MM-dd HH:mm} UTC</td></tr>
</table>
<br/>
<p>Please review the memo and take one of the following actions:</p>
<p>
  <a href='{viewLink}' style='background:#0d6efd;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;margin-right:10px;'>View Memo</a>
  <a href='{approveLink}' style='background:#198754;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;margin-right:10px;'>Approve</a>
  <a href='{rejectLink}' style='background:#dc3545;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;'>Reject</a>
</p>
<p style='color:#888;font-size:12px;'>This is an automated notification from the Corporate Memo System.</p>
</body></html>";

            var textBody = $"Approval requested for memo {memo.MemoNumber}: {memo.Title}\nAuthor: {memo.AuthorName}\nView memo: {viewLink}";

            await SendEmailAsync(step.ApproverEmail, step.ApproverName, subject, htmlBody, textBody, ct);
        }
    }

    /// <inheritdoc/>
    public async Task SendApprovalDecisionAsync(Memo memo, string approverName, bool approved, string? comment, string baseUrl, CancellationToken ct = default)
    {
        var decision = approved ? "Approved" : "Rejected";
        var decisionColor = approved ? "#198754" : "#dc3545";
        var subject = $"Memo {memo.MemoNumber} has been {decision}";
        var viewLink = $"{baseUrl}/memo/{memo.Id}";

        var htmlBody = $@"
<html><body style='font-family: Arial, sans-serif; color: #333;'>
<h2>Memo {decision}</h2>
<p>Dear {System.Net.WebUtility.HtmlEncode(memo.AuthorName)},</p>
<p>Your memo has been <strong style='color:{decisionColor};'>{decision}</strong> by {System.Net.WebUtility.HtmlEncode(approverName)}.</p>
<table style='border-collapse: collapse; width: 100%;'>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Memo Number</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{memo.MemoNumber}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Title</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(memo.Title)}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Decision</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;color:{decisionColor};'><strong>{decision}</strong></td></tr>
  {(string.IsNullOrEmpty(comment) ? "" : $"<tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Comment</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(comment)}</td></tr>")}
</table>
<br/>
<a href='{viewLink}' style='background:#0d6efd;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;'>View Memo</a>
<p style='color:#888;font-size:12px;margin-top:20px;'>This is an automated notification from the Corporate Memo System.</p>
</body></html>";

        var textBody = $"Memo {memo.MemoNumber} ({memo.Title}) has been {decision} by {approverName}.{(string.IsNullOrEmpty(comment) ? "" : $"\nComment: {comment}")}\nView: {viewLink}";

        await SendEmailAsync(memo.AuthorEmail, memo.AuthorName, subject, htmlBody, textBody, ct);
    }

    /// <inheritdoc/>
    public async Task SendMemoPublishedAsync(Memo memo, string baseUrl, CancellationToken ct = default)
    {
        var subject = $"New Memo Published: {memo.MemoNumber} — {memo.Title}";
        var viewLink = $"{baseUrl}/memo/{memo.Id}";

        var htmlBody = $@"
<html><body style='font-family: Arial, sans-serif; color: #333;'>
<h2>New Memo Published</h2>
<p>A new memo has been published that you are a recipient of:</p>
<table style='border-collapse: collapse; width: 100%;'>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Memo Number</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{memo.MemoNumber}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Title</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(memo.Title)}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Author</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(memo.AuthorName)}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; background: #f8f9fa;'><strong>Published</strong></td>
      <td style='padding: 8px; border: 1px solid #ddd;'>{memo.ApprovedDate?.ToString("yyyy-MM-dd HH:mm") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")} UTC</td></tr>
</table>
<br/>
<a href='{viewLink}' style='background:#0dcaf0;color:#333;padding:10px 20px;text-decoration:none;border-radius:4px;'>Read Memo</a>
<p style='color:#888;font-size:12px;margin-top:20px;'>This is an automated notification from the Corporate Memo System.</p>
</body></html>";

        var textBody = $"New memo published: {memo.MemoNumber} — {memo.Title}\nAuthor: {memo.AuthorName}\nRead it here: {viewLink}";

        // Send to all To recipients
        foreach (var recipient in memo.ToRecipients)
        {
            await SendEmailAsync(recipient, recipient, subject, htmlBody, textBody, ct);
        }

        // Send to all CC recipients
        foreach (var recipient in memo.CcRecipients)
        {
            await SendEmailAsync(recipient, recipient, subject, htmlBody, textBody, ct);
        }
    }

    /// <summary>
    /// Internal helper method that builds the MimeMessage and sends it via MailKit SMTP client.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="toName">The recipient's display name.</param>
    /// <param name="subject">The email subject line.</param>
    /// <param name="htmlBody">The HTML version of the email body.</param>
    /// <param name="textBody">The plain-text fallback for email clients that don't render HTML.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        try
        {
            // Build the MIME message using MimeKit's fluent API
            var message = new MimeMessage();

            // Set the From address (from configuration)
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));

            // Set the To address
            message.To.Add(new MailboxAddress(toName, toEmail));

            message.Subject = subject;

            // Create a multipart/alternative body containing both HTML and plain text
            // Email clients will display the HTML version if they support it,
            // and fall back to the text version if they don't
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Connect to the SMTP server and send the message
            using var client = new SmtpClient();

            // Connect using STARTTLS if SSL is not configured (port 587 typical)
            await client.ConnectAsync(
                _smtpSettings.Host,
                _smtpSettings.Port,
                _smtpSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                ct);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_smtpSettings.Username))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, ct);
            }

            // Send the email
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email '{Subject}' sent successfully to {Recipient}", subject, toEmail);
        }
        catch (Exception ex)
        {
            // Log the full error with recipient and subject for debugging
            // Per requirements: email failures must not roll back primary operations
            _logger.LogError(ex, "Failed to send email '{Subject}' to {Recipient}. Host: {Host}:{Port}",
                subject, toEmail, _smtpSettings.Host, _smtpSettings.Port);

            // Re-throw so callers can handle appropriately (e.g., log and continue)
            throw;
        }
    }
}
