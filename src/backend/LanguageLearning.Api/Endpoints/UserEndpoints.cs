using LanguageLearning.Api.Common;
using LanguageLearning.Application.Goals;
using LanguageLearning.Application.Users;

namespace LanguageLearning.Api.Endpoints;

/// <summary>目前登入者的個人資料與學習目標。所有端點只能存取自己的資料。</summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet(string.Empty, async (IUserProfileService profiles, CancellationToken ct) =>
                (await profiles.GetCurrentAsync(ct)).ToHttpResult(user => TypedResults.Ok(user)))
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut(string.Empty, async (UpdateProfileRequest request, IUserProfileService profiles, CancellationToken ct) =>
                (await profiles.UpdateAsync(request, ct)).ToHttpResult(user => TypedResults.Ok(user)))
            .WithValidation<UpdateProfileRequest>()
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/exam-goals", async (ExamGoalService goals, CancellationToken ct) =>
                (await goals.GetMyGoalsAsync(ct)).ToHttpResult(items => TypedResults.Ok(items)))
            .Produces<IReadOnlyList<ExamGoalDto>>();

        group.MapPut(
                "/exam-goals/{examTypeCode}",
                async (string examTypeCode, UpsertExamGoalRequest request, ExamGoalService goals, CancellationToken ct) =>
                    (await goals.UpsertAsync(examTypeCode, request, ct)).ToHttpResult(goal => TypedResults.Ok(goal)))
            .WithValidation<UpsertExamGoalRequest>()
            .Produces<ExamGoalDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/onboarding/complete", async (IUserProfileService profiles, CancellationToken ct) =>
                (await profiles.CompleteOnboardingAsync(ct)).ToHttpResult(user => TypedResults.Ok(user)))
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
