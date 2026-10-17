using PorkbunDnsUpdater.Backend.PorkBun.Dto.Response;
using PorkbunDnsUpdater.Backend.PorkBun.Dto.Response.JsonToCSharp;
using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.ViewModels;

namespace PorkbunDnsUpdater.Backend.PorkBun.WebClient
{
    public interface IPorkbunHttpClient
    {
        Task<PingResponse?> Ping(DnsType dnsType, CancellationToken ct);

        Task<PorkbunRecordResponse?> GetPorkbunRecord(Record record, DnsType type,CancellationToken ct);
        Task<PorkbunRecordResponse?> UpdatePorkbunRecord(Record record, DnsType type, string myIp, CancellationToken ct);
    }
}
