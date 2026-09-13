namespace AlAmalBusiness.Api.Email
{
    // Bound from the "Email" config section. Hostinger mail: host
    // smtp.hostinger.com, port 465 (SSL on connect) or 587 (STARTTLS),
    // username = the full mailbox address. Password never lives in a tracked
    // appsettings file — user-secrets locally, injected by deploy.yml in
    // production (same as JwtSettings:Key).
    public sealed class EmailSettings
    {
        public const string SectionName = "Email";

        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 465;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;

        // Enabled alone isn't enough: a production deploy whose SMTP secret
        // wasn't set must quietly send nothing, not crash-loop the app.
        public bool IsUsable =>
            Enabled
            && !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(FromAddress);
    }
}
