using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CommentTransferJobProductConfiguration : IEntityTypeConfiguration<CommentTransferJobProduct>
{
    public void Configure(EntityTypeBuilder<CommentTransferJobProduct> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(ctjp => ctjp.Id);

        builder.Property(ctjp => ctjp.JobId)
            .IsRequired();

        builder.Property(ctjp => ctjp.ProductId)
            .IsRequired(false);

        builder.Property(ctjp => ctjp.ProductUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ctjp => ctjp.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ctjp => ctjp.ErrorMessage)
            .HasMaxLength(500);

        // Configure CommentTransferJobProduct relationships
        builder
            .HasOne(ctjp => ctjp.Job)
            .WithMany(ctj => ctj.JobProducts)
            .HasForeignKey(ctjp => ctjp.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ctjp => ctjp.Product)
            .WithMany(tp => tp.JobProducts)
            .HasForeignKey(ctjp => ctjp.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Performance indexes for CommentTransferJobProduct
        builder
            .HasIndex(ctjp => ctjp.JobId)
            .HasDatabaseName("IX_CommentTransferJobProducts_JobId_Performance");

        builder
            .HasIndex(ctjp => ctjp.ProductId)
            .HasDatabaseName("IX_CommentTransferJobProducts_ProductId_Performance");

        builder
            .HasIndex(ctjp => ctjp.Status)
            .HasDatabaseName("IX_CommentTransferJobProducts_Status_Performance");

        builder
            .HasIndex(ctjp => new { ctjp.JobId, ctjp.ProductId })
            .HasDatabaseName("IX_CommentTransferJobProducts_JobId_ProductId");
    }
}
