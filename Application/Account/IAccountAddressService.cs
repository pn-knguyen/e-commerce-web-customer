namespace e_commerce_web_customer.Application.Account;

public interface IAccountAddressService
{
    Task<AccountAddressOperationResult> AddAddressAsync(
        string? email,
        AccountAddressInput input,
        CancellationToken cancellationToken = default);

    Task<AccountAddressOperationResult> SetDefaultAddressAsync(
        string? email,
        long addressId,
        CancellationToken cancellationToken = default);

    Task<AccountAddressSnapshot?> GetDefaultAddressAsync(
        string? email,
        CancellationToken cancellationToken = default);
}

public sealed record AccountAddressInput(
    string ContactName,
    string Phone,
    string ProvinceCode,
    string ProvinceName,
    string? DistrictCode,
    string? DistrictName,
    string WardCode,
    string WardName,
    string DetailAddress,
    bool IsDefault);

public sealed record AccountAddressSnapshot(
    long Id,
    string ContactName,
    string Phone,
    string ProvinceCode,
    string ProvinceName,
    string? DistrictCode,
    string? DistrictName,
    string WardCode,
    string WardName,
    string DetailAddress,
    string? FormattedAddress);

public sealed record AccountAddressOperationResult(
    bool Success,
    string Message);
