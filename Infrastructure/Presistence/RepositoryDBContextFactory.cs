using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Presistence
{
    internal class RepositoryDBContextFactory : IDesignTimeDbContextFactory<RepositoryDBContext>
    {
        public RepositoryDBContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RepositoryDBContext>();

            optionsBuilder.UseNpgsql(
                 "Host=localhost;Port=5433;Database=RepositoryDB;Username=postgres;Password=postgres"
            );

            return new RepositoryDBContext(optionsBuilder.Options);

        }
    }
}
