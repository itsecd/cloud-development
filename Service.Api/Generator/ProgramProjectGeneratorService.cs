using Bogus;
using Service.Api.Entity;

namespace Service.Api.Generator;

public class ProgramProjectGeneratorService(Faker<ProgramProject> faker)
{
    private Faker<ProgramProject> _faker = faker;

    public ProgramProject GetProgramProjectInstance(int id)
    {
        ProgramProject programProject = _faker.Generate();
        return programProject with { Id = id };

    }
}
