using System.Configuration;

namespace PorkbunDnsUpdater.Models
{
    public class AppConfig
    {
        public string PorkbunApiUrl => GetValue(nameof(PorkbunApiUrl));
        public string PorkbunApiUrlv4 => GetValue(nameof(PorkbunApiUrlv4));
        public string PorkbunApiKey => GetValue(nameof(PorkbunApiKey));        
        public string PorkbunApiSecret => GetValue(nameof(PorkbunApiSecret));
        private string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? throw new ConfigurationErrorsException("Missing Configuration");
        }
    }
}

