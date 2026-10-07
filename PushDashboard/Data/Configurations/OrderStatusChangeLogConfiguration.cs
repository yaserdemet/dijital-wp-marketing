using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class OrderStatusChangeLogConfiguration : IEntityTypeConfiguration<OrderStatusChangeLog>
{
    public void Configure(EntityTypeBuilder<OrderStatusChangeLog> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(oscl => oscl.Id);

        builder.Property(oscl => oscl.CompanyId)
            .IsRequired();

        builder.Property(oscl => oscl.OrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oscl => oscl.OrderNumber)
            .HasMaxLength(100);

        builder.Property(oscl => oscl.CustomerEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(oscl => oscl.CustomerName)
            .HasMaxLength(200);

        builder.Property(oscl => oscl.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(oscl => oscl.OldStatus)
            .HasMaxLength(100);

        builder.Property(oscl => oscl.NewStatus)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oscl => oscl.NewStatusDisplayName)
            .HasMaxLength(200);

        builder.Property(oscl => oscl.OrderAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(oscl => oscl.OrderCurrency)
            .HasMaxLength(10);

        builder.Property(oscl => oscl.WebhookPayload)
            .HasMaxLength(4000);

        builder.Property(oscl => oscl.NotificationChannels)
            .HasMaxLength(500);

        builder.Property(oscl => oscl.NotificationError)
            .HasMaxLength(1000);

        // Configure OrderStatusChangeLog relationships
        builder
            .HasOne(oscl => oscl.Company)
            .WithMany()
            .HasForeignKey(oscl => oscl.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for OrderStatusChangeLog
        builder
            .HasIndex(oscl => oscl.CompanyId)
            .HasDatabaseName("IX_OrderStatusChangeLogs_CompanyId_Performance");

        builder
            .HasIndex(oscl => oscl.OrderId)
            .HasDatabaseName("IX_OrderStatusChangeLogs_OrderId_Performance");

        builder
            .HasIndex(oscl => oscl.StatusChangedAt)
            .HasDatabaseName("IX_OrderStatusChangeLogs_StatusChangedAt_Performance");

        builder
            .HasIndex(oscl => new { oscl.CompanyId, oscl.StatusChangedAt })
            .HasDatabaseName("IX_OrderStatusChangeLogs_Company_Date_Performance");

        builder
            .HasIndex(oscl => new { oscl.CompanyId, oscl.NewStatus, oscl.StatusChangedAt })
            .HasDatabaseName("IX_OrderStatusChangeLogs_Company_Status_Date_Performance");
    }
}
