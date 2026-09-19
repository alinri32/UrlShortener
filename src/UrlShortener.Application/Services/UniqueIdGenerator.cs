namespace UrlShortener.Application.Services;

using UrlShortener.Application.Common.Interfaces;

public sealed class UniqueIdGenerator
{
    private readonly IIdRangeAllocator _rangeAllocator;
    private readonly SemaphoreSlim _allocationLock = new(1, 1);

    // Atomic State Trackers
    private long _currentId = 0;
    private long _maxId = 0;

    // Fast Batch Size
    private const int BatchSize = 100000;

    public UniqueIdGenerator(IIdRangeAllocator rangeAllocator)
    {
        _rangeAllocator = rangeAllocator;
    }

    // Get Next Atomic Unique Id
    public async ValueTask<long> NextIdAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            long current = Interlocked.Read(ref _currentId);
            long max = Interlocked.Read(ref _maxId);

            // Fast Path: In-Memory Atomic Increment
            if (current < max)
            {
                long nextId = Interlocked.Increment(ref _currentId);
                if (nextId <= max)
                {
                    return nextId;
                }
            }

            // Slow Path: Range Exhausted, Acquire Next Range
            await _allocationLock.WaitAsync(cancellationToken);
            try
            {
                // Double-Check Locking
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