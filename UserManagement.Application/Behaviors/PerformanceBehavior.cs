using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserManagement.Application.Behaviors
{
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly long _performanceThresholdMs;

        public PerformanceBehavior(
            ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _performanceThresholdMs = configuration.GetValue<long>("Performance:ThresholdMs", 1000);
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var response = await next();

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > _performanceThresholdMs)
            {
                _logger.LogWarning(
                    "[Performance Warning] Request {RequestName} took {ElapsedMs}ms (Threshold: {ThresholdMs}ms)",
                    requestName, stopwatch.ElapsedMilliseconds, _performanceThresholdMs);
            }

            return response;
        }
    }
}