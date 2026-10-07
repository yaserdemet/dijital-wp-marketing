using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class StockReminderWidgetSettingsConfiguration : IEntityTypeConfiguration<StockReminderWidgetSettings>
{
    public void Configure(EntityTypeBuilder<StockReminderWidgetSettings> builder)
    {
        builder.ToTable("StockReminderWidgetSettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.SubmitButtonText)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.PrivacyText)
            .HasMaxLength(1000);

        builder.Property(e => e.PhonePlaceholder)
            .HasMaxLength(50);

        builder.Property(e => e.EmailPlaceholder)
            .HasMaxLength(100);

        builder.Property(e => e.VariantOptions)
            .HasMaxLength(500);

        builder.Property(e => e.VariantLabel)
            .HasMaxLength(100);

        builder.Property(e => e.BackgroundColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TitleColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ButtonColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ButtonTextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.VariantBorderColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.VariantSelectedColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.BorderRadius)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.UpdatedByUserId)
            .IsRequired();

        // Relationships
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Submissions)
            .WithOne(s => s.StockReminderWidgetSettings)
            .HasForeignKey(s => s.StockReminderWidgetSettingsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockReminderWidgetSubmissionConfiguration : IEntityTypeConfiguration<StockReminderWidgetSubmission>
{
    public void Configure(EntityTypeBuilder<StockReminderWidgetSubmission> builder)
    {
        builder.ToTable("StockReminderWidgetSubmissions");

        builder.HasKey(e => e.Id);

        // Kaynak bilgisi
        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("widget");

        // Ürün bilgileri
        builder.Property(e => e.ProductId)
            .HasMaxLength(100);

        builder.Property(e => e.ProductName)
            .HasMaxLength(500);

        builder.Property(e => e.StockCode)
            .HasMaxLength(100);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.ProductUrl)
            .HasMaxLength(1000);

        // Abone bilgileri
        builder.Property(e => e.SelectedVariant)
            .HasMaxLength(100);

        builder.Property(e => e.Phone)
            .HasMaxLength(20);

        builder.Property(e => e.Email)
            .HasMaxLength(200);

        builder.Property(e => e.FirstName)
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .HasMaxLength(100);

        builder.Property(e => e.PageUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        // FK — optional (Ticimax kaynaklı kayıtlar için null olabilir)
        builder.HasOne(e => e.StockReminderWidgetSettings)
            .WithMany(s => s.Submissions)
            .HasForeignKey(e => e.StockReminderWidgetSettingsId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.CompanyId, e.ProductId });
        builder.HasIndex(e => new { e.CompanyId, e.IsNotified });
        builder.HasIndex(e => new { e.CompanyId, e.ExternalAlarmId });
    }
}
