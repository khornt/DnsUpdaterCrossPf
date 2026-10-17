using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorkbunDnsUpdater.Models
{
    public class Record
    {
        public required string Domain { get; set; }
        public required string HostName { get; set; }        
    }
}
