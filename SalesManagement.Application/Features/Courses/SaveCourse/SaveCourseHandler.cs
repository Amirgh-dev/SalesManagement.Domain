using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Courses;

namespace SalesManagement.Application.Features.Courses.SaveCourse
{
    public class SaveCourseHandler(
        ICourseRepository courseRepository,
        IFileStorage fileStorage,
        ILogger<SaveCourseHandler> logger)
    {
        private const string CourseFileFolder = "Course";

        public async Task<SaveCourseResult> Handle(
            SaveCourseCommand cmd, CancellationToken cancellationToken = default)
        {
            var lessons = (cmd.Lessons ?? [])
                .Select(l => new NewCourseLesson(l.Id, l.Title ?? "", l.IsOk));

            var plans = (cmd.Plans ?? [])
                .Select(p => new NewCoursePlan(p.Id, p.PaymentType ?? "", p.NumberAmountPaid, p.Discount));

            var course = NewCourse.Create(
                cmd.Id, cmd.Title, cmd.Description, cmd.Price, cmd.PaymentTerms,
                cmd.StartDate, cmd.EndDate, cmd.CourseLevel, cmd.CityRef, cmd.Duration,
                cmd.CourseCapacity, cmd.RemainingCapacity, cmd.IsPaid,
                cmd.ProfessorIds, lessons, cmd.KeepFileIds, plans);

            if (cmd.Banner is not null)
                CourseFileRules.Validate(cmd.Banner.Extension, cmd.Banner.Length, isBanner: true);
            foreach (var file in cmd.Files)
                CourseFileRules.Validate(file.Extension, file.Length, isBanner: false);

            var savedNames = new List<string>();
            var newFiles = new List<NewCourseFile>();
            try
            {
                if (cmd.Banner is not null)
                    newFiles.Add(await SaveFileAsync(cmd.Banner, isMain: true, savedNames, cancellationToken));
                foreach (var file in cmd.Files)
                    newFiles.Add(await SaveFileAsync(file, isMain: false, savedNames, cancellationToken));

                course = course.WithFiles(newFiles);

                var (id, removedFileNames) = await courseRepository.SaveAsync(course, cancellationToken);

                foreach (var fileName in removedFileNames)
                    await fileStorage.DeleteAsync(CourseFileFolder, fileName, CancellationToken.None);

                logger.LogInformation("دوره {CourseId} {Mode}", id, course.IsUpdate ? "به‌روزرسانی شد" : "ثبت شد");

                return new SaveCourseResult(id, course.IsUpdate);
            }
            catch
            {
                foreach (var fileName in savedNames)
                    await fileStorage.DeleteAsync(CourseFileFolder, fileName, CancellationToken.None);
                throw;
            }
        }

        private async Task<NewCourseFile> SaveFileAsync(
            CourseFileInput input, bool isMain, List<string> savedNames, CancellationToken ct)
        {
            var ext = CourseFileRules.NormalizeExtension(input.Extension);
            var savedName = await fileStorage.SaveAsync(input.Content, CourseFileFolder, ext, ct);
            savedNames.Add(savedName);

            return new NewCourseFile(Path.GetFileNameWithoutExtension(savedName), Path.GetExtension(savedName), isMain);
        }
    }
}
