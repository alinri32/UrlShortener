namespace UrlShortener.Application.Common.Generators;

using UrlShortener.Application.Common.Interfaces;

public sealed class UniqueIdGenerator
{
    // Services
    private readonly IIdRangeAllocatorRepository _rangeAllocator;
    private readonly SemaphoreSlim _allocationLock = new(1, 1);

    private long _currentId = 0;
    private long _maxId = 0;
    private const int BatchSize = 100000;

    // Ctor
    public UniqueIdGenerator(IIdRangeAllocatorRepository rangeAllocator)
    {
        _rangeAllocator = rangeAllocator;
    }

    // Public Method
    public async ValueTask<long> NextIdAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            // Fast Path
            long current = Interlocked.Read(ref _currentId);
            long max = Interlocked.Read(ref _maxId);

            if (current < max)
            {
                long nextId = Interlocked.Increment(ref _currentId);
                if (nextId <= max)
                {
                    return nextId;
                }
            }

            // Slow Path
            await _allocationLock.WaitAsync(cancellationToken);
            try
            {
                if (Interlocked.Read(ref _currentId) >= Interlocked.Read(ref _maxId))
                {
                    // DB Fetch
                    var (start, end) = await _rangeAllocator.AllocateRangeAsync(BatchSize, cancellationToken);

                    Interlocked.Exchange(ref _currentId, start - 1);
                    Interlocked.Exchange(ref _maxId, end);
                }
            }
            finally
            {
                _allocationLock.Release();
            }
        }
    }
}