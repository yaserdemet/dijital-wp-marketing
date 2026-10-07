using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyModuleConfiguration : IEntityTypeConfiguration<CompanyModule>
{
    public void Configure(EntityTypeBuilder<CompanyModule> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.CompanyId)
            .IsRequired();

        builder.Property(cm => cm.PaidAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(cm => cm.TransactionId)
            .HasMaxLength(100);

        builder.Property(cm => cm.PurchasedByUserId)
            .IsRequired();

        // Subscription type configuration
        builder.Property(cm => cm.SubscriptionType)
            .HasDefaultValue(PushDashboard.Models.SubscriptionType.Monthly);

        builder.Property(cm => cm.AutoRenewal)
            .HasDefaultValue(true);

        // Configure CompanyModule unique constraint
        builder
            .HasIndex(cm => new { cm.CompanyId, cm.ModuleId })
            .IsUnique();

        // Configure CompanyModule relationships
        builder
            .HasOne(cm => cm.Company)
            .WithMany(c => c.CompanyModules)
            .HasForeignKey(cm => cm.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cm => cm.Module)
            .WithMany(m => m.CompanyModules)
            .HasForeignKey(cm => cm.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cm => cm.PurchasedByUser)
            .WithMany()
            .HasForeignKey(cm => cm.PurchasedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
