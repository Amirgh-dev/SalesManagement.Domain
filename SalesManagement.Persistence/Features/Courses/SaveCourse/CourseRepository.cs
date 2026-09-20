using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Courses;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Courses.SaveCourse
{
    public class CourseRepository(IZanisServicesConnectionFactory connectionFactory) : ICourseRepository
    {
        private const string SqlCityExists = """
            SELECT TOP (1) 1 FROM [Sepidar02].[GNR].[Location] WHERE LocationId = @CityRef AND Type = 3;
            """;

        private const string SqlCourseExists = "SELECT TOP (1) 1 FROM dbo.Course WHERE Id = @Id;";

        private const string SqlCountProfessors = "SELECT COUNT(*) FROM dbo.Professors WHERE Id IN @Ids;";

        private const string SqlInsertCourse = """
            INSERT INTO dbo.Course
                (Id, Title, Description, Price, PaymentTerms, StartDate, EndDate, CourseLevel,
                 CategoryRef, CityRef, Duration, IsBlackList, CourseCapacity, RemainingCapacity, IsPaid)
            VALUES
                (@Id, @Title, @Description, @Price, @PaymentTerms, @StartDate, @EndDate, @CourseLevel,
                 NULL, @CityRef, @Duration, 0, @CourseCapacity, @RemainingCapacity, @IsPaid);
            """;

        private const string SqlUpdateCourse = """
            UPDATE dbo.Course
            SET Title = @Title, Description = @Description, Price = @Price, PaymentTerms = @PaymentTerms,
                StartDate = @StartDate, EndDate = @EndDate, CourseLevel = @CourseLevel, CityRef = @CityRef,
                Duration = @Duration, CourseCapacity = @CourseCapacity, RemainingCapacity = @RemainingCapacity, IsPaid = @IsPaid
            WHERE Id = @Id;
            """;

        private const string SqlDeleteProfessorsNotIn =
            "DELETE FROM dbo.CourseProfessors WHERE CourseRef = @CourseRef AND ProfessorRef NOT IN @Ids;";

        private const string SqlDeleteAllProfessors = "DELETE FROM dbo.CourseProfessors WHERE CourseRef = @CourseRef;";

        private const string SqlSelectProfessorRefs =
            "SELECT ProfessorRef FROM dbo.CourseProfessors WHERE CourseRef = @CourseRef;";

        private const string SqlInsertProfessor =
            "INSERT INTO dbo.CourseProfessors (Id, ProfessorRef, CourseRef) VALUES (NEWID(), @ProfessorRef, @CourseRef);";

        private const string SqlSelectLessonIds = "SELECT Id FROM dbo.CourseLessons WHERE CourseRef = @CourseRef;";

        private const string SqlDeleteLesson = "DELETE FROM dbo.CourseLessons WHERE Id = @Id AND CourseRef = @CourseRef;";

        private const string SqlUpdateLesson =
            "UPDATE dbo.CourseLessons SET Title = @Title, IsOk = @IsOk WHERE Id = @Id AND CourseRef = @CourseRef;";

        private const string SqlInsertLesson =
            "INSERT INTO dbo.CourseLessons (Id, CourseRef, Title, IsOk) VALUES (@Id, @CourseRef, @Title, @IsOk);";

        public const string FileCode = "PA-30001";
        public const string FileTable = "Course";

        private const string SqlSelectFiles = """
            SELECT Id, FileName, FileExtensions, IsMain
            FROM [ZanisLightCore_db].[dbo].[FileUploads]
            WHERE Ref = @CourseRef AND Code = @Code AND [Tables] = @Table;
            """;

        private const string SqlDeleteFile = """
            DELETE FROM [ZanisLightCore_db].[dbo].[FileUploads] WHERE Id = @Id;
            """;

        private const string SqlInsertFile = """
            INSERT INTO [ZanisLightCore_db].[dbo].[FileUploads]
                (Id, Code, Ref, FileName, FileExtensions, IsOk, IsMain, [Tables])
            VALUES
                (NEWID(), @Code, @CourseRef, @FileName, @FileExtensions, 1, @IsMain, @Table);
            """;

        public async Task<(Guid Id, IReadOnlyList<string> RemovedFileNames)> SaveAsync(
            NewCourse course, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                if (course.CityRef is not null)
                {
                    var city = await connection.ExecuteScalarAsync<int?>(
                        SqlCityExists, new { course.CityRef }, tx);
                    if (city is null)
                        throw new DomainException("شهرستان پیدا نشد");
                }

                if (course.ProfessorIds.Count > 0)
                {
                    var found = await connection.ExecuteScalarAsync<int>(
                        SqlCountProfessors, new { Ids = course.ProfessorIds }, tx);
                    if (found != course.ProfessorIds.Count)
                        throw new DomainException("استاد انتخاب‌شده پیدا نشد");
                }

                var id = course.Id ?? Guid.NewGuid();
                var args = new
                {
                    Id = id,
                    course.Title,
                    course.Description,
                    course.Price,
                    course.PaymentTerms,
                    StartDate = course.StartDate?.ToDateTime(TimeOnly.MinValue),
                    EndDate = course.EndDate?.ToDateTime(TimeOnly.MinValue),
                    course.CourseLevel,
                    course.CityRef,
                    Duration = course.Duration?.ToDateTime(TimeOnly.MinValue),
                    course.CourseCapacity,
                    course.RemainingCapacity,
                    course.IsPaid,
                };

                if (course.IsUpdate)
                {
                    var exists = await connection.ExecuteScalarAsync<int?>(SqlCourseExists, new { Id = id }, tx);
                    if (exists is null)
                        throw new DomainException("دوره پیدا نشد");

                    await connection.ExecuteAsync(SqlUpdateCourse, args, tx);
                }
                else
                {
                    await connection.ExecuteAsync(SqlInsertCourse, args, tx);
                }

                await SyncProfessorsAsync(connection, tx, id, course.ProfessorIds);
                await SyncLessonsAsync(connection, tx, id, course.Lessons);
                await SyncPlansAsync(connection, tx, id, course.Plans);
                var removedFiles = await SyncFilesAsync(connection, tx, id, course);

                await tx.CommitAsync(cancellationToken);
                return (id, removedFiles);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private const string SqlSelectPlanIds = "SELECT Id FROM dbo.CoursePlan WHERE CourseRef = @CourseRef;";

        private const string SqlDeletePlan = "DELETE FROM dbo.CoursePlan WHERE Id = @Id AND CourseRef = @CourseRef;";

        private const string SqlUpdatePlan = """
            UPDATE dbo.CoursePlan
            SET PaymentType = @PaymentType, NumberAmountPaid = @NumberAmountPaid, Discount = @Discount
            WHERE Id = @Id AND CourseRef = @CourseRef;
            """;

        private const string SqlInsertPlan = """
            INSERT INTO dbo.CoursePlan (Id, CourseRef, PaymentType, NumberAmountPaid, Discount)
            VALUES (@Id, @CourseRef, @PaymentType, @NumberAmountPaid, @Discount);
            """;

        private static async Task SyncPlansAsync(
            DbConnection connection, DbTransaction tx, Guid courseId, IReadOnlyList<NewCoursePlan> plans)
        {
            var existing = (await connection.QueryAsync<Guid>(
                SqlSelectPlanIds, new { CourseRef = courseId }, tx)).ToHashSet();

            var keptIds = plans.Where(p => p.Id is not null && existing.Contains(p.Id.Value))
                .Select(p => p.Id!.Value).ToHashSet();

            foreach (var staleId in existing.Where(e => !keptIds.Contains(e)))
                await connection.ExecuteAsync(SqlDeletePlan, new { Id = staleId, CourseRef = courseId }, tx);

            foreach (var plan in plans)
            {
                var isExisting = plan.Id is not null && keptIds.Contains(plan.Id.Value);
                await connection.ExecuteAsync(
                    isExisting ? SqlUpdatePlan : SqlInsertPlan,
                    new
                    {
                        Id = isExisting ? plan.Id!.Value : Guid.NewGuid(),
                        CourseRef = courseId,
                        plan.PaymentType,
                        plan.NumberAmountPaid,
                        plan.Discount,
                    }, tx);
            }
        }

        private static async Task<IReadOnlyList<string>> SyncFilesAsync(
            DbConnection connection, DbTransaction tx, Guid courseId, NewCourse course)
        {
            var removed = new List<string>();

            var existing = (await connection.QueryAsync<ExistingFileRow>(
                SqlSelectFiles, new { CourseRef = courseId, Code = FileCode, Table = FileTable }, tx)).ToList();

            foreach (var file in existing)
            {
                var keep = file.IsMain
                    ? !course.HasNewBanner
                    : course.KeepFileIds.Contains(file.Id);
                if (keep) continue;

                await connection.ExecuteAsync(SqlDeleteFile, new { file.Id }, tx);
                removed.Add(file.FileName + file.FileExtensions);
            }

            foreach (var file in course.Files)
            {
                await connection.ExecuteAsync(
                    SqlInsertFile,
                    new
                    {
                        Code = FileCode,
                        CourseRef = courseId,
                        file.FileName,
                        FileExtensions = file.Extension,
                        file.IsMain,
                        Table = FileTable,
                    }, tx);
            }

            return removed;
        }

        private sealed class ExistingFileRow
        {
            public Guid Id { get; set; }
            public string FileName { get; set; } = "";
            public string FileExtensions { get; set; } = "";
            public bool IsMain { get; set; }
        }

        private static async Task SyncProfessorsAsync(
            DbConnection connection, DbTransaction tx, Guid courseId, IReadOnlyList<Guid> professorIds)
        {
            if (professorIds.Count == 0)
            {
                await connection.ExecuteAsync(SqlDeleteAllProfessors, new { CourseRef = courseId }, tx);
                return;
            }

            await connection.ExecuteAsync(
                SqlDeleteProfessorsNotIn, new { CourseRef = courseId, Ids = professorIds }, tx);

            var existing = (await connection.QueryAsync<Guid>(
                SqlSelectProfessorRefs, new { CourseRef = courseId }, tx)).ToHashSet();

            foreach (var professorId in professorIds.Where(p => !existing.Contains(p)))
                await connection.ExecuteAsync(
                    SqlInsertProfessor, new { ProfessorRef = professorId, CourseRef = courseId }, tx);
        }

        private static async Task SyncLessonsAsync(
            DbConnection connection, DbTransaction tx, Guid courseId, IReadOnlyList<NewCourseLesson> lessons)
        {
            var existing = (await connection.QueryAsync<Guid>(
                SqlSelectLessonIds, new { CourseRef = courseId }, tx)).ToHashSet();

            var keptIds = lessons.Where(l => l.Id is not null && existing.Contains(l.Id.Value))
                .Select(l => l.Id!.Value).ToHashSet();

            foreach (var staleId in existing.Where(e => !keptIds.Contains(e)))
                await connection.ExecuteAsync(SqlDeleteLesson, new { Id = staleId, CourseRef = courseId }, tx);

            foreach (var lesson in lessons)
            {
                if (lesson.Id is not null && keptIds.Contains(lesson.Id.Value))
                {
                    await connection.ExecuteAsync(
                        SqlUpdateLesson,
                        new { Id = lesson.Id.Value, CourseRef = courseId, lesson.Title, lesson.IsOk }, tx);
                }
                else
                {
                    await connection.ExecuteAsync(
                        SqlInsertLesson,
                        new { Id = Guid.NewGuid(), CourseRef = courseId, lesson.Title, lesson.IsOk }, tx);
                }
            }
        }
    }
}
