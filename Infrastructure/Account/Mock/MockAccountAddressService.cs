using e_commerce_web_customer.Application.Account;

namespace e_commerce_web_customer.Infrastructure.Account.Mock;

public sealed class MockAccountAddressService : IAccountAddressService
{
    private AccountAddressSnapshot? _defaultAddress;

    public Task<AccountAddressOperationResult> AddAddressAsync(
        string? email,
        AccountAddressInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _defaultAddress = new AccountAddressSnapshot(
            1,
            input.ContactName.Trim(),
            input.Phone.Trim(),
            input.ProvinceCode.Trim(),
            input.ProvinceName.Trim(),
            input.DistrictCode?.Trim(),
            input.DistrictName?.Trim(),
            input.WardCode.Trim(),
            input.WardName.Trim(),
            input.DetailAddress.Trim(),
            string.Join(", ", new[]
            {
                input.DetailAddress,
                input.WardName,
                input.DistrictName,
                input.ProvinceName
            }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim())));

        return Task.FromResult(new AccountAddressOperationResult(
            true,
            "Đã thêm địa chỉ và đặt làm mặc định."));
    }

    public Task<AccountAddressOperationResult> SetDefaultAddressAsync(
        string? email,
        long addressId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AccountAddressOperationResult(
            true,
            "Đã đặt địa chỉ mặc định."));
    }

    public Task<AccountAddressSnapshot?> GetDefaultAddressAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_defaultAddress);
    }
}
