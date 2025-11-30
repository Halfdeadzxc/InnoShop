using System.Net;
using System.Text.Json;
using ProductManagement.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace ProductManagement.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var problemDetails = new ProblemDetails();

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = HttpStatusCode.BadRequest;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Validation Error",
                        Status = (int)statusCode,
                        Detail = validationEx.Message,
                        Extensions = { ["errors"] = validationEx.Errors }
                    };
                    break;

                case ProductNotFoundException productEx:
                    statusCode = HttpStatusCode.NotFound;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Product Not Found",
                        Status = (int)statusCode,
                        Detail = productEx.Message
                    };
                    break;

                case ProductAccessDeniedException accessEx:
                    statusCode = HttpStatusCode.Forbidden;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Access Denied",
                        Status = (int)statusCode,
                        Detail = accessEx.Message
                    };
                    break;

                case UserNotFoundException userEx:
                    statusCode = HttpStatusCode.NotFound;
                    problemDetails = new ProblemDetails
                    {
                        Title = "User Not Found",
                        Status = (int)statusCode,
                        Detail = userEx.Message
                    };
                    break;

                case UserNotActiveException userActiveEx:
                    statusCode = HttpStatusCode.BadRequest;
                    problemDetails = new ProblemDetails
                    {
                        Title = "User Not Active",
                        Status = (int)statusCode,
                        Detail = userActiveEx.Message
                    };
                    break;

                case ProductNameConflictException conflictEx:
                    statusCode = HttpStatusCode.Conflict;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Product Name Conflict",
                        Status = (int)statusCode,
                        Detail = conflictEx.Message
                    };
                    break;

                case UnauthorizedAccessException authEx:
                    statusCode = HttpStatusCode.Unauthorized;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Status = (int)statusCode,
                        Detail = authEx.Message
                    };
                    break;

                case BulkOperationException bulkEx:
                    statusCode = HttpStatusCode.BadRequest;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Bulk Operation Failed",
                        Status = (int)statusCode,
                        Detail = bulkEx.Message,
                        Extensions = { ["failedProductIds"] = bulkEx.FailedProductIds }
                    };
                    break;

                case ProductServiceCommunicationException serviceEx:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Service Unavailable",
                        Status = (int)statusCode,
                        Detail = "User service is temporarily unavailable"
                    };
                    break;

                default:
                    problemDetails = new ProblemDetails
                    {
                        Title = "An error occurred",
                        Status = (int)statusCode,
                        Detail = _env.IsDevelopment() ? exception.Message : "An internal server error occurred"
                    };

                    if (_env.IsDevelopment())
                    {
                        problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                        problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
                    }
                    break;
            }

            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(problemDetails, options);

            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}