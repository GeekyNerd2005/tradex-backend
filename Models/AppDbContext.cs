using Microsoft.EntityFrameworkCore;
using tradex_backend.Models;

namespace tradex_backend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
        // ✅ Enable WAL mode to allow concurrent reads/writes in SQLite
        if (Database.IsSqlite())
        {
            Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders { get; set; }
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Holding> Holdings { get; set; }
    public DbSet<PortfolioSnapshot> PortfolioSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trade>()
            .HasOne(t => t.Buyer)
            .WithMany()
            .HasForeignKey(t => t.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trade>()
            .HasOne(t => t.Seller)
            .WithMany()
            .HasForeignKey(t => t.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }                    
}
