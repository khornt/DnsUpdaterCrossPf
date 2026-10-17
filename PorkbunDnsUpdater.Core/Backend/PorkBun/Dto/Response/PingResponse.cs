using Newtonsoft.Json;

namespace PorkbunDnsUpdater.Backend.PorkBun.Dto.Response
{
    public class PingResponse
    {

        [JsonProperty("status")]
        public string? Status {  get; set; }

        [JsonProperty("yourIp")]
        public string? YourIp { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }
}
