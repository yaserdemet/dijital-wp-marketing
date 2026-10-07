using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class OrderStatusNotificationConfiguration : IEntityTypeConfiguration<OrderStatusNotification>
{
    public void Configure(EntityTypeBuilder<OrderStatusNotification> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(osn => osn.Id);

        builder.Property(osn => osn.CompanyId)
            .IsRequired();

        builder.Property(osn => osn.OrderStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(osn => osn.OrderStatusDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(osn => osn.WhatsAppTemplateId)
            .HasMaxLength(100);

        builder.Property(osn => osn.AdditionalSettings)
            .HasMaxLength(2000);

        // Configure OrderStatusNotification relationships
        builder
            .HasOne(osn => osn.Company)
            .WithMany()
            .HasForeignKey(osn => osn.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure OrderStatusNotification unique constraint
        builder
            .HasIndex(osn => new { osn.CompanyId, osn.OrderStatus })
            .IsUnique();

        // Performance indexes for OrderStatusNotification
        builder
            .HasIndex(osn => osn.CompanyId)
            .HasDatabaseName("IX_OrderStatusNotifications_CompanyId_Performance");

        builder
            .HasIndex(osn => osn.IsActive)
            .HasDatabaseName("IX_OrderStatusNotifications_IsActive_Performance");
    }
}
