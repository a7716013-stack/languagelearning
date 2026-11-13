using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Time;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Identity;

internal sealed class UserTimeZoneResolver(AppDbContext db) : IUserTimeZoneResolver
{
    public async Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var timeZoneId = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        return TimeZoneHelper.IsValid(timeZoneId) ? timeZoneId! : TimeZoneHelper.DefaultTimeZoneId;
    }
}
