using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Courses;

namespace SalesManagement.Tests.Features.Courses
{
    public class NewCourseTests
    {
        private static NewCourse Make(
            Guid? id = null, string? title = "Course", decimal? price = 100, int? capacity = 10, int? remaining = 10,
            DateOnly? start = null, DateOnly? end = null, int? cityRef = null,
            IEnumerable<Guid>? professors = null, IEnumerable<NewCourseLesson>? lessons = null,
            bool isPaid = false, IEnumerable<Guid>? keepFileIds = null)
            => NewCourse.Create(id, title, null, price, null, start, end, 1, cityRef, null,
                capacity, remaining, isPaid, professors, lessons, keepFileIds);

        [Fact]
        public void No_id_is_insert_id_is_update_and_empty_guid_is_insert()
        {
            Assert.False(Make().IsUpdate);
            Assert.False(Make(id: Guid.Empty).IsUpdate);
            Assert.True(Make(id: Guid.NewGuid()).IsUpdate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("  ")]
        public void Title_required(string? title)
            => Assert.Throws<DomainException>(() => Make(title: title));

        [Fact]
        public void Title_too_long_throws()
            => Assert.Throws<DomainException>(() => Make(title: new string('a', 51)));

        [Fact]
        public void Negative_price_or_capacity_throws()
        {
            Assert.Throws<DomainException>(() => Make(price: -1));
            Assert.Throws<DomainException>(() => Make(capacity: -1));
            Assert.Throws<DomainException>(() => Make(remaining: -1));
        }

        [Fact]
        public void Remaining_greater_than_capacity_throws()
            => Assert.Throws<DomainException>(() => Make(capacity: 5, remaining: 6));

        [Fact]
        public void End_before_start_throws()
            => Assert.Throws<DomainException>(() =>
                Make(start: new DateOnly(2026, 10, 10), end: new DateOnly(2026, 10, 9)));

        [Fact]
        public void Professors_are_distinct_and_empty_guid_rejected()
        {
            var a = Guid.NewGuid();
            Assert.Single(Make(professors: [a, a]).ProfessorIds);
            Assert.Throws<DomainException>(() => Make(professors: [Guid.Empty]));
        }

        [Fact]
        public void Lessons_are_trimmed_blank_rejected_and_duplicate_ids_rejected()
        {
            var c = Make(lessons: [new NewCourseLesson(null, "  L1 ", true)]);
            Assert.Equal("L1", c.Lessons[0].Title);

            Assert.Throws<DomainException>(() => Make(lessons: [new NewCourseLesson(null, " ", true)]));

            var id = Guid.NewGuid();
            Assert.Throws<DomainException>(() => Make(lessons: [
                new NewCourseLesson(id, "a", true), new NewCourseLesson(id, "b", true)]));
        }

        [Fact]
        public void Null_lists_become_empty()
        {
            var c = Make();
            Assert.Empty(c.ProfessorIds);
            Assert.Empty(c.Lessons);
        }

        [Fact]
        public void Plans_are_trimmed_and_ids_normalized()
        {
            var id = Guid.NewGuid();
            var c = NewCourse.Create(null, "T", null, null, null, null, null, null, null, null, null, null, false,
                null, null, null,
                [new NewCoursePlan(null, "  cash ", 1, 10), new NewCoursePlan(Guid.Empty, "inst", 6, null), new NewCoursePlan(id, "x", null, 0)]);
            Assert.Equal("cash", c.Plans[0].PaymentType);
            Assert.Null(c.Plans[1].Id);
            Assert.Equal(id, c.Plans[2].Id);
        }

        [Theory]
        [InlineData(" ", 1, 10)]
        [InlineData("cash", -1, 10)]
        [InlineData("cash", 1, -1)]
        [InlineData("cash", 1, 101)]
        public void Invalid_plan_throws(string type, int count, int discount)
            => Assert.Throws<DomainException>(() => NewCoursePlan.Normalize([new NewCoursePlan(null, type, count, discount)]));

        [Fact]
        public void Plan_type_too_long_or_duplicate_id_throws()
        {
            Assert.Throws<DomainException>(() => NewCoursePlan.Normalize([new NewCoursePlan(null, new string('a', 51), 1, 1)]));
            var id = Guid.NewGuid();
            Assert.Throws<DomainException>(() => NewCoursePlan.Normalize(
                [new NewCoursePlan(id, "a", 1, 1), new NewCoursePlan(id, "b", 1, 1)]));
        }

        [Fact]
        public void IsPaid_is_kept()
        {
            Assert.True(Make(isPaid: true).IsPaid);
            Assert.False(Make().IsPaid);
        }

        [Fact]
        public void KeepFileIds_drop_empty_and_duplicates()
        {
            var a = Guid.NewGuid();
            var c = Make(keepFileIds: [a, a, Guid.Empty]);
            Assert.Equal([a], c.KeepFileIds);
        }

        [Fact]
        public void WithFiles_sets_files_and_detects_banner()
        {
            var c = Make();
            Assert.False(c.HasNewBanner);
            var withBanner = c.WithFiles([new NewCourseFile("a", ".png", true), new NewCourseFile("b", ".mp4", false)]);
            Assert.True(withBanner.HasNewBanner);
            Assert.Equal(2, withBanner.Files.Count);
        }

        [Theory]
        [InlineData(".jpg", 100, false)]
        [InlineData("PNG", 100, true)]
        [InlineData(".mp4", 100, false)]
        [InlineData(".webm", 49L * 1024 * 1024, false)]
        public void File_rules_accept_valid(string ext, long len, bool banner)
            => CourseFileRules.Validate(ext, len, banner);

        [Theory]
        [InlineData(".exe", 100, false)]
        [InlineData("", 100, false)]
        [InlineData(".jpg", 0, false)]
        [InlineData(".jpg", 6L * 1024 * 1024, false)]
        [InlineData(".mp4", 51L * 1024 * 1024, false)]
        [InlineData(".mp4", 100, true)]
        public void File_rules_reject_invalid(string ext, long len, bool banner)
            => Assert.Throws<DomainException>(() => CourseFileRules.Validate(ext, len, banner));
    }
}
