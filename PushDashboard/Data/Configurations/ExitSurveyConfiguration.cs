using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ExitSurveyConfiguration : IEntityTypeConfiguration<ExitSurvey>
{
    public void Configure(EntityTypeBuilder<ExitSurvey> builder)
    {
        builder.ToTable("ExitSurveys");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AdminDescription)
            .HasMaxLength(500);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.SubmitButtonText)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CancelButtonText)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ThankYouMessage)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.BackgroundColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.SubmitButtonColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.CancelButtonColor)
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

        builder.HasMany(e => e.Questions)
            .WithOne(q => q.ExitSurvey)
            .HasForeignKey(q => q.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Responses)
            .WithOne(r => r.ExitSurvey)
            .HasForeignKey(r => r.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Events)
            .WithOne(ev => ev.ExitSurvey)
            .HasForeignKey(ev => ev.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => e.UpdatedByUserId);
        builder.HasIndex(e => e.IsActive);
    }
}

public class ExitSurveyQuestionConfiguration : IEntityTypeConfiguration<ExitSurveyQuestion>
{
    public void Configure(EntityTypeBuilder<ExitSurveyQuestion> builder)
    {
        builder.ToTable("ExitSurveyQuestions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(q => q.QuestionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(q => q.OptionsJson)
            .HasColumnType("text");

        // Relationships
        builder.HasOne(q => q.ExitSurvey)
            .WithMany(e => e.Questions)
            .HasForeignKey(q => q.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Responses)
            .WithOne(r => r.Question)
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => q.ExitSurveyId);
        builder.HasIndex(q => q.SortOrder);
        builder.HasIndex(q => q.IsActive);
    }
}

public class ExitSurveyEventConfiguration : IEntityTypeConfiguration<ExitSurveyEvent>
{
    public void Configure(EntityTypeBuilder<ExitSurveyEvent> builder)
    {
        builder.ToTable("ExitSurveyEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PageUrl)
            .HasMaxLength(2000);

        builder.Property(e => e.PageType)
            .HasMaxLength(50);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.HasOne(e => e.ExitSurvey)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ExitSurveyId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.IsMobile);
    }
}

public class ExitSurveyResponseConfiguration : IEntityTypeConfiguration<ExitSurveyResponse>
{
    public void Configure(EntityTypeBuilder<ExitSurveyResponse> builder)
    {
        builder.ToTable("ExitSurveyResponses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ResponseText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.IpAddress)
            .HasMaxLength(45); // IPv6 support

        builder.Property(r => r.UserAgent)
            .HasMaxLength(500);

        builder.Property(r => r.SessionId)
            .HasMaxLength(100);

        builder.Property(r => r.PageUrl)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(r => r.ExitSurvey)
            .WithMany(e => e.Responses)
            .HasForeignKey(r => r.ExitSurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Question)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.ExitSurveyId);
        builder.HasIndex(r => r.QuestionId);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.IpAddress);
        builder.HasIndex(r => r.SessionId);
    }
}
