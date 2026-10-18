namespace PorkbunDnsUpdater.Core.Models
{
    public class Record
    {
        public required string Domain { get; set; }
        public required string HostName { get; set; }        
    }
}
