using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CommentRequestConfiguration : IEntityTypeConfiguration<CommentRequest>
{
    public void Configure(EntityTypeBuilder<CommentRequest> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.CompanyId)
            .IsRequired();

        builder.Property(cr => cr.ProductUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cr => cr.ExternalProductId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.ExternalProductUrl)
            .HasMaxLength(500);

        builder.Property(cr => cr.ReviewSource)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cr => cr.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cr => cr.CommentsFileUrl)
            .HasMaxLength(1000);

        builder.Property(cr => cr.LogsFileUrl)
            .HasMaxLength(1000);

        builder.Property(cr => cr.ScreenshotUrl)
            .HasMaxLength(1000);

        builder.Property(cr => cr.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(cr => cr.ExportToken)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.WebhookUrl)
            .HasMaxLength(500);

        builder.Property(cr => cr.ExternalRequestId)
            .HasMaxLength(100);

        // Configure CommentRequest relationships
        builder
            .HasOne(cr => cr.Company)
            .WithMany()
            .HasForeignKey(cr => cr.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for CommentRequest
        builder
            .HasIndex(cr => cr.CompanyId)
            .HasDatabaseName("IX_CommentRequests_CompanyId_Performance");

        builder
            .HasIndex(cr => cr.CreatedAt)
            .HasDatabaseName("IX_CommentRequests_CreatedAt_Performance");

        builder
            .HasIndex(cr => cr.Status)
            .HasDatabaseName("IX_CommentRequests_Status_Performance");
    }
}
