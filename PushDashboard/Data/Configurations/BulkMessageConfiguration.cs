using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BulkMessageConfiguration : IEntityTypeConfiguration<BulkMessage>
{
    public void Configure(EntityTypeBuilder<BulkMessage> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(bm => bm.Id);

        builder.Property(bm => bm.CompanyId)
            .IsRequired();

        builder.Property(bm => bm.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(bm => bm.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bm => bm.Description)
            .HasMaxLength(1000);

        builder.Property(bm => bm.CustomerFiltersJson)
            .HasMaxLength(2000);

        builder.Property(bm => bm.ChannelSettingsJson)
            .HasMaxLength(2000);

        builder.Property(bm => bm.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bm => bm.EstimatedCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(bm => bm.ActualCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(bm => bm.ErrorMessage)
            .HasMaxLength(1000);

        // Configure BulkMessage relationships
        builder
            .HasOne(bm => bm.Company)
            .WithMany()
            .HasForeignKey(bm => bm.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(bm => bm.User)
            .WithMany()
            .HasForeignKey(bm => bm.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Performance indexes for BulkMessage
        builder
            .HasIndex(bm => bm.CompanyId)
            .HasDatabaseName("IX_BulkMessages_CompanyId_Performance");

        builder
            .HasIndex(bm => bm.CreatedAt)
            .HasDatabaseName("IX_BulkMessages_CreatedAt_Performance");

        builder
            .HasIndex(bm => bm.Status)
            .HasDatabaseName("IX_BulkMessages_Status_Performance");

        builder
            .HasIndex(bm => new { bm.CompanyId, bm.Status, bm.CreatedAt })
            .HasDatabaseName("IX_BulkMessages_Company_Status_Date_Performance");
    }
}
