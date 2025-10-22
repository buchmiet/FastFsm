using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machines.Tests.Payloads
{
    public class OrderPayload
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? TrackingNumber { get; set; }
    }
}
