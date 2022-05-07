using System.Collections.Generic;
using System.Linq;
using static MinThd.Parallel;
using Xunit;

namespace minThd.UnitTests;

public class UnitTests
{
    [Theory]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 4)]
    [InlineData(2_000_000, 1)]
    [InlineData(200_000, 32)]
    [InlineData(20_000, 64)]
    public void ThreadingTestOneCollectionPerThread(int length, int threads)
    {
        var collections = new List<int[]>();
        
        for (int i = 0; i < threads; i++) 
            collections.Add(new int[length]);

        const int value = 1;
        ParallelForThreadsUniform(threads, threadIndex =>
        {
            var collection = collections[threadIndex];
            for (var i = 0; i < collection.Length; i++)
            {
                collection[i] = value;
            }
        });

        Assert.All(collections.SelectMany(c => c).ToList(), i => Assert.Equal(value, i));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 4)]
    [InlineData(7, 4)]
    [InlineData(8, 4)]
    [InlineData(9, 4)]
    [InlineData(2_000_000, 1)]
    [InlineData(2_000_000, null)]
    public void ThreadingTestSingleCollection(int length, int? maxThreads)
    {
        var collection = new int[length];
        const int value = 1;
        ParallelForThread((nuint)collection.Length, threadData =>
        {
            for (var i = threadData.IndexRange.Start; i < threadData.IndexRange.End; i++)
            {
                collection[i] = value;
            }
        }, (nuint?)maxThreads);
        Assert.All(collection, i => Assert.Equal(value, i));
    }
}