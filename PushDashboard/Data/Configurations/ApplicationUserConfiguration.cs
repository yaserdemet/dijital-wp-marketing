using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure properties from Data Annotations
        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.CreditBalance)
            .HasColumnType("decimal(10,2)");

        builder.Property(u => u.ReferralCode)
            .HasMaxLength(6);

        builder.Property(u => u.ReferralCount)
            .HasDefaultValue(0);

        builder.HasIndex(u => u.ReferralCode)
            .IsUnique()
            .HasFilter("\"ReferralCode\" IS NOT NULL");

        builder.Property(u => u.ReferredByReferralCode)
            .HasMaxLength(6);

        // Configure the relationship between User and Company
        builder
            .HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure the relationship between User and NotificationPreferences
        builder
            .HasOne(u => u.NotificationPreferences)
            .WithOne(np => np.User)
            .HasForeignKey<NotificationPreferences>(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
