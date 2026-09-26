using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace SalesManagement.API.Common
{
    public static class ProblemResponseWriter
    {
        public static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
        {
            var problem = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
            };

            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsJsonAsync(problem);
        }
    }
}
