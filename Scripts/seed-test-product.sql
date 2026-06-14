USE [e-commerce];
GO

DECLARE @BrandId BIGINT;
DECLARE @CategoryId BIGINT;
DECLARE @ProductId BIGINT;
DECLARE @BlackVariantId BIGINT;
DECLARE @BlueVariantId BIGINT;

INSERT INTO brands
(
    Name,
    Description,
    ImagePath,
    Slug,
    IsActive,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    N'Test Brand',
    N'Thuong hieu dung de test product service',
    N'/images/brands/test-brand.png',
    N'test-brand',
    1,
    GETDATE(),
    NULL
);

SET @BrandId = SCOPE_IDENTITY();

INSERT INTO categories
(
    Name,
    ParentId,
    Description,
    ImagePath,
    Slug,
    Position,
    IsActive,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    N'Dien thoai test',
    NULL,
    N'Danh muc dung de test product service',
    N'/images/categories/test-phone.png',
    N'dien-thoai-test',
    1,
    1,
    GETDATE(),
    NULL
);

SET @CategoryId = SCOPE_IDENTITY();

INSERT INTO products
(
    BrandId,
    CategoryId,
    Name,
    Description,
    Slug,
    ViewsCount,
    TotalSoldCount,
    RatingAverage,
    RatingCount,
    IsActive,
    IsFeatured,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    @BrandId,
    @CategoryId,
    N'Dien thoai Test Pro 128GB',
    N'San pham mau de test ProductService va ProductDto.',
    N'dien-thoai-test-pro-128gb',
    125,
    37,
    4.70,
    18,
    1,
    1,
    GETDATE(),
    NULL
);

SET @ProductId = SCOPE_IDENTITY();

INSERT INTO product_variants
(
    ProductId,
    Code,
    Price,
    SoldCount,
    Quantity,
    IsDefault,
    IsActive,
    CreatedAt,
    UpdatedAt,
    ColorHex,
    ColorName
)
VALUES
(
    @ProductId,
    N'TEST-PRO-128-BLACK',
    12990000,
    25,
    50,
    1,
    1,
    GETDATE(),
    NULL,
    N'#111111',
    N'Den'
);

SET @BlackVariantId = SCOPE_IDENTITY();

INSERT INTO product_variants
(
    ProductId,
    Code,
    Price,
    SoldCount,
    Quantity,
    IsDefault,
    IsActive,
    CreatedAt,
    UpdatedAt,
    ColorHex,
    ColorName
)
VALUES
(
    @ProductId,
    N'TEST-PRO-128-BLUE',
    13490000,
    12,
    30,
    0,
    1,
    GETDATE(),
    NULL,
    N'#2563EB',
    N'Xanh'
);

SET @BlueVariantId = SCOPE_IDENTITY();

INSERT INTO product_variant_images
(
    ProductVariantId,
    ImagePath,
    AltText,
    Position
)
VALUES
(
    @BlackVariantId,
    N'/images/products/test-pro-black-1.png',
    N'Dien thoai Test Pro mau den',
    1
),
(
    @BlackVariantId,
    N'/images/products/test-pro-black-2.png',
    N'Dien thoai Test Pro mau den goc nghieng',
    2
),
(
    @BlueVariantId,
    N'/images/products/test-pro-blue-1.png',
    N'Dien thoai Test Pro mau xanh',
    1
);

SELECT
    @ProductId AS ProductId,
    @BlackVariantId AS DefaultVariantId,
    @BlueVariantId AS BlueVariantId;
