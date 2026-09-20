using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Courses;
using SalesManagement.Application.Features.Courses.SaveCourse;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private const long MaxRequestBytes = 200L * 1024 * 1024;

        private readonly SaveCourseHandler _saveCourseHandler;

        public CoursesController(SaveCourseHandler saveCourseHandler)
        {
            _saveCourseHandler = saveCourseHandler;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        [ProducesResponseType(typeof(SaveCourseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SaveCourseResult>> Save(
            [FromForm] SaveCourseRequest request, CancellationToken cancellationToken)
        {
            var streams = new List<Stream>();

            try
            {
                CourseFileInput Open(IFormFile file)
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    return new CourseFileInput(stream, Path.GetExtension(file.FileName), file.Length);
                }

                var banner = request.Banner is null ? null : Open(request.Banner);
                var files = (request.Files ?? []).Select(Open).ToList();

                var result = await _saveCourseHandler.Handle(
                    new SaveCourseCommand(
                        request.Id, request.Title, request.Description, request.Price, request.PaymentTerms,
                        request.StartDate, request.EndDate, request.CourseLevel, request.CityRef, request.Duration,
                        request.CourseCapacity, request.RemainingCapacity, request.IsPaid,
                        request.ProfessorIds,
                        request.Lessons?.Select(l => new SaveCourseLessonCommand(l.Id, l.Title, l.IsOk)).ToList(),
                        banner, files, request.KeepFileIds,
                        request.Plans?.Select(p => new SaveCoursePlanCommand(
                            p.Id, p.PaymentType, p.NumberAmountPaid, p.Discount)).ToList()),
                    cancellationToken);

                return Ok(result);
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }
        }
    }
}
