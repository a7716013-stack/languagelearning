using LanguageLearning.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Common;

/// <summary>統一的 ProblemDetails（RFC 9457）輸出，包含穩定的錯誤代碼 <c>code</c>。</summary>
public static class Problems
{
    public static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Locked => StatusCodes.Status423Locked,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static string TitleFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "請求內容有誤",
        StatusCodes.Status401Unauthorized => "尚未登入",
        StatusCodes.Status403Forbidden => "沒有權限",
        StatusCodes.Status404NotFound => "找不到資源",
        StatusCodes.Status405MethodNotAllowed => "不支援的請求方法",
        StatusCodes.Status409Conflict => "資料衝突",
        StatusCodes.Status423Locked => "帳號已鎖定",
        StatusCodes.Status429TooManyRequests => "請求太頻繁",
        _ => "伺服器發生錯誤",
    };

    public static IResult ToProblemResult(this AppError error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.Kind == ErrorKind.Validation && error.FieldErrors is { Count: > 0 })
        {
            return TypedResults.ValidationProblem(
                error.FieldErrors.ToDictionary(pair => ToCamelCase(pair.Key), pair => pair.Value),
                detail: error.Message,
                title: "資料驗證失敗",
                extensions: extensions);
        }

        var status = StatusCodeFor(error.Kind);
        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: TitleFor(status),
            extensions: extensions);
    }

    public static IResult ValidationFailed(IDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(
            errors,
            detail: "輸入資料有誤，請檢查後再試。",
            title: "資料驗證失敗",
            extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });

    /// <summary>在 Middleware 或 Cookie 事件中直接寫出 ProblemDetails。</summary>
    public static async Task WriteAsync(HttpContext httpContext, int statusCode, string code, string detail)
    {
        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = TitleFor(statusCode),
            Detail = detail,
        };
        problem.Extensions["code"] = code;

        var service = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        if (!await service.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }))
        {
            await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }

    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // FluentValidation 巢狀屬性以 "." 分隔，逐段轉換。
        return string.Join('.', name.Split('.').Select(part =>
            part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
    }
}
