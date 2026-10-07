using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CommentTransferJobConfiguration : IEntityTypeConfiguration<CommentTransferJob>
{
    public void Configure(EntityTypeBuilder<CommentTransferJob> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(ctj => ctj.Id);

        builder.Property(ctj => ctj.CompanyId)
            .IsRequired();

        builder.Property(ctj => ctj.JobId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ctj => ctj.ExternalBatchId)
            .HasMaxLength(100);

        builder.Property(ctj => ctj.JobType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ctj => ctj.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ctj => ctj.CurrentStep)
            .HasMaxLength(200);

        builder.Property(ctj => ctj.ResultFileUrl)
            .HasMaxLength(1000);

        builder.Property(ctj => ctj.LogsFileUrl)
            .HasMaxLength(1000);

        builder.Property(ctj => ctj.ErrorMessage)
            .HasMaxLength(500);

        // Configure CommentTransferJob relationships
        builder
            .HasOne(ctj => ctj.Company)
            .WithMany()
            .HasForeignKey(ctj => ctj.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ctj => ctj.Store)
            .WithMany(ts => ts.TransferJobs)
            .HasForeignKey(ctj => ctj.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasMany(ctj => ctj.JobProducts)
            .WithOne(ctjp => ctjp.Job)
            .HasForeignKey(ctjp => ctjp.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for CommentTransferJob
        builder
            .HasIndex(ctj => ctj.CompanyId)
            .HasDatabaseName("IX_CommentTransferJobs_CompanyId_Performance");

        builder
            .HasIndex(ctj => ctj.StoreId)
            .HasDatabaseName("IX_CommentTransferJobs_StoreId_Performance");

        builder
            .HasIndex(ctj => ctj.Status)
            .HasDatabaseName("IX_CommentTransferJobs_Status_Performance");

        builder
            .HasIndex(ctj => ctj.JobId)
            .IsUnique()
            .HasDatabaseName("IX_CommentTransferJobs_JobId_Unique");

        builder
            .HasIndex(ctj => ctj.CreatedAt)
            .HasDatabaseName("IX_CommentTransferJobs_CreatedAt_Performance");
    }
}
