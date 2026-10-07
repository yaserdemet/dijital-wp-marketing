using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanySmsTemplateConfiguration : IEntityTypeConfiguration<CompanySmsTemplate>
{
    public void Configure(EntityTypeBuilder<CompanySmsTemplate> builder)
    {
        builder.ToTable("CompanySmsTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CustomContent)
            .IsRequired();

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SmsTemplate)
            .WithMany(t => t.CompanyTemplates)
            .HasForeignKey(e => e.SmsTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => new { e.CompanyId, e.SmsTemplateId })
            .IsUnique()
            .HasFilter("\"SmsTemplateId\" IS NOT NULL"); // Allow multiple custom templates (SmsTemplateId = null)

        builder.HasIndex(e => e.CompanyId);
    }
}
