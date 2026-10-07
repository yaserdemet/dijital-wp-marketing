using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(et => et.Id);

        builder.Property(et => et.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(et => et.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(et => et.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(et => et.DefaultContent)
            .IsRequired();

        builder.Property(et => et.DefaultSubject)
            .HasMaxLength(100);

        builder.Property(et => et.Variables)
            .HasMaxLength(1000);
    }
}
