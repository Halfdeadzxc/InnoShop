using MediatR;
using Microsoft.Extensions.Logging;

namespace UserManagement.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid();

            _logger.LogInformation(
                "[Request Start] ID: {RequestId}, Name: {RequestName}, Time: {Timestamp}",
                requestId, requestName, DateTime.UtcNow);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var response = await next();

                stopwatch.Stop();

                _logger.LogInformation(
                    "[Request Completed] ID: {RequestId}, Name: {RequestName}, Duration: {ElapsedMs}ms, Time: {Timestamp}",
                    requestId, requestName, stopwatch.ElapsedMilliseconds, DateTime.UtcNow);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "[Request Failed] ID: {RequestId}, Name: {RequestName}, Duration: {ElapsedMs}ms, Error: {ErrorMessage}, Time: {Timestamp}",
                    requestId, requestName, stopwatch.ElapsedMilliseconds, ex.Message, DateTime.UtcNow);

                throw;
            }
        }
    }
}