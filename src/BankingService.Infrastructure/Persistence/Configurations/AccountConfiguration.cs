using BankingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingService.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.AccountId);

        builder.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.LastName).IsRequired().HasMaxLength(100);

        builder.Property(a => a.Iban).IsRequired().HasMaxLength(34);
        builder.HasIndex(a => a.Iban).IsUnique();

        builder.Property(a => a.Currency).HasConversion<string>().IsRequired();

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("BalanceAmount").HasPrecision(18, 4);
            money.Property(m => m.Currency).HasColumnName("BalanceCurrency").HasConversion<string>();
        });

        builder.Property(a => a.IsActive).HasDefaultValue(true);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
