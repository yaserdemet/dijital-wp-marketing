using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.CompanyId)
            .IsRequired();

        builder.Property(ct => ct.TransactionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(ct => ct.Amount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(ct => ct.BalanceBefore)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(ct => ct.BalanceAfter)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(ct => ct.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ct => ct.PaytrOrderId)
            .HasMaxLength(100);

        builder.Property(ct => ct.PaytrTransactionId)
            .HasMaxLength(100);

        builder.Property(ct => ct.CardLastFour)
            .HasMaxLength(4);

        builder.Property(ct => ct.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ct => ct.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(ct => ct.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        // Relationships
        builder
            .HasOne(ct => ct.Company)
            .WithMany()
            .HasForeignKey(ct => ct.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ct => ct.CreatedByUser)
            .WithMany()
            .HasForeignKey(ct => ct.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder
            .HasIndex(ct => ct.CompanyId)
            .HasDatabaseName("IX_CreditTransactions_CompanyId");

        builder
            .HasIndex(ct => ct.CreatedAt)
            .HasDatabaseName("IX_CreditTransactions_CreatedAt");

        builder
            .HasIndex(ct => ct.PaytrOrderId)
            .HasDatabaseName("IX_CreditTransactions_PaytrOrderId");

        builder
            .HasIndex(ct => new { ct.CompanyId, ct.CreatedAt })
            .HasDatabaseName("IX_CreditTransactions_Company_Date");
    }
}
