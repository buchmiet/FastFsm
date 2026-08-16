using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFsm.Tests.Payloads
{
    public class OrderData
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Customer { get; set; }
    }
}
