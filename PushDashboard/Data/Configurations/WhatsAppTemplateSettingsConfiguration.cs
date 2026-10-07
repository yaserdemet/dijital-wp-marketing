using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class WhatsAppTemplateSettingsConfiguration : IEntityTypeConfiguration<WhatsAppTemplateSettings>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplateSettings> builder)
    {
        builder.ToTable("WhatsAppTemplateSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TemplateName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ModuleType)
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Unique constraint: Aynı şirket için aynı template NAME birden fazla kez olamaz
        builder.HasIndex(s => new { s.CompanyId, s.TemplateName })
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppTemplateSettings_CompanyId_TemplateName");

        // Foreign key relationship
        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for queries
        builder.HasIndex(s => s.CompanyId)
            .HasDatabaseName("IX_WhatsAppTemplateSettings_CompanyId");
    }
}
