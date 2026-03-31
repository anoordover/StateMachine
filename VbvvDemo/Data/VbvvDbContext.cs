using Microsoft.EntityFrameworkCore;
using VbvvDemo.Entities;

namespace VbvvDemo.Data;

public class VbvvDbContext : DbContext
{
    public VbvvDbContext(DbContextOptions<VbvvDbContext> options) : base(options)
    {
    }
    
    public DbSet<DossierState> DossierStates { get; set; }
    public DbSet<FileQueueItem> FileQueueItems { get; set; }
    
    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileQueueItem>()
            .Property(p => p.Status)
            .HasConversion<string>();
    }
}