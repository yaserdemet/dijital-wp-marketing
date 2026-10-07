using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Website)
            .HasMaxLength(200);

        builder.Property(c => c.BillingType)
            .HasMaxLength(20);

        builder.Property(c => c.TaxOffice)
            .HasMaxLength(100);

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(20);

        builder.Property(c => c.IdentityNumber)
            .HasMaxLength(11);

        builder.Property(c => c.BillingAddress)
            .HasMaxLength(500);

        builder.Property(c => c.CompanyName)
            .HasMaxLength(100);

        builder.Property(c => c.FullName)
            .HasMaxLength(100);

        builder.Property(c => c.CreditBalance)
            .HasColumnType("decimal(10,2)");
    }
}
