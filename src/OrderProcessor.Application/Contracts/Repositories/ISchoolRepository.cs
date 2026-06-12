using OrderProcessor.Domain.Enums;

namespace OrderProcessor.Application.Contracts.Repositories
{
    public interface ISchoolRepository
    {
        Task<SchoolTier?> GetTierAsync(
                 int schoolId,
                 CancellationToken ct);
    }
}