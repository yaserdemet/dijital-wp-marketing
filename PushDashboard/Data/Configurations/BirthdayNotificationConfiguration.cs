using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class BirthdayNotificationConfiguration : IEntityTypeConfiguration<BirthdayNotification>
{
    public void Configure(EntityTypeBuilder<BirthdayNotification> builder)
    {
        builder.HasKey(bn => bn.Id);

        builder.Property(bn => bn.Id)
            .ValueGeneratedOnAdd();

        builder.Property(bn => bn.Channel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bn => bn.TemplateName)
            .HasMaxLength(200);

        builder.Property(bn => bn.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(bn => bn.Cost)
            .HasPrecision(18, 2);

        builder.Property(bn => bn.SentAt)
            .IsRequired();

        // Company relationship
        builder
            .HasOne(bn => bn.Company)
            .WithMany()
            .HasForeignKey(bn => bn.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer relationship
        builder
            .HasOne(bn => bn.Customer)
            .WithMany()
            .HasForeignKey(bn => bn.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship (nullable)
        builder
            .HasOne(bn => bn.SentByUser)
            .WithMany()
            .HasForeignKey(bn => bn.SentByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(bn => new { bn.CompanyId, bn.CustomerId, bn.BirthYear })
            .HasDatabaseName("IX_BirthdayNotification_Company_Customer_Year");

        builder.HasIndex(bn => bn.SentAt)
            .HasDatabaseName("IX_BirthdayNotification_SentAt");
    }
}
