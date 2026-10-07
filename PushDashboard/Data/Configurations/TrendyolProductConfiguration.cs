using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class TrendyolProductConfiguration : IEntityTypeConfiguration<TrendyolProduct>
{
    public void Configure(EntityTypeBuilder<TrendyolProduct> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.StoreId)
            .IsRequired();

        builder.Property(tp => tp.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(tp => tp.ProductId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tp => tp.Href)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(tp => tp.IsSelected)
            .IsRequired();

        // Configure TrendyolProduct relationships
        builder
            .HasOne(tp => tp.Store)
            .WithMany(ts => ts.Products)
            .HasForeignKey(tp => tp.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(tp => tp.JobProducts)
            .WithOne(ctjp => ctjp.Product)
            .HasForeignKey(ctjp => ctjp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for TrendyolProduct
        builder
            .HasIndex(tp => tp.StoreId)
            .HasDatabaseName("IX_TrendyolProducts_StoreId_Performance");

        builder
            .HasIndex(tp => tp.ProductId)
            .HasDatabaseName("IX_TrendyolProducts_ProductId_Performance");

        builder
            .HasIndex(tp => tp.IsSelected)
            .HasDatabaseName("IX_TrendyolProducts_IsSelected_Performance");

        builder
            .HasIndex(tp => new { tp.StoreId, tp.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_TrendyolProducts_StoreId_ProductId_Unique");
    }
}
