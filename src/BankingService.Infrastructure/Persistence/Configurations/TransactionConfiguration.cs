using BankingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingService.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.AccountId).IsRequired();
        builder.HasIndex(t => t.AccountId);

        builder.Property(t => t.Type).HasConversion<string>().IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 4);
            money.Property(m => m.Currency).HasColumnName("Currency").HasConversion<string>();
        });

        builder.Property(t => t.Description).HasMaxLength(500);

        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
