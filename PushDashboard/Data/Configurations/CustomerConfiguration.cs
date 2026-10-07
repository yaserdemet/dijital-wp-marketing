using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.MobilePhone)
            .HasMaxLength(20);

        builder.Property(c => c.City)
            .HasMaxLength(100);

        builder.Property(c => c.District)
            .HasMaxLength(100);

        builder.Property(c => c.CustomerCode)
            .HasMaxLength(100);

        builder.Property(c => c.MembershipType)
            .HasMaxLength(100);

        builder.Property(c => c.LastLoginIp)
            .HasMaxLength(50);

        builder.Property(c => c.CreditLimit)
            .HasColumnType("decimal(10,2)");

        builder.Property(c => c.Profession)
            .HasMaxLength(100);

        builder.Property(c => c.EducationLevel)
            .HasMaxLength(100);

        // Configure Customer relationships
        builder
            .HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Customer unique constraint for ExternalId and CompanyId
        builder
            .HasIndex(c => new { c.ExternalId, c.CompanyId })
            .IsUnique();
    }
}
