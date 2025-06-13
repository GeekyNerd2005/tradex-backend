using Microsoft.EntityFrameworkCore.Query;

using tradex_backend.Models;
namespace tradex_backend.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public double Quantity { get; set; }
        public double? Price { get; set; }
        public double? StopPrice { get; set; }
        public OrderStatus Status { get; set; }
        public double? ExecutedPrice { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }
}