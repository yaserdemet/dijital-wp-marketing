using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BasketReminderScheduleConfiguration : IEntityTypeConfiguration<BasketReminderSchedule>
{
    public void Configure(EntityTypeBuilder<BasketReminderSchedule> builder)
    {
        builder.HasKey(brs => brs.Id);

        builder.Property(brs => brs.Id)
            .ValueGeneratedOnAdd();

        builder.Property(brs => brs.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(brs => brs.ReminderTimeForHours)
            .IsRequired();

        builder.Property(brs => brs.NotificationContent)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(brs => brs.CommunicationChannels)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue("[]");

        builder.Property(brs => brs.ChannelMessages)
            .IsRequired()
            .HasMaxLength(2000)
            .HasDefaultValue("{}");

        builder.Property(brs => brs.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        // Company relationship
        builder
            .HasOne(brs => brs.Company)
            .WithMany()
            .HasForeignKey(brs => brs.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship
        builder
            .HasOne(brs => brs.CreatedByUser)
            .WithMany()
            .HasForeignKey(brs => brs.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder
            .HasIndex(brs => brs.CompanyId)
            .HasDatabaseName("IX_BasketReminderSchedules_CompanyId");

        builder
            .HasIndex(brs => new { brs.CompanyId, brs.IsActive })
            .HasDatabaseName("IX_BasketReminderSchedules_CompanyId_IsActive");
    }
}

public class BasketReminderLogConfiguration : IEntityTypeConfiguration<BasketReminderLog>
{
    public void Configure(EntityTypeBuilder<BasketReminderLog> builder)
    {
        builder.HasKey(brl => brl.Id);

        builder.Property(brl => brl.Id)
            .ValueGeneratedOnAdd();

        builder.Property(brl => brl.BasketExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(brl => brl.CustomerEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(brl => brl.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(brl => brl.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(brl => brl.NotificationContent)
            .HasMaxLength(500);

        // Company relationship
        builder
            .HasOne(brl => brl.Company)
            .WithMany()
            .HasForeignKey(brl => brl.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Basket relationship
        builder
            .HasOne(brl => brl.Basket)
            .WithMany()
            .HasForeignKey(brl => brl.BasketId)
            .OnDelete(DeleteBehavior.Cascade);

        // BasketReminderSchedule relationship
        builder
            .HasOne(brl => brl.BasketReminderSchedule)
            .WithMany()
            .HasForeignKey(brl => brl.BasketReminderScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder
            .HasIndex(brl => brl.CompanyId)
            .HasDatabaseName("IX_BasketReminderLogs_CompanyId");

        builder
            .HasIndex(brl => new { brl.CompanyId, brl.SentAt })
            .HasDatabaseName("IX_BasketReminderLogs_CompanyId_SentAt");

        builder
            .HasIndex(brl => new { brl.CustomerId, brl.CompanyId })
            .HasDatabaseName("IX_BasketReminderLogs_CustomerId_CompanyId");

        builder
            .HasIndex(brl => new { brl.CustomerId, brl.CompanyId, brl.BasketReminderScheduleId })
            .HasDatabaseName("IX_BasketReminderLogs_CustomerId_CompanyId_ScheduleId");

        builder
            .HasIndex(brl => brl.BasketReminderScheduleId)
            .HasDatabaseName("IX_BasketReminderLogs_ScheduleId");
    }
}

public class BasketReminderSettingsConfiguration : IEntityTypeConfiguration<BasketReminderSettings>
{
    public void Configure(EntityTypeBuilder<BasketReminderSettings> builder)
    {
        builder.HasKey(brs => brs.Id);

        builder.Property(brs => brs.Id)
            .ValueGeneratedOnAdd();

        builder.Property(brs => brs.NotificationContent)
            .IsRequired()
            .HasMaxLength(300); // Maksimum 300 karakter

        builder.Property(brs => brs.UpdatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        // Company relationship
        builder
            .HasOne(brs => brs.Company)
            .WithMany()
            .HasForeignKey(brs => brs.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship
        builder
            .HasOne(brs => brs.UpdatedByUser)
            .WithMany()
            .HasForeignKey(brs => brs.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - her şirket için tek ayar
        builder
            .HasIndex(brs => brs.CompanyId)
            .IsUnique()
            .HasDatabaseName("IX_BasketReminderSettings_CompanyId_Unique");
    }
}
