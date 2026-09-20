namespace UrlShortener.Application.Common.Interfaces;

public interface IIdRangeAllocatorRepository
{
    // Atomic Range Fetch
    Task<(long Start, long End)> AllocateRangeAsync(int batchSize = 100000, CancellationToken cancellationToken = default);
}