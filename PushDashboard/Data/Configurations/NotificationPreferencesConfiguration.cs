using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(np => np.Id);

        builder.Property(np => np.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // Configure NotificationPreferences relationships
        builder
            .HasOne(np => np.User)
            .WithOne(u => u.NotificationPreferences)
            .HasForeignKey<NotificationPreferences>(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
