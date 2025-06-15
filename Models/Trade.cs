using System;

namespace tradex_backend.Models
{
    public class Trade
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double Price { get; set; }
        public DateTime ExecutedAt { get; set; }
        public double RealizedPnL { get; set; }

        public int BuyerId { get; set; }
        public User Buyer { get; set; }

        public int SellerId { get; set; }
        public User Seller { get; set; }
    }
}
