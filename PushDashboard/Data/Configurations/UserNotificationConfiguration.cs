using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Icon)
            .HasMaxLength(100)
            .HasDefaultValue("fas fa-bell");

        builder.Property(n => n.IconColor)
            .HasMaxLength(50)
            .HasDefaultValue("text-blue-500");

        builder.Property(n => n.IconBackground)
            .HasMaxLength(50)
            .HasDefaultValue("bg-blue-100");

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);

        builder.Property(n => n.Type)
            .HasConversion<int>()
            .HasDefaultValue(NotificationType.Info);

        builder.Property(n => n.IsRead)
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // User ilişkisi
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Company ilişkisi (opsiyonel)
        builder.HasOne(n => n.Company)
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // İndeksler
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.CompanyId);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
