namespace SalesManagement.Application.Features.Courses.SaveCourse
{
    public record SaveCourseLessonCommand(Guid? Id, string? Title, bool IsOk);

    public record SaveCoursePlanCommand(Guid? Id, string? PaymentType, int? NumberAmountPaid, decimal? Discount);

    public record CourseFileInput(Stream Content, string? Extension, long Length);

    public record SaveCourseCommand(
        Guid? Id,
        string? Title,
        string? Description,
        decimal? Price,
        string? PaymentTerms,
        DateOnly? StartDate,
        DateOnly? EndDate,
        int? CourseLevel,
        int? CityRef,
        DateOnly? Duration,
        int? CourseCapacity,
        int? RemainingCapacity,
        bool IsPaid,
        IReadOnlyList<Guid>? ProfessorIds,
        IReadOnlyList<SaveCourseLessonCommand>? Lessons,
        CourseFileInput? Banner,
        IReadOnlyList<CourseFileInput> Files,
        IReadOnlyList<Guid>? KeepFileIds,
        IReadOnlyList<SaveCoursePlanCommand>? Plans = null);
}
