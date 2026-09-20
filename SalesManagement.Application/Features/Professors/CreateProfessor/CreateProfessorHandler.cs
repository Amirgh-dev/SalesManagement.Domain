using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.Professors;

namespace SalesManagement.Application.Features.Professors.CreateProfessor
{
    public class CreateProfessorHandler(
        IProfessorRepository professorRepository,
        ILogger<CreateProfessorHandler> logger)
    {
        public async Task<CreateProfessorResult> Handle(
            CreateProfessorCommand cmd, CancellationToken cancellationToken = default)
        {
            var professor = NewProfessor.Create(cmd.Id, cmd.Name, cmd.LastName, cmd.Specialization, cmd.IsOk);

            var id = await professorRepository.SaveAsync(professor, cancellationToken);

            logger.LogInformation(
                "استاد {ProfessorId} {Mode}", id, professor.IsUpdate ? "به‌روزرسانی شد" : "ثبت شد");

            return new CreateProfessorResult(id, professor.IsUpdate);
        }
    }
}
