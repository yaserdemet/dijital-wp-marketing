using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class WhatsAppMessageConfiguration : IEntityTypeConfiguration<WhatsAppMessage>
{
    public void Configure(EntityTypeBuilder<WhatsAppMessage> builder)
    {
        builder.ToTable("WhatsAppMessages");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.GupshupMessageId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(w => w.CompanyId)
            .IsRequired();

        builder.Property(w => w.RecipientPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(w => w.TemplateName)
            .HasMaxLength(255);

        builder.Property(w => w.TemplateId)
            .HasMaxLength(255);

        builder.Property(w => w.MessageType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("template");

        builder.Property(w => w.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("pending");

        builder.Property(w => w.ErrorCode)
            .HasMaxLength(100);

        builder.Property(w => w.Source)
            .HasMaxLength(100);

        builder.Property(w => w.SourceReferenceId)
            .HasMaxLength(255);

        builder.Property(w => w.SentAt)
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        builder.Property(w => w.AppId)
            .HasMaxLength(255);

        // Indexes for performance
        builder.HasIndex(w => w.CompanyId)
            .HasDatabaseName("IX_WhatsAppMessages_CompanyId");

        builder.HasIndex(w => w.GupshupMessageId)
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppMessages_GupshupMessageId");

        builder.HasIndex(w => w.Status)
            .HasDatabaseName("IX_WhatsAppMessages_Status");

        builder.HasIndex(w => w.SentAt)
            .HasDatabaseName("IX_WhatsAppMessages_SentAt");

        builder.HasIndex(w => new { w.CompanyId, w.SentAt })
            .HasDatabaseName("IX_WhatsAppMessages_CompanyId_SentAt");

        builder.HasIndex(w => new { w.CompanyId, w.Status })
            .HasDatabaseName("IX_WhatsAppMessages_CompanyId_Status");

        // Relationships
        builder.HasOne(w => w.Company)
            .WithMany()
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.CompanyIntegration)
            .WithMany()
            .HasForeignKey(w => w.CompanyIntegrationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(w => w.WebhookEvents)
            .WithOne(e => e.WhatsAppMessage)
            .HasForeignKey(e => e.WhatsAppMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
