using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Infrastructure.Services;

public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);

            if (bytes is null || bytes.Length == 0)
            {
                _logger.LogDebug("Cache MISS → {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache HIT → {Key}", key);
            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler cache para chave {Key}. Continuando sem cache", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetAsync(key, bytes, options, ct);
            _logger.LogDebug("Cache SET → {Key} | TTL: {TTL}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao escrever cache para chave {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("Cache INVALIDADO → {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao invalidar cache para chave {Key}", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);

        if (cached is not null)
            return cached;

        var result = await factory();

        if (result is not null)
            await SetAsync(key, result, expiration, ct);

        return result;
    }
}