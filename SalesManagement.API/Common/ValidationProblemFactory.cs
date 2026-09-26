using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace SalesManagement.API.Common
{
    public static partial class ValidationProblemFactory
    {
        public static IActionResult Create(ActionContext context)
        {
            var errors = context.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    e => Display(e.Key),
                    e => e.Value!.Errors.Select(x => Translate(Display(e.Key), x.ErrorMessage)).ToArray());

            var detail = errors.Values.SelectMany(v => v).FirstOrDefault() ?? "درخواست نامعتبر است";

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "درخواست نامعتبر",
                Detail = detail,
                Instance = context.HttpContext.Request.Path,
            };

            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        }

        internal static string Translate(string field, string message)
        {
            if (ContainsPersian(message)) return message;

            var m = RequiredPattern().Match(message);
            if (m.Success) return $"فیلد {m.Groups[1].Value} الزامی است";

            m = RangePattern().Match(message);
            if (m.Success) return $"مقدار {m.Groups[1].Value} باید بین {m.Groups[2].Value} و {m.Groups[3].Value} باشد";

            m = MaxLengthPattern().Match(message);
            if (m.Success) return $"طول {m.Groups[1].Value} نباید بیشتر از {m.Groups[2].Value} کاراکتر باشد";

            if (message.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase))
                return "بدنه‌ی درخواست خالی است";

            if (string.IsNullOrEmpty(field) || field == "request")
                return "درخواست نامعتبر است";

            return $"مقدار فیلد {field} نامعتبر است";
        }

        private static string Display(string key)
        {
            var k = key.StartsWith("$.") ? key[2..] : key;
            return k;
        }

        private static bool ContainsPersian(string text) => text.Any(c => c is >= '؀' and <= 'ۿ');

        [GeneratedRegex(@"^The (.+?) field is required\.$")]
        private static partial Regex RequiredPattern();

        [GeneratedRegex(@"^The field (.+?) must be between (.+?) and (.+?)\.$")]
        private static partial Regex RangePattern();

        [GeneratedRegex(@"^The field (.+?) must be a string(?: or array type)? with a maximum length of '?(\d+)'?\.$")]
        private static partial Regex MaxLengthPattern();
    }
}
