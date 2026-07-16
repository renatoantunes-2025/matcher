using Microsoft.EntityFrameworkCore;
using MatchR.Api.Models;

namespace MatchR.Api.Data;

public class MatchRDbContext(DbContextOptions<MatchRDbContext> options) : DbContext(options)
{
    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<SearchRequest> SearchRequests => Set<SearchRequest>();
    public DbSet<SearchResult> SearchResults => Set<SearchResult>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ShareEvent> ShareEvents => Set<ShareEvent>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<Broker>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Creci).HasMaxLength(50);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Broker).WithMany(b => b.Clients).HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Agency>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Property>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.AreaM2).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Agency).WithMany(a => a.Properties).HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Features)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<SearchRequest>(e =>
        {
            e.HasOne(x => x.Client).WithMany(c => c.Searches).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Broker).WithMany().HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PriceMin).HasColumnType("decimal(18,2)");
            e.Property(x => x.PriceMax).HasColumnType("decimal(18,2)");
            e.Property(x => x.MinArea).HasColumnType("decimal(10,2)");
            e.Property(x => x.Features)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<SearchResult>(e =>
        {
            e.HasOne(x => x.SearchRequest).WithMany(s => s.Results).HasForeignKey(x => x.SearchRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Property).WithMany(p => p.SearchResults).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Reasons)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<Favorite>(e =>
        {
            e.HasIndex(x => new { x.BrokerId, x.PropertyId }).IsUnique();
            e.HasOne(x => x.Broker).WithMany().HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Property).WithMany(p => p.Favorites).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShareEvent>(e =>
        {
            e.HasOne(x => x.Client).WithMany(c => c.ShareEvents).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Broker).WithMany().HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Restrict);
            // ClientSetNull (not SetNull) to avoid SQL Server error 1785: Clients already
            // cascades to ShareEvents directly, and to SearchRequests -> ShareEvents would
            // be a second cascade path to the same table, which SQL Server disallows.
            e.HasOne(x => x.SearchRequest).WithMany().HasForeignKey(x => x.SearchRequestId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ImportBatch>(e =>
        {
            e.HasOne(x => x.Broker).WithMany().HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccessRequest>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
        });
    }
}
