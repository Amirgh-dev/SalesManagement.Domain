using System.Net;
using Microsoft.AspNetCore.Mvc;
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
            catch (AuthenticationException ex)
            {
                await WriteProblem(context, HttpStatusCode.Unauthorized, "خطای احراز هویت", ex.Message);
            }
            catch (TooManyRequestsException ex)
            {
                context.Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
                await WriteProblem(context, HttpStatusCode.TooManyRequests, "تعداد تلاش زیاد", ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await WriteProblem(context, HttpStatusCode.Forbidden, "دسترسی غیرمجاز", ex.Message);
            }
            catch (NotFoundException ex)
            {
                await WriteProblem(context, HttpStatusCode.NotFound, "پیدا نشد", ex.Message);
            }
            catch (DomainException ex)
            {
                await WriteProblem(context, HttpStatusCode.BadRequest, "درخواست نامعتبر", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای غیرمنتظره در {Path}", context.Request.Path);
                await WriteProblem(context, HttpStatusCode.InternalServerError, "خطای داخلی سرور", "خطای داخلی سرور");
            }
        }

        private static Task WriteProblem(
            HttpContext context, HttpStatusCode statusCode, string title, string detail)
        {
            var problem = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsJsonAsync(problem);
        }
    }
}
