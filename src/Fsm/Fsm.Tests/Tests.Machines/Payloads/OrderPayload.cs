using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Machines.Payloads
{
    public class OrderPayload
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? TrackingNumber { get; set; }
    }
}
