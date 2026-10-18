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

        
public async Task<string?> InitDnsUpdaterdater(
    Record record,
    DnsType dnsType,
    IProgress<StatusReport> report,
    CancellationToken ct)
{
    var ipVersion = DnsTypeToString(dnsType);

    // 1. Retrieve public IP address
    var pingResponse = await _httpClient.Ping(dnsType, ct);

    if (pingResponse == null ||
        !string.Equals(pingResponse.Status, "SUCCESS",
            StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(pingResponse.YourIp))
    {
        report.Report(new StatusReport
        {
            Content = $"Could not retrieve public {ipVersion}: " +
                      $"{pingResponse?.Message ?? "No valid response"}"
        });

        return null;
    }

    var publicIp = pingResponse.YourIp;

    report.Report(new StatusReport
    {
        Content = $"Your public {ipVersion} is: {publicIp}"
    });

    // 2. Retrieve existing DNS record
    var dnsResponse = await _httpClient.GetPorkbunRecord(
        record, dnsType, ct);

    if (dnsResponse == null)
    {
        report.Report(new StatusReport
        {
            Content = $"Failed to retrieve {ipVersion} DNS record: " +
                      "Empty response from Porkbun."
        });

        return null;
    }

    if (!string.Equals(dnsResponse.Status, "SUCCESS",
            StringComparison.OrdinalIgnoreCase))
    {
        report.Report(new StatusReport
        {
            Content = $"Failed to retrieve {ipVersion} DNS record: " +
                      $"{dnsResponse.Message ?? "Unknown error"}"
        });

        return null;
    }

    // 3. Validate DNS record collection
    var dnsRecord = dnsResponse.Records?.FirstOrDefault();

    if (dnsRecord == null ||
        string.IsNullOrWhiteSpace(dnsRecord.Content))
    {
        report.Report(new StatusReport
        {
            Content = $"No valid {ipVersion} DNS record found for " +
                      $"{record.HostName}.{record.Domain}."
        });

        return null;
    }

    var currentDnsIp = dnsRecord.Content;

    report.Report(new StatusReport
    {
        Content = $"Your DNS {ipVersion} is: {currentDnsIp}"
    });

    // 4. No update required
    if (string.Equals(publicIp, currentDnsIp,
            StringComparison.OrdinalIgnoreCase))
    {
        report.Report(new StatusReport
        {
            Content = $"{ipVersion} DNS record is already up to date."
        });

        return publicIp;
    }

    // 5. Update DNS record
    report.Report(new StatusReport
    {
        Content = $"Updating {ipVersion} DNS record..."
    });

    var updateResponse = await _httpClient.UpdatePorkbunRecord(
        record, dnsType, publicIp, ct);

    if (updateResponse == null ||
        !string.Equals(updateResponse.Status, "SUCCESS",
            StringComparison.OrdinalIgnoreCase))
    {
        report.Report(new StatusReport
        {
            Content = $"Failed to update {ipVersion} DNS record: " +
                      $"{updateResponse?.Message ?? "No valid response"}"
        });

        return null;
    }

    report.Report(new StatusReport
    {
        Content = $"{ipVersion} DNS record updated successfully."
    });

    return publicIp;
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
