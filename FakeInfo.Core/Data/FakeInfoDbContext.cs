using Microsoft.EntityFrameworkCore;

namespace FakeInfo.Core.Data;

public class FakeInfoDbContext : DbContext
{
    public FakeInfoDbContext(DbContextOptions<FakeInfoDbContext> options)
        : base(options)
    {
    }

    public DbSet<GeneratedPersonEntity> GeneratedPersons => Set<GeneratedPersonEntity>();
}