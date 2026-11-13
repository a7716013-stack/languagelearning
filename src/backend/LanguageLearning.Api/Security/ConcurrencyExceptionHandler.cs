using LanguageLearning.Api.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Api.Security;

/// <summary>未被應用層處理的並行衝突，統一回傳 409。</summary>
public sealed class ConcurrencyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        await Problems.WriteAsync(
            httpContext,
            StatusCodes.Status409Conflict,
            "concurrency_conflict",
            "資料已被其他操作更新，請重新整理後再試。");
        return true;
    }
}
