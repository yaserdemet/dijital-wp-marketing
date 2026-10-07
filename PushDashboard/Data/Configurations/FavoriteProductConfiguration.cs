using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct>
{
    public void Configure(EntityTypeBuilder<FavoriteProduct> builder)
    {
        builder.ToTable("FavoriteProducts");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ProductName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.StockCode)
            .HasMaxLength(100);

        builder.Property(f => f.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("TRY");

        builder.Property(f => f.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(f => f.ProductUrl)
            .HasMaxLength(1000);

        builder.Property(f => f.Price)
            .HasPrecision(18, 2);

        builder.Property(f => f.PriceWithVat)
            .HasPrecision(18, 2);

        // İndeksler
        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => f.ExternalProductId);
        builder.HasIndex(f => f.ExternalMemberId);
        builder.HasIndex(f => f.ExternalFavoriteId);
        builder.HasIndex(f => new { f.CompanyId, f.ExternalFavoriteId }).IsUnique();
        builder.HasIndex(f => new { f.CompanyId, f.ExternalProductId });
        builder.HasIndex(f => new { f.CompanyId, f.IsActive });
        builder.HasIndex(f => f.AddedToFavoritesAt);

        // İlişkiler
        builder.HasOne(f => f.Company)
            .WithMany()
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FavoriteProductSyncLogConfiguration : IEntityTypeConfiguration<FavoriteProductSyncLog>
{
    public void Configure(EntityTypeBuilder<FavoriteProductSyncLog> builder)
    {
        builder.ToTable("FavoriteProductSyncLogs");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ErrorMessage)
            .HasMaxLength(2000);

        // İndeksler
        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => f.StartedAt);
        builder.HasIndex(f => new { f.CompanyId, f.StartedAt });

        // İlişkiler
        builder.HasOne(f => f.Company)
            .WithMany()
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}