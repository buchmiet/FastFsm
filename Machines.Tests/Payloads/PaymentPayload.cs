using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machines.Tests.Payloads
{
    public class PaymentPayload : OrderPayload
    {
        public string PaymentMethod { get; set; } = "";
        public DateTime PaymentDate { get; set; }
    }
}
