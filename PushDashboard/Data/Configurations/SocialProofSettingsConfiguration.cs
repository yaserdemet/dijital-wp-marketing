using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class SocialProofSettingsConfiguration : IEntityTypeConfiguration<SocialProofSettings>
{
    public void Configure(EntityTypeBuilder<SocialProofSettings> builder)
    {
        // Primary key
        builder.HasKey(sps => sps.Id);

        // Company relationship
        builder
            .HasOne(sps => sps.Company)
            .WithMany()
            .HasForeignKey(sps => sps.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship
        builder
            .HasOne(sps => sps.UpdatedByUser)
            .WithMany()
            .HasForeignKey(sps => sps.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - her şirket için sadece bir ayar
        builder
            .HasIndex(sps => sps.CompanyId)
            .IsUnique()
            .HasDatabaseName("IX_SocialProofSettings_CompanyId_Unique");

        // Performance indexes
        builder
            .HasIndex(sps => sps.IsActive)
            .HasDatabaseName("IX_SocialProofSettings_IsActive_Performance");

        builder
            .HasIndex(sps => sps.UpdatedAt)
            .HasDatabaseName("IX_SocialProofSettings_UpdatedAt_Performance");

        // Column configurations
        builder.Property(sps => sps.TextTemplatesJson)
            .HasColumnType("text");

        builder.Property(sps => sps.DisplaySettingsJson)
            .HasColumnType("text");

        builder.Property(sps => sps.UpdatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        // Default values
        builder.Property(sps => sps.IsActive)
            .HasDefaultValue(true);

        builder.Property(sps => sps.ViewersMin)
            .HasDefaultValue(15);

        builder.Property(sps => sps.ViewersMax)
            .HasDefaultValue(45);

        builder.Property(sps => sps.FollowersMin)
            .HasDefaultValue(5);

        builder.Property(sps => sps.FollowersMax)
            .HasDefaultValue(20);

        builder.Property(sps => sps.BuyersMin)
            .HasDefaultValue(2);

        builder.Property(sps => sps.BuyersMax)
            .HasDefaultValue(8);

        builder.Property(sps => sps.UpdateInterval)
            .HasDefaultValue(60);

        builder.Property(sps => sps.DisplayDuration)
            .HasDefaultValue(5);

        builder.Property(sps => sps.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(sps => sps.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
