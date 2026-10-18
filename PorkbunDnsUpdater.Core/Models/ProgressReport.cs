using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Core.Models
{
    public class ProgressReport
    {                
        public string? IP { get; set; }
        public DnsType DnsType { get; set; }
    }
}
