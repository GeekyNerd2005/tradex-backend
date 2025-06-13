// Models/PortfolioSnapshot.cs
public class PortfolioSnapshot
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public double TotalValue { get; set; }
    public DateTime Timestamp { get; set; }
}
