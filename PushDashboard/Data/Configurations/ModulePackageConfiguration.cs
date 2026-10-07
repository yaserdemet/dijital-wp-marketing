using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ModulePackageConfiguration : IEntityTypeConfiguration<ModulePackage>
{
    public void Configure(EntityTypeBuilder<ModulePackage> builder)
    {
        builder.ToTable("ModulePackages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.QuotaAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.MonthlyPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.YearlyPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(x => x.Module)
            .WithMany()
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
