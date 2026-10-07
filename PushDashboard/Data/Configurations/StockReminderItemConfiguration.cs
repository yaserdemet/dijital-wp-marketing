using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class StockReminderItemConfiguration : IEntityTypeConfiguration<StockReminderItem>
{
    public void Configure(EntityTypeBuilder<StockReminderItem> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.ProductName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.StockCode)
            .HasMaxLength(100);

        builder.Property(s => s.Currency)
            .HasMaxLength(10);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.ProductUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.CustomerName)
            .HasMaxLength(200);

        builder.Property(s => s.CustomerEmail)
            .HasMaxLength(200);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Property(s => s.PriceWithTax)
            .HasPrecision(18, 2);

        builder.Property(s => s.VariationBarcode)
            .HasMaxLength(100);

        builder.Property(s => s.VariationStockCode)
            .HasMaxLength(100);

        builder.Property(s => s.AlarmCreatedAt)
            .IsRequired();

        builder.Property(s => s.LastSyncedAt)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Company relationship
        builder
            .HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => new { s.CompanyId, s.ExternalAlarmId })
            .HasDatabaseName("IX_StockReminderItem_Company_ExternalAlarm");

        builder.HasIndex(s => new { s.CompanyId, s.ProductId })
            .HasDatabaseName("IX_StockReminderItem_Company_Product");

        builder.HasIndex(s => s.LastSyncedAt)
            .HasDatabaseName("IX_StockReminderItem_LastSynced");

        builder.HasIndex(s => new { s.CompanyId, s.IsNotified })
            .HasDatabaseName("IX_StockReminderItem_Company_Notified");
    }
}
