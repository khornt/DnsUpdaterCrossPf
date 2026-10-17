using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PorkbunDnsUpdater.Backend.PorkBun.Dto.Request;
using PorkbunDnsUpdater.Backend.PorkBun.Dto.Response;
using PorkbunDnsUpdater.Backend.PorkBun.Dto.Response.JsonToCSharp;
using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.ViewModels;

namespace PorkbunDnsUpdater.Backend.PorkBun.WebClient
{
    public class PorkbunHttpClient : IPorkbunHttpClient
    {
                
        private readonly AppConfig _appConfig;
        private static readonly HttpClient _httpClient = new();
        private static JsonSerializerSettings? _jsonFomatter;

        public PorkbunHttpClient(AppConfig appConfig)
        {
            _appConfig = appConfig;        
            _jsonFomatter = JsonFormatter();
        }


        public async Task<PingResponse?> Ping(DnsType dnsType, CancellationToken ct)
        {
            string apiUrl = "";
            if (DnsType.AAAA == dnsType)
            {
                apiUrl = _appConfig.PorkbunApiUrl + "/ping";
            } else if (DnsType.A == dnsType) 
            {
                apiUrl = _appConfig.PorkbunApiUrlv4 + "/ping";
            }


            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, apiUrl))
                {                   
                    request.Content = new StringContent(CreatePingRequest(), Encoding.UTF8, "application/json");

                    using (var httpResponse = await _httpClient.SendAsync(request, ct))
                    {
                        PingResponse? dtoObject;
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            var response = await httpResponse.Content.ReadAsStringAsync();
                            dtoObject = JsonConvert.DeserializeObject<PingResponse>(response);

                            return dtoObject;
                        }
                        else
                        {
                            var response = await httpResponse.Content.ReadAsStringAsync();
                            dtoObject = JsonConvert.DeserializeObject<PingResponse>(response);
                            if (dtoObject != null) return dtoObject;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }


        public async Task<PorkbunRecordResponse?> GetPorkbunRecord(Record record, DnsType type,CancellationToken ct)
        {                        
            var requestUrl =
                $"{_appConfig.PorkbunApiUrl}/dns/retrieveByNameType/{record.Domain}/{DnsTypeToString(type)}/{record.HostName}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                var stringPayload = CreateRequest();
                request.Content = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                using (var httpResponse = await _httpClient.SendAsync(request, ct))
                {
                    var response = await httpResponse.Content.ReadAsStringAsync(ct);
                    if (httpResponse.IsSuccessStatusCode)
                    {                        
                        var respDto = JsonConvert.DeserializeObject<PorkbunRecordResponse>(response);                                                 
                        return respDto;
                    }
                    else
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<PorkbunRecordResponse>(response);

                        } catch
                        {
                            return new PorkbunRecordResponse
                            {
                                Status = "ERROR",
                                Message = "Failed to deserialize message from Porkbun"
                            };
                        }                                                
                    }
                }
            }         
        }


        public async Task<PorkbunRecordResponse?> UpdatePorkbunRecord(Record record, DnsType type ,string myNewIp, CancellationToken ct)
        {
            try
            {                
                var requestUrl =
                    $"{_appConfig.PorkbunApiUrl}/dns/editByNameType/{record.Domain}/{DnsTypeToString(type)}/{record.HostName}";

                using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
                {
                    var stringPayload = CreateRequest(myNewIp);
                    request.Content = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                    using (var httpResponse = await _httpClient.SendAsync(request, ct))
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync(ct);

                        if (httpResponse.IsSuccessStatusCode)
                        {                            
                            var responseDto = JsonConvert.DeserializeObject<PorkbunRecordResponse>(response);
                            return responseDto; 
                        }
                        else
                        {
                            var responseDto = JsonConvert.DeserializeObject<PorkbunRecordResponse>(response);
                            return responseDto;
                        }
                    }
                }
            }
            catch
            {
                return new PorkbunRecordResponse
                {
                    Status = "ERROR",
                    Message = "Failed to deserialize message from Porkbun"
                };
            }
        }

        private string CreateRequest()
        {
            var dto = new PorkbunDto
            {
                Apikey = _appConfig.PorkbunApiKey,
                Secretapikey = _appConfig.PorkbunApiSecret
            };

            var jsonPayload = JsonConvert.SerializeObject(dto, _jsonFomatter);
            return jsonPayload;
                      
        }

        private string CreateRequest(string myNewIp)
        {

            var dto = new PorkbunDto
            {
                Apikey = _appConfig.PorkbunApiKey,
                Secretapikey = _appConfig.PorkbunApiSecret,
                Content = myNewIp,
                Ttl = "600"
            };

            var jsonPayload = JsonConvert.SerializeObject(dto, _jsonFomatter);
            return jsonPayload;
        }

        private string CreatePingRequest()
        {
            var dto = new PorkbunDto
            {
                Apikey = _appConfig.PorkbunApiKey,
                Secretapikey = _appConfig.PorkbunApiSecret
            };

            return JsonConvert.SerializeObject(dto, _jsonFomatter);             
        }

        private JsonSerializerSettings JsonFormatter()
        {
            var formatter = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
            };

            return formatter;
        }


        private static string DnsTypeToString(DnsType dnsType)
        {

            switch (dnsType)
            {
                case DnsType.A:
                    return "A";
                case DnsType.AAAA:
                    return "AAAA";
                default:
                    return "";

            }
        }


    }
}
