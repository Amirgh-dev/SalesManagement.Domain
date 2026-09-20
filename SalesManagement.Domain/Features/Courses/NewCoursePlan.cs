using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Courses
{
    public sealed record NewCoursePlan(Guid? Id, string PaymentType, int? NumberAmountPaid, decimal? Discount)
    {
        public static IReadOnlyList<NewCoursePlan> Normalize(IEnumerable<NewCoursePlan>? plans)
        {
            var result = new List<NewCoursePlan>();

            foreach (var plan in plans ?? [])
            {
                if (string.IsNullOrWhiteSpace(plan.PaymentType))
                    throw new DomainException("نوع پرداخت را وارد کنید");

                var type = plan.PaymentType.Trim();
                if (type.Length > 50)
                    throw new DomainException("نوع پرداخت نباید بیشتر از ۵۰ کاراکتر باشد");
                if (plan.NumberAmountPaid is < 0)
                    throw new DomainException("تعداد پرداخت نمی‌تواند منفی باشد");
                if (plan.Discount is < 0 or > 100)
                    throw new DomainException("تخفیف باید درصدی بین ۰ تا ۱۰۰ باشد");

                var id = plan.Id is null || plan.Id == Guid.Empty ? (Guid?)null : plan.Id;
                result.Add(new NewCoursePlan(id, type, plan.NumberAmountPaid, plan.Discount));
            }

            if (result.Where(p => p.Id is not null).GroupBy(p => p.Id).Any(g => g.Count() > 1))
                throw new DomainException("شناسه‌ی پلن پرداخت تکراری است");

            return result;
        }
    }
}
