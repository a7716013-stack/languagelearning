using FluentValidation;
using LanguageLearning.Application.Exams;
using LanguageLearning.Application.Goals;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<ExamCatalogService>();
        services.AddScoped<ExamGoalService>();
        return services;
    }
}
