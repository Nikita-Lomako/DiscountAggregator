using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DiscountAggregator.Infrastructure.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connection = Environment.GetEnvironmentVariable("DA_DefaultConnection");
            if (string.IsNullOrWhiteSpace(connection))
                throw new Exception("DA_DefaultConnection is not configured for design-time operations");
            optionsBuilder.UseNpgsql(connection);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

