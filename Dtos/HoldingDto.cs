using tradex_backend.Models;
namespace tradex_backend.Dtos
{
    public class HoldingDto
    {
        public string Symbol { get; set; }
        public double Quantity { get; set; }
        public double AverageBuyPrice { get; set; }
        public double CurrentPrice { get; set; }
        public double UnrealizedPnL { get; set; }
    }

    public class PortfolioDto
    {
        public string UserId { get; set; }
        public List<HoldingDto> Holdings { get; set; } = new();
        public double TotalUnrealizedPnL => Holdings.Sum(h => h.UnrealizedPnL);
    }
}
