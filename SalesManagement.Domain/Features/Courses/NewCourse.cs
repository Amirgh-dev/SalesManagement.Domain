using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Courses
{
    public sealed record NewCourseLesson(Guid? Id, string Title, bool IsOk);

    public sealed record NewCourse
    {
        public Guid? Id { get; }
        public string Title { get; }
        public string? Description { get; }
        public decimal? Price { get; }
        public string? PaymentTerms { get; }
        public DateOnly? StartDate { get; }
        public DateOnly? EndDate { get; }
        public int? CourseLevel { get; }
        public int? CityRef { get; }
        public DateOnly? Duration { get; }
        public int? CourseCapacity { get; }
        public int? RemainingCapacity { get; }
        public bool IsPaid { get; }
        public IReadOnlyList<Guid> ProfessorIds { get; }
        public IReadOnlyList<NewCourseLesson> Lessons { get; }
        public IReadOnlyList<NewCoursePlan> Plans { get; }
        public IReadOnlyList<Guid> KeepFileIds { get; }
        public IReadOnlyList<NewCourseFile> Files { get; init; } = [];

        public bool IsUpdate => Id is not null;

        public bool HasNewBanner => Files.Any(f => f.IsMain);

        public NewCourse WithFiles(IReadOnlyList<NewCourseFile> files) => this with { Files = files };

        private NewCourse(
            Guid? id, string title, string? description, decimal? price, string? paymentTerms,
            DateOnly? startDate, DateOnly? endDate, int? courseLevel, int? cityRef, DateOnly? duration,
            int? courseCapacity, int? remainingCapacity, bool isPaid,
            IReadOnlyList<Guid> professorIds, IReadOnlyList<NewCourseLesson> lessons,
            IReadOnlyList<Guid> keepFileIds, IReadOnlyList<NewCoursePlan> plans)
        {
            IsPaid = isPaid;
            KeepFileIds = keepFileIds;
            Plans = plans;
            Id = id;
            Title = title;
            Description = description;
            Price = price;
            PaymentTerms = paymentTerms;
            StartDate = startDate;
            EndDate = endDate;
            CourseLevel = courseLevel;
            CityRef = cityRef;
            Duration = duration;
            CourseCapacity = courseCapacity;
            RemainingCapacity = remainingCapacity;
            ProfessorIds = professorIds;
            Lessons = lessons;
        }

        public static NewCourse Create(
            Guid? id, string? title, string? description, decimal? price, string? paymentTerms,
            DateOnly? startDate, DateOnly? endDate, int? courseLevel, int? cityRef, DateOnly? duration,
            int? courseCapacity, int? remainingCapacity, bool isPaid,
            IEnumerable<Guid>? professorIds, IEnumerable<NewCourseLesson>? lessons,
            IEnumerable<Guid>? keepFileIds, IEnumerable<NewCoursePlan>? plans = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("عنوان دوره را وارد کنید");

            var t = title.Trim();
            var d = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            var p = string.IsNullOrWhiteSpace(paymentTerms) ? null : paymentTerms.Trim();

            if (t.Length > 50) throw new DomainException("عنوان دوره نباید بیشتر از ۵۰ کاراکتر باشد");
            if (p is { Length: > 50 }) throw new DomainException("شرایط پرداخت نباید بیشتر از ۵۰ کاراکتر باشد");
            if (price is < 0) throw new DomainException("قیمت نمی‌تواند منفی باشد");
            if (courseCapacity is < 0) throw new DomainException("ظرفیت دوره نمی‌تواند منفی باشد");
            if (remainingCapacity is < 0) throw new DomainException("ظرفیت باقی‌مانده نمی‌تواند منفی باشد");
            if (courseCapacity is not null && remainingCapacity is not null && remainingCapacity > courseCapacity)
                throw new DomainException("ظرفیت باقی‌مانده نمی‌تواند از ظرفیت دوره بیشتر باشد");
            if (startDate is not null && endDate is not null && endDate < startDate)
                throw new DomainException("تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد");
            if (cityRef is <= 0) throw new DomainException("شهرستان نامعتبر است");

            var professors = (professorIds ?? []).ToList();
            if (professors.Any(x => x == Guid.Empty)) throw new DomainException("شناسه‌ی استاد نامعتبر است");
            professors = professors.Distinct().ToList();

            var lessonList = new List<NewCourseLesson>();
            foreach (var lesson in lessons ?? [])
            {
                if (string.IsNullOrWhiteSpace(lesson.Title)) throw new DomainException("عنوان سرفصل را وارد کنید");
                var lt = lesson.Title.Trim();
                if (lt.Length > 500) throw new DomainException("عنوان سرفصل نباید بیشتر از ۵۰۰ کاراکتر باشد");
                var lid = lesson.Id is null || lesson.Id == Guid.Empty ? (Guid?)null : lesson.Id;
                lessonList.Add(new NewCourseLesson(lid, lt, lesson.IsOk));
            }

            var duplicateLessonIds = lessonList.Where(l => l.Id is not null).GroupBy(l => l.Id).Any(g => g.Count() > 1);
            if (duplicateLessonIds) throw new DomainException("شناسه‌ی سرفصل تکراری است");

            var keepIds = (keepFileIds ?? []).Where(x => x != Guid.Empty).Distinct().ToList();
            var planList = NewCoursePlan.Normalize(plans);

            var normalizedId = id is null || id == Guid.Empty ? (Guid?)null : id;

            return new NewCourse(
                normalizedId, t, d, price, p, startDate, endDate, courseLevel, cityRef, duration,
                courseCapacity, remainingCapacity, isPaid, professors, lessonList, keepIds, planList);
        }
    }
}
