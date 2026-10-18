namespace PorkbunDnsUpdater.Core.Models;

public class AppConfig
{
    public string PorkbunApiUrl { get; set; } =
        "https://api.porkbun.com/api/json/v3";

    public string PorkbunApiUrlv4 { get; set; } =
        "https://api-ipv4.porkbun.com/api/json/v3";

    public string PorkbunApiKey { get; set; } = "";

    public string PorkbunApiSecret { get; set; } = "";
}
