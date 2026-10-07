using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class VideoHostingConfiguration : IEntityTypeConfiguration<VideoHosting>
{
    public void Configure(EntityTypeBuilder<VideoHosting> builder)
    {
        builder.HasKey(vh => vh.Id);

        // Basic Properties
        builder.Property(vh => vh.CompanyId)
            .IsRequired();

        builder.Property(vh => vh.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(vh => vh.Description)
            .HasMaxLength(1000);

        // S3 Properties
        builder.Property(vh => vh.S3Key)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(vh => vh.S3Url)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(vh => vh.ThumbnailS3Key)
            .HasMaxLength(500);

        builder.Property(vh => vh.ThumbnailS3Url)
            .HasMaxLength(1000);

        // File Properties
        builder.Property(vh => vh.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(vh => vh.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vh => vh.VideoCodec)
            .HasMaxLength(50);

        builder.Property(vh => vh.AudioCodec)
            .HasMaxLength(50);

        // Billing Properties
        builder.Property(vh => vh.UploadCost)
            .HasColumnType("decimal(10,2)");

        builder.Property(vh => vh.StorageCostPerMonth)
            .HasColumnType("decimal(10,2)");

        // Status and Error
        builder.Property(vh => vh.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(vh => vh.ProcessingError)
            .HasMaxLength(1000);

        // Access Control
        builder.Property(vh => vh.AccessToken)
            .HasMaxLength(100);

        // Audit Properties
        builder.Property(vh => vh.UploadedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(vh => vh.UpdatedByUserId)
            .HasMaxLength(450);

        builder.Property(vh => vh.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(vh => vh.Company)
            .WithMany()
            .HasForeignKey(vh => vh.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vh => vh.UploadedByUser)
            .WithMany()
            .HasForeignKey(vh => vh.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vh => vh.UpdatedByUser)
            .WithMany()
            .HasForeignKey(vh => vh.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(vh => vh.Views)
            .WithOne(vhv => vhv.VideoHosting)
            .HasForeignKey(vhv => vhv.VideoHostingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(vh => new { vh.CompanyId, vh.Status });
        builder.HasIndex(vh => vh.CreatedAt);
        builder.HasIndex(vh => vh.S3Key).IsUnique();
        builder.HasIndex(vh => vh.AccessToken);
    }
}

public class VideoHostingViewConfiguration : IEntityTypeConfiguration<VideoHostingView>
{
    public void Configure(EntityTypeBuilder<VideoHostingView> builder)
    {
        builder.HasKey(vhv => vhv.Id);

        // Properties
        builder.Property(vhv => vhv.VideoHostingId)
            .IsRequired();

        builder.Property(vhv => vhv.CompanyId)
            .IsRequired();

        builder.Property(vhv => vhv.ViewerIpAddress)
            .HasMaxLength(45); // IPv6 support

        builder.Property(vhv => vhv.ViewerUserAgent)
            .HasMaxLength(500);

        builder.Property(vhv => vhv.ViewerCountry)
            .HasMaxLength(100);

        builder.Property(vhv => vhv.ViewerCity)
            .HasMaxLength(100);

        builder.Property(vhv => vhv.ReferrerUrl)
            .HasMaxLength(1000);

        builder.Property(vhv => vhv.EmbedUrl)
            .HasMaxLength(1000);

        builder.Property(vhv => vhv.ViewedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(vhv => vhv.VideoHosting)
            .WithMany(vh => vh.Views)
            .HasForeignKey(vhv => vhv.VideoHostingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vhv => vhv.Company)
            .WithMany()
            .HasForeignKey(vhv => vhv.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(vhv => new { vhv.CompanyId, vhv.ViewedAt });
        builder.HasIndex(vhv => vhv.VideoHostingId);
        builder.HasIndex(vhv => vhv.ViewerIpAddress);
    }
}

public class VideoHostingSettingsConfiguration : IEntityTypeConfiguration<VideoHostingSettings>
{
    public void Configure(EntityTypeBuilder<VideoHostingSettings> builder)
    {
        builder.HasKey(vhs => vhs.Id);

        // Properties
        builder.Property(vhs => vhs.CompanyId)
            .IsRequired();

        // Pricing Properties
        builder.Property(vhs => vhs.CostPerMinute)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0.5m);

        builder.Property(vhs => vhs.StorageCostPerGBPerMonth)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0.1m);

        // Limit Properties
        builder.Property(vhs => vhs.MaxFileSizeBytes)
            .HasDefaultValue(500 * 1024 * 1024); // 500MB

        builder.Property(vhs => vhs.MaxDurationMinutes)
            .HasDefaultValue(60);

        builder.Property(vhs => vhs.MaxVideosPerCompany)
            .HasDefaultValue(100);

        // Player Properties
        builder.Property(vhs => vhs.PlayerTheme)
            .HasMaxLength(50)
            .HasDefaultValue("default");

        builder.Property(vhs => vhs.CustomPlayerCss)
            .HasMaxLength(5000);

        builder.Property(vhs => vhs.AllowedDomains)
            .HasMaxLength(2000);

        // Audit Properties
        builder.Property(vhs => vhs.UpdatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(vhs => vhs.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(vhs => vhs.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(vhs => vhs.Company)
            .WithMany()
            .HasForeignKey(vhs => vhs.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vhs => vhs.UpdatedByUser)
            .WithMany()
            .HasForeignKey(vhs => vhs.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(vhs => vhs.CompanyId).IsUnique(); // One settings per company
    }
}
