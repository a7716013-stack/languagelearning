namespace LanguageLearning.Application.Abstractions;

/// <summary>目前請求的使用者資訊。</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);

    string? IpAddress { get; }
}
