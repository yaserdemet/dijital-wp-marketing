using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PushDashboard.Models;

namespace PushDashboard.Data.Configurations;

public class ProductSliderConfiguration : IEntityTypeConfiguration<ProductSlider>
{
    public void Configure(EntityTypeBuilder<ProductSlider> builder)
    {
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ps => ps.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ps => ps.Description)
            .HasMaxLength(500);

        builder.Property(ps => ps.DisplayType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        // Company relationship
        builder
            .HasOne(ps => ps.Company)
            .WithMany()
            .HasForeignKey(ps => ps.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship
        builder
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Items relationship
        builder
            .HasMany(ps => ps.Items)
            .WithOne(psi => psi.Slider)
            .HasForeignKey(psi => psi.SliderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Settings relationship
        builder
            .HasOne(ps => ps.Settings)
            .WithOne(pss => pss.Slider)
            .HasForeignKey<ProductSliderSettings>(pss => pss.SliderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder
            .HasIndex(ps => ps.CompanyId)
            .HasDatabaseName("IX_ProductSliders_CompanyId");

        builder
            .HasIndex(ps => new { ps.CompanyId, ps.IsActive })
            .HasDatabaseName("IX_ProductSliders_CompanyId_IsActive");
    }
}

public class ProductSliderItemConfiguration : IEntityTypeConfiguration<ProductSliderItem>
{
    public void Configure(EntityTypeBuilder<ProductSliderItem> builder)
    {
        builder.HasKey(psi => psi.Id);

        builder.Property(psi => psi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(psi => psi.ProductTitle)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(psi => psi.ProductImage)
            .HasMaxLength(1000);

        builder.Property(psi => psi.ProductUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(psi => psi.ExternalProductId)
            .HasMaxLength(50);

        builder.Property(psi => psi.ProductPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(psi => psi.DiscountPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(psi => psi.KDVAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(psi => psi.Currency)
            .HasMaxLength(50);

        builder.Property(psi => psi.ProductDescription)
            .HasMaxLength(1000);

        builder.Property(psi => psi.ManualDescription)
            .HasMaxLength(2000);

        // Slider relationship
        builder
            .HasOne(psi => psi.Slider)
            .WithMany(ps => ps.Items)
            .HasForeignKey(psi => psi.SliderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder
            .HasIndex(psi => psi.SliderId)
            .HasDatabaseName("IX_ProductSliderItems_SliderId");

        builder
            .HasIndex(psi => new { psi.SliderId, psi.SortOrder })
            .HasDatabaseName("IX_ProductSliderItems_SliderId_SortOrder");

        builder
            .HasIndex(psi => new { psi.SliderId, psi.IsActive })
            .HasDatabaseName("IX_ProductSliderItems_SliderId_IsActive");
    }
}

public class ProductSliderSettingsConfiguration : IEntityTypeConfiguration<ProductSliderSettings>
{
    public void Configure(EntityTypeBuilder<ProductSliderSettings> builder)
    {
        builder.HasKey(pss => pss.Id);

        builder.Property(pss => pss.Id)
            .ValueGeneratedOnAdd();

        builder.Property(pss => pss.Theme)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pss => pss.PrimaryColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pss => pss.SecondaryColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pss => pss.BackgroundColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pss => pss.TextColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pss => pss.AnimationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pss => pss.ImageAspectRatio)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pss => pss.DescriptionSource)
            .HasMaxLength(50);

        // Slider relationship
        builder
            .HasOne(pss => pss.Slider)
            .WithOne(ps => ps.Settings)
            .HasForeignKey<ProductSliderSettings>(pss => pss.SliderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - her slider için tek ayar
        builder
            .HasIndex(pss => pss.SliderId)
            .IsUnique()
            .HasDatabaseName("IX_ProductSliderSettings_SliderId_Unique");
    }
}
