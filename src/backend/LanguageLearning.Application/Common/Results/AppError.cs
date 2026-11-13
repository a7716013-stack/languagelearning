namespace LanguageLearning.Application.Common.Results;

public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Locked,
    Failure,
}

/// <summary>
/// 應用層錯誤。<see cref="Code"/> 是給前端判斷用的穩定代碼，<see cref="Message"/> 是給使用者看的繁體中文訊息。
/// </summary>
public sealed record AppError(
    string Code,
    string Message,
    ErrorKind Kind,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static AppError Validation(string code, string message, string? field = null) =>
        new(
            code,
            message,
            ErrorKind.Validation,
            field is null ? null : new Dictionary<string, string[]> { [field] = [message] });

    public static AppError Validation(string code, string message, IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(code, message, ErrorKind.Validation, fieldErrors);

    public static AppError Unauthorized(string code, string message) => new(code, message, ErrorKind.Unauthorized);

    public static AppError Forbidden(string code, string message) => new(code, message, ErrorKind.Forbidden);

    public static AppError NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static AppError Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static AppError Locked(string code, string message) => new(code, message, ErrorKind.Locked);

    public static AppError Failure(string code, string message) => new(code, message, ErrorKind.Failure);
}
