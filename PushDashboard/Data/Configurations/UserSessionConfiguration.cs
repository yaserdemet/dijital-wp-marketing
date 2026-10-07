using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(us => us.Id);

        builder.Property(us => us.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(us => us.SessionId)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(us => us.DeviceId);

        builder.HasOne(us => us.User)
            .WithMany(u => u.UserSessions)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(us => us.Device)
            .WithMany(d => d.UserSessions)
            .HasForeignKey(us => us.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
