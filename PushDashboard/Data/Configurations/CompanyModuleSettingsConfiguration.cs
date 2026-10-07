using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyModuleSettingsConfiguration : IEntityTypeConfiguration<CompanyModuleSettings>
{
    public void Configure(EntityTypeBuilder<CompanyModuleSettings> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(cms => cms.Id);

        builder.Property(cms => cms.SettingsJson)
            .HasMaxLength(4000);

        builder.Property(cms => cms.UpdatedByUserId)
            .IsRequired();

        builder
            .HasOne(cms => cms.CompanyModule)
            .WithOne(cm => cm.Settings)
            .HasForeignKey<CompanyModuleSettings>(cms => cms.CompanyModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cms => cms.UpdatedByUser)
            .WithMany()
            .HasForeignKey(cms => cms.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
