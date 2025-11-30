using MediatR;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Exceptions;

namespace ProductManagement.Application.Behaviors
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
            catch (ProductNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product not found while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (ProductAccessDeniedException ex)
            {
                _logger.LogWarning(ex, "Product access denied while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (UserNotActiveException ex)
            {
                _logger.LogWarning(ex, "User not active while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (ProductNameConflictException ex)
            {
                _logger.LogWarning(ex, "Product name conflict while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (ProductBusinessRuleException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (BulkOperationException ex)
            {
                _logger.LogWarning(ex, "Bulk operation failed while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (ProductServiceCommunicationException ex)
            {
                _logger.LogError(ex, "Service communication error while processing request {RequestType}",
                    typeof(TRequest).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred while processing request {RequestType}",
                    typeof(TRequest).Name);

                throw new ApplicationException(
                    $"An error occurred while processing your request. Request type: {typeof(TRequest).Name}", ex);
            }
        }
    }
}