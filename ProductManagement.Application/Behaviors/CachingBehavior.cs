using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ProductManagement.Application.Behaviors
{
    public interface ICacheableRequest
    {
        string CacheKey { get; }
        TimeSpan? Expiration { get; }
        bool BypassCache { get; }
    }

    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(
            IMemoryCache cache,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not ICacheableRequest cacheableRequest)
            {
                return await next();
            }

            if (cacheableRequest.BypassCache)
            {
                _logger.LogDebug("Cache bypassed for request {RequestType}", typeof(TRequest).Name);
                return await next();
            }

            var cacheKey = cacheableRequest.CacheKey;

            if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
            {
                _logger.LogDebug("Cache hit for key {CacheKey}", cacheKey);
                return cachedResponse;
            }

            _logger.LogDebug("Cache miss for key {CacheKey}", cacheKey);

            var response = await next();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableRequest.Expiration ?? TimeSpan.FromMinutes(5)
            };

            _cache.Set(cacheKey, response, cacheOptions);

            _logger.LogDebug("Cached response for key {CacheKey} with expiration {Expiration}",
                cacheKey, cacheOptions.AbsoluteExpirationRelativeToNow);

            return response;
        }
    }
}