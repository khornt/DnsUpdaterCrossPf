using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorkbunDnsUpdater.Backend.PorkBun.Dto.Response
{
    using System;
    using System.Collections.Generic;

    namespace JsonToCSharp
    {
        public class PorkbunRecord
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Type { get; set; }
            public string? Content { get; set; }
            public string? Ttl { get; set; }
            public string? Prio { get; set; }
            public string? Notes { get; set; }
        }

        public class PorkbunRecordResponse
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public string? Cloudflare { get; set; }
            public List<PorkbunRecord>? Records { get; set; }
        }
    }
}
