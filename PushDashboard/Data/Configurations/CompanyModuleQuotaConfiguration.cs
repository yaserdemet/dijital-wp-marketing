using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyModuleQuotaConfiguration : IEntityTypeConfiguration<CompanyModuleQuota>
{
    public void Configure(EntityTypeBuilder<CompanyModuleQuota> builder)
    {
        builder.ToTable("CompanyModuleQuotas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalQuota)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.RemainingQuota)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Enforce uniqueness for Company and Module
        builder.HasIndex(x => new { x.CompanyId, x.ModuleId }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Module)
            .WithMany()
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModulePackage)
            .WithMany()
            .HasForeignKey(x => x.ModulePackageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
