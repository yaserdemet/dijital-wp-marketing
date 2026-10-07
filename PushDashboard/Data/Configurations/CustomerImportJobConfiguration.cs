using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CustomerImportJobConfiguration : IEntityTypeConfiguration<CustomerImportJob>
{
    public void Configure(EntityTypeBuilder<CustomerImportJob> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(cij => cij.Id);

        builder.Property(cij => cij.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(cij => cij.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(cij => cij.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cij => cij.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        // Configure CustomerImportJob relationships
        builder
            .HasOne(cij => cij.Company)
            .WithMany()
            .HasForeignKey(cij => cij.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cij => cij.CreatedByUser)
            .WithMany()
            .HasForeignKey(cij => cij.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance indexes for CustomerImportJob
        builder
            .HasIndex(cij => cij.CompanyId)
            .HasDatabaseName("IX_CustomerImportJobs_CompanyId_Performance");

        builder
            .HasIndex(cij => cij.Status)
            .HasDatabaseName("IX_CustomerImportJobs_Status_Performance");

        builder
            .HasIndex(cij => cij.CreatedAt)
            .HasDatabaseName("IX_CustomerImportJobs_CreatedAt_Performance");
    }
}
