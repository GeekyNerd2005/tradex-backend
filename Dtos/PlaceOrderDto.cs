using tradex_backend.Models;
namespace tradex_backend.Dtos
{
    public class PlaceOrderDto
    {
        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public double Quantity { get; set; }
        public double? Price { get; set; }
        public double? StopPrice { get; set; }
    }
}
