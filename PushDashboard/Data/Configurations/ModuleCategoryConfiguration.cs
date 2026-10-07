using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ModuleCategoryConfiguration : IEntityTypeConfiguration<ModuleCategory>
{
    public void Configure(EntityTypeBuilder<ModuleCategory> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mc => mc.Description)
            .HasMaxLength(200);

        builder.Property(mc => mc.IconClass)
            .HasMaxLength(50);
    }
}
