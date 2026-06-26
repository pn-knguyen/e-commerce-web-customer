using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Account.Db;

public sealed class DbAccountAddressService(EcommerceDbContext dbContext) : IAccountAddressService
{
    public async Task<AccountAddressOperationResult> AddAddressAsync(
        string? email,
        AccountAddressInput input,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(email, cancellationToken);
        if (user is null)
        {
            return new(false, "Không tìm thấy tài khoản để thêm địa chỉ.");
        }

        if (!IsValid(input))
        {
            return new(false, "Thông tin địa chỉ chưa đầy đủ.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var existingAddresses = await dbContext.UserAddresses
            .Where(address => address.UserId == user.Id && !address.IsDeleted)
            .ToListAsync(cancellationToken);
        var shouldBeDefault = input.IsDefault || existingAddresses.Count == 0;

        if (shouldBeDefault)
        {
            foreach (var address in existingAddresses.Where(address => address.IsDefault))
            {
                address.IsDefault = false;
                address.UpdatedAt = now;
            }
        }

        dbContext.UserAddresses.Add(new UserAddress
        {
            UserId = user.Id,
            ContactName = input.ContactName.Trim(),
            Phone = input.Phone.Trim(),
            ProvinceCode = input.ProvinceCode.Trim(),
            ProvinceName = input.ProvinceName.Trim(),
            DistrictCode = TrimOrNull(input.DistrictCode),
            DistrictName = TrimOrNull(input.DistrictName),
            WardCode = input.WardCode.Trim(),
            WardName = input.WardName.Trim(),
            DetailAddress = input.DetailAddress.Trim(),
            FormattedAddress = BuildFormattedAddress(input),
            Type = AddressType.Shipping,
            IsDefault = shouldBeDefault,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, shouldBeDefault
            ? "Đã thêm địa chỉ và đặt làm mặc định."
            : "Đã thêm địa chỉ mới.");
    }

    public async Task<AccountAddressOperationResult> SetDefaultAddressAsync(
        string? email,
        long addressId,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(email, cancellationToken);
        if (user is null)
        {
            return new(false, "Không tìm thấy tài khoản để cập nhật địa chỉ.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var addresses = await dbContext.UserAddresses
            .Where(address => address.UserId == user.Id && !address.IsDeleted)
            .ToListAsync(cancellationToken);
        var selectedAddress = addresses.FirstOrDefault(address => address.Id == addressId);
        if (selectedAddress is null)
        {
            return new(false, "Địa chỉ không tồn tại hoặc không thuộc tài khoản này.");
        }

        var now = DateTime.UtcNow;
        foreach (var address in addresses)
        {
            address.IsDefault = address.Id == selectedAddress.Id;
            address.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, "Đã đặt địa chỉ mặc định.");
    }

    public async Task<AccountAddressSnapshot?> GetDefaultAddressAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var address = await dbContext.UserAddresses
            .AsNoTracking()
            .Where(item => item.UserId == user.Id && !item.IsDeleted)
            .OrderByDescending(item => item.IsDefault)
            .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return address is null ? null : ToSnapshot(address);
    }

    private async Task<User?> ResolveUserAsync(
        string? email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        return await dbContext.Users
            .FirstOrDefaultAsync(
                user => user.IsActive && user.Email.ToLower() == normalizedEmail,
                cancellationToken);
    }

    private static bool IsValid(AccountAddressInput input) =>
        !string.IsNullOrWhiteSpace(input.ContactName)
        && !string.IsNullOrWhiteSpace(input.Phone)
        && !string.IsNullOrWhiteSpace(input.ProvinceCode)
        && !string.IsNullOrWhiteSpace(input.ProvinceName)
        && !string.IsNullOrWhiteSpace(input.WardCode)
        && !string.IsNullOrWhiteSpace(input.WardName)
        && !string.IsNullOrWhiteSpace(input.DetailAddress);

    private static AccountAddressSnapshot ToSnapshot(UserAddress address) => new(
        address.Id,
        address.ContactName,
        address.Phone,
        address.ProvinceCode,
        address.ProvinceName,
        address.DistrictCode,
        address.DistrictName,
        address.WardCode,
        address.WardName,
        address.DetailAddress,
        address.FormattedAddress);

    private static string BuildFormattedAddress(AccountAddressInput input)
    {
        var parts = new[]
        {
            input.DetailAddress,
            input.WardName,
            input.DistrictName,
            input.ProvinceName
        }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        return string.Join(", ", parts);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
