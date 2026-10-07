using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class TrendyolStoreConfiguration : IEntityTypeConfiguration<TrendyolStore>
{
    public void Configure(EntityTypeBuilder<TrendyolStore> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(ts => ts.Id);

        builder.Property(ts => ts.CompanyId)
            .IsRequired();

        builder.Property(ts => ts.StoreUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ts => ts.ExternalStoreId)
            .HasMaxLength(100);

        builder.Property(ts => ts.StoreName)
            .HasMaxLength(200);

        builder.Property(ts => ts.SyncStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ts => ts.ErrorMessage)
            .HasMaxLength(500);

        // Configure TrendyolStore relationships
        builder
            .HasOne(ts => ts.Company)
            .WithMany()
            .HasForeignKey(ts => ts.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(ts => ts.Products)
            .WithOne(tp => tp.Store)
            .HasForeignKey(tp => tp.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(ts => ts.TransferJobs)
            .WithOne(ctj => ctj.Store)
            .HasForeignKey(ctj => ctj.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Performance indexes for TrendyolStore
        builder
            .HasIndex(ts => ts.CompanyId)
            .HasDatabaseName("IX_TrendyolStores_CompanyId_Performance");

        builder
            .HasIndex(ts => ts.SyncStatus)
            .HasDatabaseName("IX_TrendyolStores_SyncStatus_Performance");

        builder
            .HasIndex(ts => new { ts.CompanyId, ts.StoreUrl })
            .IsUnique()
            .HasDatabaseName("IX_TrendyolStores_CompanyId_StoreUrl_Unique");
    }
}
