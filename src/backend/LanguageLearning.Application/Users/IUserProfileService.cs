using LanguageLearning.Application.Common.Results;

namespace LanguageLearning.Application.Users;

public interface IUserProfileService
{
    Task<Result<CurrentUserDto>> GetCurrentAsync(CancellationToken cancellationToken);

    Task<Result<CurrentUserDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken);

    /// <summary>完成初次設定；至少需要一筆學習目標。</summary>
    Task<Result<CurrentUserDto>> CompleteOnboardingAsync(CancellationToken cancellationToken);
}
