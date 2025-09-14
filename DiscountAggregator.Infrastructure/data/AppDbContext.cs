using DiscountAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Discount> Discounts => Set<Discount>();
        public DbSet<ApiSubscription> ApiSubscriptions => Set<ApiSubscription>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
        public DbSet<QueryLog> QueryLogs => Set<QueryLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Discount>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.Fingerprint).IsUnique();
                entity.Property(d => d.Source).HasMaxLength(64);
                entity.Property(d => d.ExternalId).HasMaxLength(128);
                entity.Property(d => d.Title).HasMaxLength(1024);
                entity.Property(d => d.Brand).HasMaxLength(256);
                entity.Property(d => d.Url).HasMaxLength(1024);
            });

            modelBuilder.Entity<ApiSubscription>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.SourceKey).HasMaxLength(64);
                entity.Property(a => a.Keyword).HasMaxLength(256);
                entity.HasIndex(a => new { a.SourceKey, a.Keyword }).IsUnique();
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => new { u.UserId, u.ApiSubscriptionId }).IsUnique();
                entity.HasOne<ApiSubscription>()
                    .WithMany()
                    .HasForeignKey(u => u.ApiSubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QueryLog>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Keyword).HasMaxLength(256);
                entity.HasIndex(q => new { q.UserId, q.QueriedAtUtc });
            });
        }
    }
}

