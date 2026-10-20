using SentinelPay.Application.Abstractions;
using StackExchange.Redis;
namespace SentinelPay.Infrastructure.Caching;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer redis) : IIdempotencyStore
{
    public Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct) =>
        redis.GetDatabase().StringSetAsync($"idem:{key}", "1", ttl, When.NotExists);
}
