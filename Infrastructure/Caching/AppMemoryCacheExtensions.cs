using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Caching;

internal static class AppMemoryCacheExtensions
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<object?>>> InFlightRequests =
        new(StringComparer.Ordinal);

    public static async Task<T> GetOrCreateExclusiveAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T>> factory,
        MemoryCacheEntryOptions options)
    {
        if (cache.TryGetValue<CacheEnvelope<T>>(key, out var cached)
            && cached is not null)
        {
            return cached.Value;
        }

        var inFlightKey = $"{typeof(T).FullName}:{key}";
        var lazyRequest = InFlightRequests.GetOrAdd(
            inFlightKey,
            _ => new Lazy<Task<object?>>(
                async () => await factory().ConfigureAwait(false),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var value = (T)(await lazyRequest.Value.ConfigureAwait(false))!;
            cache.Set(key, new CacheEnvelope<T>(value), options);
            return value;
        }
        finally
        {
            InFlightRequests.TryRemove(
                new KeyValuePair<string, Lazy<Task<object?>>>(inFlightKey, lazyRequest));
        }
    }

    private sealed record CacheEnvelope<T>(T Value);
}
