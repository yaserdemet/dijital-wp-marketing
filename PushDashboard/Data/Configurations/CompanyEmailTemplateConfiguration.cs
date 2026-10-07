using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyEmailTemplateConfiguration : IEntityTypeConfiguration<CompanyEmailTemplate>
{
    public void Configure(EntityTypeBuilder<CompanyEmailTemplate> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(cet => cet.Id);

        builder.Property(cet => cet.CompanyId)
            .IsRequired();

        builder.Property(cet => cet.CustomContent)
            .IsRequired();

        builder.Property(cet => cet.CustomSubject)
            .HasMaxLength(200);

        builder.Property(cet => cet.LastModifiedBy)
            .HasMaxLength(450);

        // Configure CompanyEmailTemplate relationships
        builder
            .HasOne(cet => cet.Company)
            .WithMany()
            .HasForeignKey(cet => cet.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // No foreign key constraint for EmailTemplate to allow negative IDs for custom templates
        // builder.Entity<CompanyEmailTemplate>()
        //     .HasOne(cet => cet.EmailTemplate)
        //     .WithMany(et => et.CompanyTemplates)
        //     .HasForeignKey(cet => cet.EmailTemplateId)
        //     .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cet => cet.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(cet => cet.LastModifiedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure CompanyEmailTemplate unique constraint
        // Only enforce uniqueness for system templates (where EmailTemplateId is not null)
        builder
            .HasIndex(cet => new { cet.CompanyId, cet.EmailTemplateId })
            .IsUnique()
            .HasFilter("\"EmailTemplateId\" IS NOT NULL");
    }
}
