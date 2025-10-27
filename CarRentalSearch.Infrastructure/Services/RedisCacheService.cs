using System.Text.Json;
using CarRentalSearch.Application.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CarRentalSearch.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(15);

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var cachedData = await _cache.GetAsync(key);
        if (cachedData == null) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing cached data for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? duration = null) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration ?? DefaultCacheDuration
            };

            var jsonData = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, jsonData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching data for key: {Key}", key);
        }
    }
}