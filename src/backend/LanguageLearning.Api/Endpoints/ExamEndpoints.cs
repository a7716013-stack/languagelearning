using LanguageLearning.Application.Exams;

namespace LanguageLearning.Api.Endpoints;

public static class ExamEndpoints
{
    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exam-types", async (ExamCatalogService catalog, CancellationToken ct) =>
                TypedResults.Ok(await catalog.GetExamTypesAsync(ct)))
            .WithTags("Exams")
            .WithSummary("考試類型與開放狀態")
            .AllowAnonymous();

        app.MapGet("/api/exam-versions", async (string? examType, ExamCatalogService catalog, CancellationToken ct) =>
                TypedResults.Ok(await catalog.GetExamVersionsAsync(examType, ct)))
            .WithTags("Exams")
            .WithSummary("考制版本，可用 examType 篩選")
            .AllowAnonymous();

        return app;
    }
}
