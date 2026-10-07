using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyIntegrationConfiguration : IEntityTypeConfiguration<CompanyIntegration>
{
    public void Configure(EntityTypeBuilder<CompanyIntegration> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.CompanyId)
            .IsRequired();

        builder.Property(ci => ci.IntegrationId)
            .IsRequired();

        builder.Property(ci => ci.SettingsJson)
            .HasMaxLength(4000);

        builder.Property(ci => ci.SyncStatsJson)
            .HasMaxLength(1000);

        builder.Property(ci => ci.KvkkLink)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ci => ci.TicariElektronikIletiLink)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ci => ci.LegalTextsAccepted)
            .IsRequired();

        // Configure CompanyIntegration relationships
        builder
            .HasOne(ci => ci.Company)
            .WithMany()
            .HasForeignKey(ci => ci.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ci => ci.Integration)
            .WithMany(i => i.CompanyIntegrations)
            .HasForeignKey(ci => ci.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure CompanyIntegration unique constraint
        builder
            .HasIndex(ci => new { ci.CompanyId, ci.IntegrationId })
            .IsUnique();
    }
}
