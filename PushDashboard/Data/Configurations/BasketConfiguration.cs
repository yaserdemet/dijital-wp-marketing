using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BasketConfiguration : IEntityTypeConfiguration<Basket>
{
    public void Configure(EntityTypeBuilder<Basket> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.CompanyId)
            .IsRequired();

        builder.Property(b => b.GuidBasketId)
            .HasMaxLength(100);

        builder.Property(b => b.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.CustomerEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.TotalAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.TotalTax)
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.ShippingCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10);

        // Configure Basket relationships
        builder
            .HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Basket index for ExternalId and CompanyId (performance only, not unique)
        builder
            .HasIndex(b => new { b.ExternalId, b.CompanyId })
            .HasDatabaseName("IX_Baskets_ExternalId_CompanyId_Performance");

        // Performance indexes
        builder
            .HasIndex(b => b.CompanyId)
            .HasDatabaseName("IX_Baskets_CompanyId_Performance");

        builder
            .HasIndex(b => b.BasketDate)
            .HasDatabaseName("IX_Baskets_BasketDate_Performance");
    }
}
