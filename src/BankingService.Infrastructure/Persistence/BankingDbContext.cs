using BankingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Infrastructure.Persistence;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();

    // TODO: DbSet<Transaction> Transactions added when Transaction entity is implemented

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}