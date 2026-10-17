namespace PorkbunDnsUpdater.Models;

public class AppConfig
{
    public string PorkbunApiUrl => Environment.GetEnvironmentVariable("PORKBUN_API_URL")
        ?? "https://api.porkbun.com/api/json/v3";
    public string PorkbunApiUrlv4 => Environment.GetEnvironmentVariable("PORKBUN_API_URL_V4")
        ?? "https://api-ipv4.porkbun.com/api/json/v3";
    public string PorkbunApiKey => Environment.GetEnvironmentVariable("PORKBUN_API_KEY") ?? "";
    public string PorkbunApiSecret => Environment.GetEnvironmentVariable("PORKBUN_API_SECRET") ?? "";
}
