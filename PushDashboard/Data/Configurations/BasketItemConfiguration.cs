using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BasketItemConfiguration : IEntityTypeConfiguration<BasketItem>
{
    public void Configure(EntityTypeBuilder<BasketItem> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.BasketId)
            .IsRequired();

        builder.Property(bi => bi.GuidBasketItemId)
            .HasMaxLength(100);

        builder.Property(bi => bi.ProductCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bi => bi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bi => bi.ProductImage)
            .HasMaxLength(500);

        builder.Property(bi => bi.UnitPrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(bi => bi.TotalPrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(bi => bi.TaxAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(bi => bi.ShippingCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(bi => bi.Currency)
            .IsRequired()
            .HasMaxLength(10);

        // Configure BasketItem relationships
        builder
            .HasOne(bi => bi.Basket)
            .WithMany(b => b.BasketItems)
            .HasForeignKey(bi => bi.BasketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder
            .HasIndex(bi => bi.BasketId)
            .HasDatabaseName("IX_BasketItems_BasketId_Performance");
    }
}
