using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class NewsletterPopupSettingsConfiguration : IEntityTypeConfiguration<NewsletterPopupSettings>
{
    public void Configure(EntityTypeBuilder<NewsletterPopupSettings> builder)
    {
        builder.ToTable("NewsletterPopupSettings");

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

        builder.Property(e => e.PrivacyUrl)
            .HasMaxLength(500);

        builder.Property(e => e.NamePlaceholder)
            .HasMaxLength(100);

        builder.Property(e => e.SurnamePlaceholder)
            .HasMaxLength(100);

        builder.Property(e => e.PhonePlaceholder)
            .HasMaxLength(50);

        builder.Property(e => e.BackgroundColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ButtonColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ButtonTextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.BorderRadius)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.ImagePosition)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("left");

        builder.Property(e => e.HideImageOnMobile)
            .HasDefaultValue(false);

        builder.Property(e => e.MobileImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.SloganEnabled)
            .HasDefaultValue(false);

        builder.Property(e => e.SloganBadgeText)
            .HasMaxLength(200);

        builder.Property(e => e.SloganText)
            .HasMaxLength(1000);

        builder.Property(e => e.SloganHighlightColor)
            .HasMaxLength(20)
            .HasDefaultValue("#c8a96e");

        builder.Property(e => e.SloganTextColor)
            .HasMaxLength(20)
            .HasDefaultValue("#ffffff");

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

        builder.HasMany(e => e.Events)
            .WithOne(ev => ev.NewsletterPopupSettings)
            .HasForeignKey(ev => ev.NewsletterPopupSettingsId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.UpdatedByUserId);
        builder.HasIndex(e => e.IsActive);
    }
}

public class NewsletterPopupEventConfiguration : IEntityTypeConfiguration<NewsletterPopupEvent>
{
    public void Configure(EntityTypeBuilder<NewsletterPopupEvent> builder)
    {
        builder.ToTable("NewsletterPopupEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PageUrl)
            .HasMaxLength(2000);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(100);

        builder.Property(e => e.CustomerSurname)
            .HasMaxLength(100);

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(20);

        builder.HasOne(e => e.NewsletterPopupSettings)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.NewsletterPopupSettingsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.NewsletterPopupSettingsId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.IsMobile);
    }
}
