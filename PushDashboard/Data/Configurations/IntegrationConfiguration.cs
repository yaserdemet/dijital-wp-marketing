using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.DetailedDescription)
            .HasMaxLength(1000);

        builder.Property(i => i.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.IconClass)
            .HasMaxLength(50);

        builder.Property(i => i.IconColor)
            .HasMaxLength(20);

        builder.Property(i => i.BackgroundColor)
            .HasMaxLength(20);

        builder.Property(i => i.LogoUrl)
            .HasMaxLength(100);

        builder.Property(i => i.Features)
            .HasMaxLength(2000);

        builder.Property(i => i.DefaultSettingsTemplate)
            .HasMaxLength(4000);
    }
}
