using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(ui => ui.Id);

        builder.Property(ui => ui.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ui => ui.CompanyId)
            .IsRequired();

        builder.Property(ui => ui.InvitationToken)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(ui => ui.ExpirationDate)
            .IsRequired();

        builder.Property(ui => ui.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        // Configure UserInvitation relationships
        builder
            .HasOne(ui => ui.Company)
            .WithMany()
            .HasForeignKey(ui => ui.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ui => ui.CreatedByUser)
            .WithMany()
            .HasForeignKey(ui => ui.CreatedBy)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure UserInvitation unique constraint for Email and CompanyId (prevent duplicate invitations)
        builder
            .HasIndex(ui => new { ui.Email, ui.CompanyId })
            .IsUnique()
            .HasFilter("\"IsUsed\" = false"); // Only enforce uniqueness for unused invitations

        // Configure UserInvitation index for InvitationToken
        builder
            .HasIndex(ui => ui.InvitationToken)
            .IsUnique();
    }
}
