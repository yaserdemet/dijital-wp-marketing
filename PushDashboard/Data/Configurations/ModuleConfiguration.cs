using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.DetailedDescription)
            .HasMaxLength(1000);

        builder.Property(m => m.Price)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(m => m.MonthlyPrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(m => m.YearlyPrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(m => m.IconClass)
            .HasMaxLength(50);

        builder.Property(m => m.IconColor)
            .HasMaxLength(20);

        builder.Property(m => m.BackgroundColor)
            .HasMaxLength(20);

        // Configure Module relationships
        builder
            .HasOne(m => m.Category)
            .WithMany(c => c.Modules)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
