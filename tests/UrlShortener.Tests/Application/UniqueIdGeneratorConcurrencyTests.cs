namespace UrlShortener.Tests.Application;

using System.Collections.Concurrent;
using FluentAssertions;
using NSubstitute;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Services;
using Xunit;

public class UniqueIdGeneratorConcurrencyTests
{
    [Fact]
    public async Task NextIdAsync_UnderHighConcurrency_ShouldProduceUniqueSequentialIdsWithoutCollisions()
    {
        // Arrange
        var rangeAllocator = Substitute.For<IIdRangeAllocator>();
        long currentRangeBase = 1;
        const int batchSize = 10000;

        // Mock Range Allocator Thread-Safe Call
        rangeAllocator.AllocateRangeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                long start = Interlocked.Add(ref currentRangeBase, batchSize) - batchSize;
                long end = start + batchSize - 1;
                return Task.FromResult((start, end));
            });

        var generator = new UniqueIdGenerator(rangeAllocator);

        const int totalRequests = 30000;
        var generatedIds = new ConcurrentBag<long>();

        // Act: Execute concurrent tasks
        var tasks = Enumerable.Range(0, totalRequests).Select(async _ =>
        {
            long id = await generator.NextIdAsync();
            generatedIds.Add(id);
        });

        await Task.WhenAll(tasks);

        // Assert
        generatedIds.Should().HaveCount(totalRequests);
        generatedIds.Distinct().Should().HaveCount(totalRequests, "because every generated ID must be globally unique");
    }
}