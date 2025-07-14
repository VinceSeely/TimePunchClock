using Microsoft.EntityFrameworkCore;
using TimeApi.Models;

namespace TimeApi.Services;

public class TimeClockDbContext : DbContext
{
    public TimeClockDbContext(DbContextOptions<TimeClockDbContext> options): base(options)
    {
    }

    public DbSet<PunchEntity> Punchs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PunchEntity
        modelBuilder.Entity<PunchEntity>(entity =>
        {
            entity.HasKey(e => e.PunchId);
            entity.Property(e => e.PunchId).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            // Create an index on PunchIn for improved query performance
            entity.HasIndex(e => e.PunchIn);
        });
    }
}

