namespace CorporateMemo.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration class for SMTP email settings.
/// Bound to the "SmtpSettings" section in appsettings.json.
/// The options pattern (IOptions&lt;SmtpSettings&gt;) is used to inject this configuration
/// into services that need to send emails (EmailService).
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Gets or sets the SMTP server hostname or IP address.
    /// Example: "smtp.office365.com" or "smtp.gmail.com"
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP server port number.
    /// Common values: 587 (STARTTLS), 465 (SSL), 25 (unencrypted — not recommended).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets the SMTP authentication username.
    /// Usually the email address of the sending account.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP authentication password.
    /// Never commit real passwords to source control — use environment variable overrides.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the "From" email address for outgoing emails.
    /// Example: "memos@company.com"
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the "From" display name shown in email clients.
    /// Example: "Corporate Memo System"
    /// </summary>
    public string FromName { get; set; } = "Corporate Memo System";

    /// <summary>
    /// Gets or sets whether to use SSL/TLS for the connection.
    /// Set to false if using STARTTLS (port 587).
    /// </summary>
    public bool UseSsl { get; set; } = false;
}
