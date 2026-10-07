using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BulkMessageRecipientConfiguration : IEntityTypeConfiguration<BulkMessageRecipient>
{
    public void Configure(EntityTypeBuilder<BulkMessageRecipient> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(bmr => bmr.Id);

        builder.Property(bmr => bmr.SentChannels)
            .HasMaxLength(500);

        builder.Property(bmr => bmr.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bmr => bmr.Cost)
            .HasColumnType("decimal(10,2)");

        builder.Property(bmr => bmr.ErrorMessage)
            .HasMaxLength(500);

        // Configure BulkMessageRecipient relationships
        builder
            .HasOne(bmr => bmr.BulkMessage)
            .WithMany(bm => bm.Recipients)
            .HasForeignKey(bmr => bmr.BulkMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(bmr => bmr.Customer)
            .WithMany()
            .HasForeignKey(bmr => bmr.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for BulkMessageRecipient
        builder
            .HasIndex(bmr => bmr.BulkMessageId)
            .HasDatabaseName("IX_BulkMessageRecipients_BulkMessageId_Performance");

        builder
            .HasIndex(bmr => bmr.Status)
            .HasDatabaseName("IX_BulkMessageRecipients_Status_Performance");

        builder
            .HasIndex(bmr => new { bmr.BulkMessageId, bmr.Status })
            .HasDatabaseName("IX_BulkMessageRecipients_BulkMessage_Status_Performance");
    }
}
