using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using e_commerce_web_customer.Models;

namespace e_commerce_web_customer.Data;

public partial class EcommerceDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public EcommerceDbContext()
    {
    }

    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<e_commerce_web_customer.Models.Attribute> Attributes { get; set; }

    public virtual DbSet<AttributeOption> AttributeOptions { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CampaignCategory> CampaignCategories { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategorySpecification> CategorySpecifications { get; set; }

    public virtual DbSet<CategoryVariantAttribute> CategoryVariantAttributes { get; set; }

    public virtual DbSet<GoodReceiptItem> GoodReceiptItems { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ProductVariantImage> ProductVariantImages { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionRule> PromotionRules { get; set; }

    public virtual DbSet<PromotionTarget> PromotionTargets { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<Specification> Specifications { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<VariantAttribute> VariantAttributes { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherTarget> VoucherTargets { get; set; }

    public virtual DbSet<VoucherUsage> VoucherUsages { get; set; }

    public virtual DbSet<VoucherUser> VoucherUsers { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = _configuration?.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        }

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<e_commerce_web_customer.Models.Attribute>(entity =>
        {
            entity.ToTable("attributes");

            entity.HasIndex(e => e.Code, "IX_attributes_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(80);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AttributeOption>(entity =>
        {
            entity.ToTable("attribute_options");

            entity.HasIndex(e => new { e.AttributeId, e.Value }, "IX_attribute_options_AttributeId_Value").IsUnique();

            entity.Property(e => e.Label).HasMaxLength(255);
            entity.Property(e => e.Value).HasMaxLength(120);

            entity.HasOne(d => d.Attribute).WithMany(p => p.AttributeOptions)
                .HasForeignKey(d => d.AttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");

            entity.HasIndex(e => e.Slug, "IX_brands_Slug").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(255);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("campaigns");

            entity.HasIndex(e => e.Slug, "IX_campaigns_Slug").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(30);
        });

        modelBuilder.Entity<CampaignCategory>(entity =>
        {
            entity.ToTable("campaign_categories");

            entity.HasIndex(e => e.CampaignId, "IX_campaign_categories_CampaignId");

            entity.HasIndex(e => e.CategoryId, "IX_campaign_categories_CategoryId");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignCategories)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Category).WithMany(p => p.CampaignCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");

            entity.HasIndex(e => e.ProductVariantId, "IX_cart_items_ProductVariantId");

            entity.HasIndex(e => new { e.UserId, e.ProductVariantId }, "IX_cart_items_UserId_ProductVariantId").IsUnique();

            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasIndex(e => e.ParentId, "IX_categories_ParentId");

            entity.HasIndex(e => e.Slug, "IX_categories_Slug").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(255);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);
        });

        modelBuilder.Entity<CategorySpecification>(entity =>
        {
            entity.HasKey(e => new { e.CategoryId, e.SpecificationId });

            entity.ToTable("category_specifications");

            entity.HasIndex(e => e.SpecificationId, "IX_category_specifications_SpecificationId");

            entity.Property(e => e.GroupName).HasMaxLength(120);

            entity.HasOne(d => d.Category).WithMany(p => p.CategorySpecifications)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Specification).WithMany(p => p.CategorySpecifications)
                .HasForeignKey(d => d.SpecificationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CategoryVariantAttribute>(entity =>
        {
            entity.HasKey(e => new { e.CategoryId, e.AttributeId });

            entity.ToTable("category_variant_attributes");

            entity.HasIndex(e => e.AttributeId, "IX_category_variant_attributes_AttributeId");

            entity.HasOne(d => d.Attribute).WithMany(p => p.CategoryVariantAttributes)
                .HasForeignKey(d => d.AttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryVariantAttributes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<GoodReceiptItem>(entity =>
        {
            entity.ToTable("good_receipt_items");

            entity.HasIndex(e => e.GoodsReceiptId, "IX_good_receipt_items_GoodsReceiptId");

            entity.HasIndex(e => e.ProductVariantId, "IX_good_receipt_items_ProductVariantId");

            entity.Property(e => e.ImportPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.GoodsReceipt).WithMany(p => p.GoodReceiptItems)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.GoodReceiptItems)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");

            entity.HasIndex(e => e.ApprovedBy, "IX_goods_receipts_ApprovedBy");

            entity.HasIndex(e => e.CreatedBy, "IX_goods_receipts_CreatedBy");

            entity.HasIndex(e => e.ReceiptCode, "IX_goods_receipts_ReceiptCode").IsUnique();

            entity.HasIndex(e => e.SupplierId, "IX_goods_receipts_SupplierId");

            entity.Property(e => e.ReceiptCode).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.GoodsReceiptApprovedByNavigations).HasForeignKey(d => d.ApprovedBy);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GoodsReceiptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            entity.HasIndex(e => e.OrderCode, "IX_orders_OrderCode").IsUnique();

            entity.HasIndex(e => e.PaymentMethodId, "IX_orders_PaymentMethodId");

            entity.HasIndex(e => e.ShippingAddressId, "IX_orders_ShippingAddressId");

            entity.HasIndex(e => e.UserId, "IX_orders_UserId");

            entity.HasIndex(e => e.VoucherId, "IX_orders_VoucherId");

            entity.Property(e => e.OrderCode).HasMaxLength(50);
            entity.Property(e => e.OrderStatus).HasMaxLength(30);
            entity.Property(e => e.PaymentStatus).HasMaxLength(30);
            entity.Property(e => e.ShippingContactName).HasMaxLength(255);
            entity.Property(e => e.ShippingDetail).HasMaxLength(500);
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippingPhone).HasMaxLength(30);
            entity.Property(e => e.ShippingProvince).HasMaxLength(120);
            entity.Property(e => e.ShippingWard).HasMaxLength(120);
            entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VoucherDiscount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ShippingAddress).WithMany(p => p.Orders).HasForeignKey(d => d.ShippingAddressId);

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders).HasForeignKey(d => d.VoucherId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            entity.HasIndex(e => e.OrderId, "IX_order_items_OrderId");

            entity.HasIndex(e => e.ProductVariantId, "IX_order_items_ProductVariantId");

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasIndex(e => e.BrandId, "IX_products_BrandId");

            entity.HasIndex(e => e.CategoryId, "IX_products_CategoryId");

            entity.HasIndex(e => e.Slug, "IX_products_Slug").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.RatingAverage).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Slug).HasMaxLength(255);

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.SpecificationId });

            entity.ToTable("product_specifications");

            entity.HasIndex(e => e.SpecificationId, "IX_product_specifications_SpecificationId");

            entity.Property(e => e.Value).HasMaxLength(1000);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Specification).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.SpecificationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");

            entity.HasIndex(e => e.Code, "IX_product_variants_Code").IsUnique();

            entity.HasIndex(e => e.ProductId, "IX_product_variants_ProductId");

            entity.Property(e => e.Code).HasMaxLength(80);
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.ColorName).HasMaxLength(120);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductVariantImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_product_color_images");

            entity.ToTable("product_variant_images");

            entity.HasIndex(e => e.ProductVariantId, "IX_product_variant_images_ProductVariantId");

            entity.Property(e => e.AltText).HasMaxLength(255);
            entity.Property(e => e.ImagePath).HasMaxLength(500);

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.ProductVariantImages)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MaxDiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<PromotionRule>(entity =>
        {
            entity.ToTable("promotion_rules");

            entity.HasIndex(e => e.GiftProductVariantId, "IX_promotion_rules_GiftProductVariantId");

            entity.HasIndex(e => e.PromotionId, "IX_promotion_rules_PromotionId");

            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.GiftProductVariant).WithMany(p => p.PromotionRules).HasForeignKey(d => d.GiftProductVariantId);

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.ToTable("promotion_targets");

            entity.HasIndex(e => new { e.PromotionId, e.TargetType, e.TargetId }, "IX_promotion_targets_PromotionId_TargetType_TargetId").IsUnique();

            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.ToTable("ratings");

            entity.HasIndex(e => e.OrderItemId, "IX_ratings_OrderItemId").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_ratings_UserId");

            entity.Property(e => e.Comment).HasMaxLength(1000);

            entity.HasOne(d => d.OrderItem).WithOne(p => p.Rating)
                .HasForeignKey<Rating>(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Specification>(entity =>
        {
            entity.ToTable("specifications");

            entity.HasIndex(e => e.Key, "IX_specifications_Key").IsUnique();

            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(30);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "IX_users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "IX_users_Username").IsUnique();

            entity.Property(e => e.AvatarImage).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(30);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Role).HasMaxLength(30);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_addresses");

            entity.HasIndex(e => new { e.UserId, e.IsDefault }, "IX_user_addresses_UserId_IsDefault");

            entity.HasIndex(e => new { e.UserId, e.IsDeleted }, "IX_user_addresses_UserId_IsDeleted");

            entity.Property(e => e.ContactName).HasMaxLength(255);
            entity.Property(e => e.DetailAddress).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.ProvinceCode).HasMaxLength(30);
            entity.Property(e => e.ProvinceName).HasMaxLength(120);
            entity.Property(e => e.Type).HasMaxLength(30);
            entity.Property(e => e.WardCode).HasMaxLength(30);
            entity.Property(e => e.WardName).HasMaxLength(120);

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VariantAttribute>(entity =>
        {
            entity.HasKey(e => new { e.ProductVariantId, e.AttributeOptionId });

            entity.ToTable("variant_attributes");

            entity.HasIndex(e => e.AttributeOptionId, "IX_variant_attributes_AttributeOptionId");

            entity.HasOne(d => d.AttributeOption).WithMany(p => p.VariantAttributes)
                .HasForeignKey(d => d.AttributeOptionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.VariantAttributes)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("vouchers");

            entity.HasIndex(e => e.Code, "IX_vouchers_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(80);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountType).HasMaxLength(30);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VoucherTarget>(entity =>
        {
            entity.ToTable("voucher_targets");

            entity.HasIndex(e => new { e.VoucherId, e.TargetType, e.TargetId }, "IX_voucher_targets_VoucherId_TargetType_TargetId").IsUnique();

            entity.Property(e => e.TargetType).HasMaxLength(50);

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherTargets)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VoucherUsage>(entity =>
        {
            entity.ToTable("voucher_usages");

            entity.HasIndex(e => e.OrderId, "IX_voucher_usages_OrderId").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_voucher_usages_UserId");

            entity.HasIndex(e => e.VoucherId, "IX_voucher_usages_VoucherId");

            entity.HasOne(d => d.Order).WithOne(p => p.VoucherUsage)
                .HasForeignKey<VoucherUsage>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.VoucherUsages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherUsages)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VoucherUser>(entity =>
        {
            entity.ToTable("voucher_users");

            entity.HasIndex(e => e.UserId, "IX_voucher_users_UserId");

            entity.HasIndex(e => new { e.VoucherId, e.UserId }, "IX_voucher_users_VoucherId_UserId").IsUnique();

            entity.HasOne(d => d.User).WithMany(p => p.VoucherUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherUsers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.ToTable("wishlist");

            entity.HasIndex(e => e.ProductVariantId, "IX_wishlist_ProductVariantId");

            entity.HasIndex(e => new { e.UserId, e.ProductVariantId }, "IX_wishlist_UserId_ProductVariantId").IsUnique();

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
