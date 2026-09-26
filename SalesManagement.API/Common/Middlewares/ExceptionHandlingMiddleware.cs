using System.Net;
using Microsoft.AspNetCore.Http;
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
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.Unauthorized, "خطای احراز هویت", ex.Message);
            }
            catch (TooManyRequestsException ex)
            {
                context.Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.TooManyRequests, "تعداد تلاش زیاد", ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.Forbidden, "دسترسی غیرمجاز", ex.Message);
            }
            catch (ConflictException ex)
            {
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.Conflict, "تکراری", ex.Message);
            }
            catch (NotFoundException ex)
            {
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.NotFound, "پیدا نشد", ex.Message);
            }
            catch (DomainException ex)
            {
                await ProblemResponseWriter.WriteAsync(context, HttpStatusCode.BadRequest, "درخواست نامعتبر", ex.Message);
            }
            catch (BadHttpRequestException ex)
            {
                var detail = ex.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "حجم درخواست بیش از حد مجاز است"
                    : "درخواست نامعتبر است";
                await ProblemResponseWriter.WriteAsync(context, (HttpStatusCode)ex.StatusCode, "درخواست نامعتبر", detail);
            }
            catch (InvalidDataException ex) when (ex.Message.Contains("Multipart body length limit"))
            {
                await ProblemResponseWriter.WriteAsync(
                    context, HttpStatusCode.RequestEntityTooLarge, "درخواست نامعتبر", "حجم فایل‌های ارسالی بیش از حد مجاز است");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطای غیرمنتظره در {Path}", context.Request.Path);
                await ProblemResponseWriter.WriteAsync(
                    context, HttpStatusCode.InternalServerError, "خطای داخلی سرور", "خطای داخلی سرور");
            }
        }
    }
}
