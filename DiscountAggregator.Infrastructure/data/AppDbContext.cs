using DiscountAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductPriceHistory> ProductPriceHistories => Set<ProductPriceHistory>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserCategorySubscription> UserCategorySubscriptions => Set<UserCategorySubscription>();
        public DbSet<SearchQuery> SearchQueries => Set<SearchQuery>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Source).HasMaxLength(64);
                entity.Property(d => d.ExternalId).HasMaxLength(128);
                entity.Property(d => d.Title).HasMaxLength(300);
                entity.Property(d => d.Brand).HasMaxLength(150);
                entity.Property(d => d.Url).HasMaxLength(1024);
                entity.HasIndex(d => new { d.Source, d.ExternalId }).IsUnique();
            });

            modelBuilder.Entity<ProductPriceHistory>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasOne(p => p.Product)
                    .WithMany(p => p.PriceHistory)
                    .HasForeignKey(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(p => new { p.ProductId, p.RecordedAtUtc });
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).HasMaxLength(100);
                entity.HasIndex(u => u.LastActivityAtUtc);
            });


            modelBuilder.Entity<UserCategorySubscription>(entity =>
            {
                entity.HasKey(x => new { x.UserId, x.Keyword, x.SourceFilter });
                entity.Property(x => x.Keyword).HasMaxLength(200);
                entity.Property(x => x.SourceFilter).HasMaxLength(50);
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => new { x.UserId, x.IsActive });
            });

            modelBuilder.Entity<SearchQuery>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Keyword).HasMaxLength(200);
                entity.Property(q => q.SourceFilter).HasMaxLength(50);
                entity.Property(q => q.KeywordNormalized).HasMaxLength(200);
                entity.HasIndex(q => new { q.UserId, q.QueriedAtUtc });
            });
        }
    }
}

