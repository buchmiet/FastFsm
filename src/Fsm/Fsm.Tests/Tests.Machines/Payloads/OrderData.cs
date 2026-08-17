namespace Tests.Machines.Payloads
{
    public class OrderData
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Customer { get; set; } = null!;
    }
}
