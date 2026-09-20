using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Courses
{
    public class SaveCourseLessonRequest
    {
        public Guid? Id { get; init; }
        [Required][MaxLength(500)] public string Title { get; init; } = "";
        public bool IsOk { get; init; }
    }

    public class SaveCoursePlanRequest
    {
        public Guid? Id { get; init; }
        [Required][MaxLength(50)] public string PaymentType { get; init; } = "";
        [Range(0, int.MaxValue)] public int? NumberAmountPaid { get; init; }
        [Range(0, 100)] public decimal? Discount { get; init; }
    }

    public class SaveCourseRequest
    {
        public Guid? Id { get; init; }
        [Required][MaxLength(50)] public string Title { get; init; } = "";
        public string? Description { get; init; }
        [Range(0, double.MaxValue)] public decimal? Price { get; init; }
        [MaxLength(50)] public string? PaymentTerms { get; init; }
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public int? CourseLevel { get; init; }
        public int? CityRef { get; init; }
        public DateOnly? Duration { get; init; }
        [Range(0, int.MaxValue)] public int? CourseCapacity { get; init; }
        [Range(0, int.MaxValue)] public int? RemainingCapacity { get; init; }
        public bool IsPaid { get; init; }
        public List<Guid>? ProfessorIds { get; init; }
        public List<SaveCourseLessonRequest>? Lessons { get; init; }
        public IFormFile? Banner { get; init; }
        public List<IFormFile>? Files { get; init; }
        public List<Guid>? KeepFileIds { get; init; }
        public List<SaveCoursePlanRequest>? Plans { get; init; }
    }
}
