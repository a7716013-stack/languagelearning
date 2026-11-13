using LanguageLearning.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Application.Exams;

/// <summary>考試類型與考制版本查詢（公開資料）。</summary>
public sealed class ExamCatalogService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ExamTypeDto>> GetExamTypesAsync(CancellationToken cancellationToken) =>
        await db.ExamTypes
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new ExamTypeDto(
                x.Code,
                x.Name,
                x.Description,
                x.IsAvailable,
                x.MinScore,
                x.MaxScore,
                x.ScoreStep))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExamVersionDto>> GetExamVersionsAsync(
        string? examTypeCode,
        CancellationToken cancellationToken)
    {
        var query = db.ExamVersions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(examTypeCode))
        {
            var code = examTypeCode.Trim().ToUpperInvariant();
            query = query.Where(x => x.ExamType!.Code == code);
        }

        return await query
            .OrderBy(x => x.ExamType!.SortOrder)
            .ThenByDescending(x => x.IsCurrent)
            .ThenBy(x => x.Code)
            .Select(x => new ExamVersionDto(
                x.Code,
                x.ExamType!.Code,
                x.Name,
                x.Description,
                x.IsCurrent,
                x.EffectiveFrom))
            .ToListAsync(cancellationToken);
    }
}
