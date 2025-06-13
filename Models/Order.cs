using System.ComponentModel.DataAnnotations;
using tradex_backend.Models; // To access enums

namespace tradex_backend.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Symbol { get; set; } = string.Empty;

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public double Quantity { get; set; }

        public double? Price { get; set; }

        public double? StopPrice { get; set; }
        public double? ExecutedPrice { get; set; }
        public DateTime? ExecutedAt { get; set; }


        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public double OriginalQuantity { get; set; }  // Add this to track original quantity

    }
}
