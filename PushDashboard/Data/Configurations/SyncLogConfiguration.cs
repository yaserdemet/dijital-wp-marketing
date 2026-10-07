using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        // Configure properties from Data Annotations
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.CompanyId)
            .IsRequired();

        builder.Property(sl => sl.SyncType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(sl => sl.Notes)
            .HasMaxLength(500);

        // Configure SyncLog relationships
        builder
            .HasOne(sl => sl.Company)
            .WithMany()
            .HasForeignKey(sl => sl.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
