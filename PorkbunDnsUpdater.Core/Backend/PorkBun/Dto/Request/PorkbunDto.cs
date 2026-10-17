using Newtonsoft.Json;

namespace PorkbunDnsUpdater.Backend.PorkBun.Dto.Request
{
    public class PorkbunDto
    {
        [JsonProperty("apikey")]
        public required string Apikey { get; set; }

        [JsonProperty("secretapikey")]
        public required string Secretapikey {  get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("ttl")]
        public string? Ttl { get; set; }
    }
}
