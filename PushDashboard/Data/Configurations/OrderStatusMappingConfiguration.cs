using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class OrderStatusMappingConfiguration : IEntityTypeConfiguration<OrderStatusMapping>
{
    public void Configure(EntityTypeBuilder<OrderStatusMapping> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IntegrationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.ExternalStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.ExternalStatusDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.InternalStatus)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt);

        // Configure the relationship between OrderStatusMapping and Company
        builder
            .HasOne(m => m.Company)
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create unique index for company, integration type, and external status
        builder.HasIndex(m => new { m.CompanyId, m.IntegrationType, m.ExternalStatus })
            .IsUnique()
            .HasDatabaseName("IX_OrderStatusMappings_Company_Integration_ExternalStatus");

        // Create index for faster lookups
        builder.HasIndex(m => new { m.CompanyId, m.IntegrationType, m.IsActive })
            .HasDatabaseName("IX_OrderStatusMappings_Company_Integration_Active");
    }
}
