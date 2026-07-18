namespace e_commerce_web_customer.Infrastructure.Caching;

public sealed class StorefrontDbQueryGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return _gate.WaitAsync(cancellationToken);
    }

    public void Release()
    {
        _gate.Release();
    }

    public async Task<T> RunAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await WaitAsync(cancellationToken);
        try
        {
            return await operation();
        }
        finally
        {
            Release();
        }
    }
}
