namespace tradex_backend.Models
{
    public enum OrderType
    {
        Market,
        Limit,
        StopMarket,
        StopLimit
    }

    public enum OrderSide
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        Pending,
        Executed,
        Cancelled
    }
}