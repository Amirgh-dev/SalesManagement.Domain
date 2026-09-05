using System.Net;
using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.API.Common.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                await WriteResponse(context, HttpStatusCode.BadRequest, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای غیرمنتظره در {Path}", context.Request.Path);
                await WriteResponse(context, HttpStatusCode.InternalServerError, new { error = "خطای داخلی سرور" });
            }
        }

        private static async Task WriteResponse(HttpContext context, HttpStatusCode statusCode, object payload)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}