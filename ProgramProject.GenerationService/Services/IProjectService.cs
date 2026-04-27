using ProgramProject.GenerationService.Models;

namespace ProgramProject.GenerationService.Services;

public interface IProjectService
{ 
    public Task<ProgramProjectModel> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default);
}
