using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.HasKey(ud => ud.Id);

        builder.Property(ud => ud.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(ud => ud.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ud => ud.DeviceId)
            .HasMaxLength(500);

        builder.Property(ud => ud.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(ud => ud.Browser)
            .HasMaxLength(100);

        builder.Property(ud => ud.BrowserFamily)
            .HasMaxLength(100);

        builder.Property(ud => ud.DeviceType)
            .HasMaxLength(50);

        builder.Property(ud => ud.LastIpAddress)
            .HasMaxLength(45);

        builder.Property(ud => ud.LastLocation)
            .HasMaxLength(200);

        builder.HasIndex(ud => ud.DeviceFingerprint)
            .IsUnique();

        builder.HasIndex(ud => new { ud.UserId, ud.DeviceFingerprint });

        builder.HasIndex(ud => ud.IsLocalhost);

        builder.HasIndex(ud => ud.IsIncognito);

        builder.HasOne(ud => ud.User)
            .WithMany()
            .HasForeignKey(ud => ud.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
