namespace LanguageLearning.Application.Abstractions;

/// <summary>帳號相關系統信件的種類。</summary>
public enum AccountEmailKind
{
    ConfirmEmail = 1,
    AccountAlreadyExists = 2,
    ResetPassword = 3,
}

/// <summary>
/// 將系統信件排入寄件匣，由背景服務非同步寄出。
/// 寄件匣只保存信件種類與使用者，驗證或重設用的 Token 在實際寄出時才產生，不會存進資料庫。
/// </summary>
public interface IAccountEmailQueue
{
    Task EnqueueAsync(AccountEmailKind kind, Guid userId, CancellationToken cancellationToken);
}
