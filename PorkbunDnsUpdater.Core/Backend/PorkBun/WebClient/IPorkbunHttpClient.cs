using PorkbunDnsUpdater.Core.Backend.PorkBun.Dto.Response;
using PorkbunDnsUpdater.Core.Backend.PorkBun.Dto.Response.JsonToCSharp;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Core.Backend.PorkBun.WebClient
{
    public interface IPorkbunHttpClient
    {
        Task<PingResponse?> Ping(DnsType dnsType, CancellationToken ct);

        Task<PorkbunRecordResponse?> GetPorkbunRecord(Record record, DnsType type,CancellationToken ct);
        Task<PorkbunRecordResponse?> UpdatePorkbunRecord(Record record, DnsType type, string myIp, CancellationToken ct);
    }
}
