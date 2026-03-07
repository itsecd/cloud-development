using ServiceApi.Entities;

namespace ServiceApi.Generator;

  /// <summary>
  /// Служба для запуска usecase по обработке программных проектов
  /// </summary>
  /// <param name="cache">Кэш</param>
  /// <param name="logger">Логгер</param>
public class GeneratorService(IProgramProjectCache cache, ILogger<GeneratorService> logger) : IGeneratorService
{
    public async Task<ProgramProject> ProcessProgramProject(int id)
    {
        logger.LogInformation("Начало обработки программного проекта {id}", id);
        try
        {
            logger.LogInformation("Попытка получить {id} программного проекта из кэша", id);
            var programProject = await cache.GetProjectFromCache(id);
            if (programProject != null)
            {
                logger.LogInformation("Программный проект {id} был найден в кэше", id);
                return programProject;
            }
            logger.LogInformation("Программного проекта {id} нет в кэше. Создаем программный проект", id);
            programProject = ProgramProjectGenerator.GenerateProgramProject(id);
            logger.LogInformation("Сохраняем данные программного проекта {id} в кэш", id);
            await cache.SaveProjectToCache(programProject);
            return programProject;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла ошибка во время обработки программного проекта {id}.", id);
            return null;
        }
    }
}
