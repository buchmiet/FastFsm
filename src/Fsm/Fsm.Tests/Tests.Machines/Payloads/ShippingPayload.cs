using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Machines.Payloads
{
    public class ShippingPayload : OrderPayload
    {
        public string Carrier { get; set; } = "";
        public DateTime EstimatedDelivery { get; set; }
    }
}
