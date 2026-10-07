using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class WhatsAppTemplateVariableMappingConfiguration : IEntityTypeConfiguration<WhatsAppTemplateVariableMapping>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplateVariableMapping> builder)
    {
        builder.ToTable("WhatsAppTemplateVariableMappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WhatsAppTemplateName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ComponentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.VariableType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.VariableLabel)
            .HasMaxLength(200);

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Unique constraint: Aynı template, component ve position için birden fazla mapping olamaz
        builder.HasIndex(m => new { m.CompanyId, m.WhatsAppTemplateName, m.ComponentType, m.VariablePosition })
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppTemplateVariableMapping_Unique");

        // Foreign key relationship
        builder.HasOne(m => m.Company)
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for query performance
        builder.HasIndex(m => m.CompanyId)
            .HasDatabaseName("IX_WhatsAppTemplateVariableMappings_CompanyId");

        builder.HasIndex(m => new { m.CompanyId, m.WhatsAppTemplateName })
            .HasDatabaseName("IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateName");
    }
}
