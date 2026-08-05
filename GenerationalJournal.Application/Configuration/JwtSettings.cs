namespace GenerationalJournal.Application.Configuration;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GenerationalJournal";
    public string Audience { get; set; } = "GenerationalJournal";
    public int ExpiryMinutes { get; set; } = 1440;
}
