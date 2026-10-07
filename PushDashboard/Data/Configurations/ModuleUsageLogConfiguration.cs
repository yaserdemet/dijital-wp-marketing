using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ModuleUsageLogConfiguration : IEntityTypeConfiguration<ModuleUsageLog>
{
    public void Configure(EntityTypeBuilder<ModuleUsageLog> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(mul => mul.Id);

        builder.Property(mul => mul.CompanyId)
            .IsRequired();

        builder.Property(mul => mul.ModuleId)
            .IsRequired();

        builder.Property(mul => mul.UsageType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mul => mul.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(mul => mul.Cost)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(mul => mul.BalanceBefore)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(mul => mul.BalanceAfter)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(mul => mul.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(mul => mul.ReferenceId)
            .HasMaxLength(100);

        builder.Property(mul => mul.Channel)
            .HasMaxLength(50);

        builder.Property(mul => mul.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(mul => mul.Metadata)
            .HasMaxLength(1000);

        // Configure ModuleUsageLog relationships
        builder
            .HasOne(mul => mul.Company)
            .WithMany()
            .HasForeignKey(mul => mul.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(mul => mul.Module)
            .WithMany()
            .HasForeignKey(mul => mul.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(mul => mul.User)
            .WithMany()
            .HasForeignKey(mul => mul.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for ModuleUsageLog
        builder
            .HasIndex(mul => mul.CompanyId)
            .HasDatabaseName("IX_ModuleUsageLogs_CompanyId_Performance");

        builder
            .HasIndex(mul => mul.CreatedAt)
            .HasDatabaseName("IX_ModuleUsageLogs_CreatedAt_Performance");

        builder
            .HasIndex(mul => new { mul.CompanyId, mul.ModuleId, mul.CreatedAt })
            .HasDatabaseName("IX_ModuleUsageLogs_Company_Module_Date_Performance");
    }
}
