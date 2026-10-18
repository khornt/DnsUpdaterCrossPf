using PorkbunDnsUpdater.Core.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Core.Services
{
    public class PorkbunUpdaterService
    {

        private readonly IPorkbunHttpClient _httpClient;

        public PorkbunUpdaterService(IPorkbunHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> InitDnsUpdaterdater(Record record, DnsType dnsType, IProgress<StatusReport> report, CancellationToken ct)
        {
           
            var realIp = await _httpClient.Ping(dnsType, ct);

            if (realIp?.YourIp == null)
            {
                var reportBack = "Your public IP is null";
                report.Report(new StatusReport { Content = reportBack });
                return null;
            }

            // ReSharper disable once InconsistentNaming
            var IPvX = DnsTypeToString(dnsType);

            if (realIp?.YourIp != null && realIp?.Status != "Error")
            {
                var reportBack = "Your public " + IPvX + " is: " + realIp?.YourIp;
                report.Report(new StatusReport { Content = reportBack });
            }

            if (realIp == null | realIp?.YourIp == null)
            {
                var reportBack = "Could not get " + IPvX + " address";
                report.Report(new StatusReport { Content = reportBack });
                return null;
            }

            var responseDto = await _httpClient.GetPorkbunRecord(record, dnsType, ct);

            if (responseDto == null)
            {
                var reportBack = "Initialization faild with empty response from Porkbun";
                report.Report(new StatusReport { Content = reportBack });
                return null;
            }
            else if (responseDto?.Status == "ERROR")
            {
                var reportBack = $"Error getting record from Porkbun: {responseDto.Message} ";
                report.Report(new StatusReport { Content = reportBack });
                return null;
            }

            if (responseDto == null || responseDto?.Records?.Count == 0)
            {
                var reportBack = "Initialization faild with empty response from Porkbun";
                report.Report(new StatusReport { Content = reportBack });
            }
            else if (responseDto?.Status == "ERROR")
            {
                var reportBack = $"Error getting record from Porkbun: {responseDto.Message} ";
                report.Report(new StatusReport { Content = reportBack });
            }

            var currentDnsIP = responseDto?.Records?.First()?.Content;

            if (!string.IsNullOrEmpty(currentDnsIP))
            {
                var reportBack = $"Your DNS  {IPvX}  is: { currentDnsIP.ToString()}";
                report.Report(new StatusReport { Content = reportBack });
            }
            else
            {
                var reportBack = $"Failed retrieving you {IPvX} DNS record";
                report.Report(new StatusReport { Content = reportBack });
                return null;
            }

            if (realIp != null && realIp.YourIp != currentDnsIP)
            {
                var reportBack = $"Updating DNS {IPvX} PorkbunRecord";
                report.Report(new StatusReport { Content = reportBack });

                var response = await _httpClient.UpdatePorkbunRecord(record, dnsType, realIp.YourIp, ct);

                if (response != null && response.Status == "SUCCESS")
                {
                    report.Report(new StatusReport { Content = $"Done Update {IPvX}!"});
                }
            }

            return realIp?.YourIp;
        }

        

        public async Task ContinuouslyUpdateIP(Record record, DnsType dnsType, string ip, int interval, IProgress<StatusReport> report, IProgress<ProgressReport> progress, CancellationToken ct)
        {
            var intervalInSecounds = interval * 60;
            var runIt = true;
            var IPvX = DnsTypeToString(dnsType);

            var nextUpdateDue = DateTimeOffset.UtcNow.AddSeconds(intervalInSecounds);

            while (runIt)
            {
                var justNow = DateTimeOffset.UtcNow;

                if (nextUpdateDue < justNow)
                {
                    report.Report(new StatusReport { Content = $"Just checking {IPvX}"});
                    nextUpdateDue = DateTimeOffset.UtcNow.AddSeconds(intervalInSecounds);
                    
                    var realIP = await _httpClient.Ping(dnsType, ct);
                                                          
                    if (realIP?.YourIp != null && realIP.YourIp != ip)
                    {
                        report.Report(new StatusReport { Content = $"{IPvX} has changed!!" });

                        var updateResponse = await _httpClient.UpdatePorkbunRecord(record, dnsType, realIP.YourIp, ct);

                        if (updateResponse?.Status == "SUCCESS")
                        {
                            var reportBack = $"New {IPvX} : {realIP.YourIp}";
                            report.Report(new StatusReport { Content = reportBack });

                            ip = realIP.YourIp;
                            progress.Report(new ProgressReport { DnsType = dnsType, IP = ip });
                        }
                        else
                        {
                            var message = updateResponse?.Message ?? "";
                            var reportBack = $"Eror updating {IPvX} DNS record: {message}";
                            report.Report(new StatusReport { Content = reportBack });
                        }
                    }
                    else if (realIP?.YourIp == null)
                    {
                        var reportBack = $"Missing {IPvX} from Ping request";
                        report.Report(new StatusReport { Content = reportBack });
                    }                    
                }

                ct.ThrowIfCancellationRequested();
                await Task.Delay(1000, ct);
            }
        }


        private string DnsTypeToString(DnsType dnsType)
        {

            switch (dnsType) 
            {
                case DnsType.A:
                    return "IPv4";
                case DnsType.AAAA:
                    return "IPv6";
                default:
                    return "";

           }
       }
    }
}
