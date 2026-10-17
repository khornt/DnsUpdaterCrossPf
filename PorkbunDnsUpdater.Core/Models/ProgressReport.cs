using PorkbunDnsUpdater.ViewModels;

namespace PorkbunDnsUpdater.Models
{
    public class ProgressReport
    {                
        public string? IP { get; set; }
        public DnsType DnsType { get; set; }
    }
}
