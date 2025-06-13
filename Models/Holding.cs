// Models/Holding.cs
using System.ComponentModel.DataAnnotations;

namespace tradex_backend.Models
{
    public class Holding
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Symbol { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public double AverageBuyPrice { get; set; }
        public double LastPrice { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
