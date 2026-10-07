using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class WhatsAppWebhookEventConfiguration : IEntityTypeConfiguration<WhatsAppWebhookEvent>
{
    public void Configure(EntityTypeBuilder<WhatsAppWebhookEvent> builder)
    {
        builder.ToTable("WhatsAppWebhookEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanyId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EventSubType)
            .HasMaxLength(100);

        builder.Property(e => e.GupshupMessageId)
            .HasMaxLength(255);

        builder.Property(e => e.RawPayload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ReceivedAt)
            .IsRequired();

        builder.Property(e => e.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.SourceIp)
            .HasMaxLength(50);

        // Indexes for performance
        builder.HasIndex(e => e.CompanyId)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_CompanyId");

        builder.HasIndex(e => e.GupshupMessageId)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_GupshupMessageId");

        builder.HasIndex(e => e.IsProcessed)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_IsProcessed");

        builder.HasIndex(e => e.ReceivedAt)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_ReceivedAt");

        builder.HasIndex(e => new { e.CompanyId, e.ReceivedAt })
            .HasDatabaseName("IX_WhatsAppWebhookEvents_CompanyId_ReceivedAt");

        // Relationships
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.WhatsAppMessage)
            .WithMany(m => m.WebhookEvents)
            .HasForeignKey(e => e.WhatsAppMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
