using MediatR;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Exceptions;

namespace UserManagement.Application.Behaviors
{
    public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

        public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Authorization failed while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflict detected while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred while processing request {RequestType}",
                    typeof(TRequest).Name);

                throw new System.ApplicationException(
                    $"An error occurred while processing your request. Request type: {typeof(TRequest).Name}", ex);
            }
        }
    }
}