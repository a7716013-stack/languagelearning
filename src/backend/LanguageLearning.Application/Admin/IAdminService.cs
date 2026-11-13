using LanguageLearning.Application.Common.Paging;

namespace LanguageLearning.Application.Admin;

public interface IAdminService
{
    Task<AdminSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken);

    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
