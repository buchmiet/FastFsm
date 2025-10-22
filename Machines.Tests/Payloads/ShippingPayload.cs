using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machines.Tests.Payloads
{
    public class ShippingPayload : OrderPayload
    {
        public string Carrier { get; set; } = "";
        public DateTime EstimatedDelivery { get; set; }
    }
}
