using System.Net;
using System.Text.Json;
using UserManagement.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.API.Middleware
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

                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Resource Not Found",
                        Status = (int)statusCode,
                        Detail = exception.Message
                    };
                    break;

                case UnauthorizedException:
                    statusCode = HttpStatusCode.Unauthorized;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Status = (int)statusCode,
                        Detail = exception.Message
                    };
                    break;

                case BusinessRuleException:
                    statusCode = HttpStatusCode.Conflict;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Business Rule Violation",
                        Status = (int)statusCode,
                        Detail = exception.Message
                    };
                    break;

                case ConflictException:
                    statusCode = HttpStatusCode.Conflict;
                    problemDetails = new ProblemDetails
                    {
                        Title = "Conflict",
                        Status = (int)statusCode,
                        Detail = exception.Message
                    };
                    break;

                default:
                    problemDetails = new ProblemDetails
                    {
                        Title = "An error occurred",
                        Status = (int)statusCode,
                        Detail = _env.IsDevelopment() ? exception.Message : "An internal server error occurred"
                    };
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