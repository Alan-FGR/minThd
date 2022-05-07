namespace MinThd;

public struct IndexRange
{
    public readonly nuint Start;
    public readonly nuint End;

    public IndexRange(nuint start, nuint end)
    {
        Start = start;
        End = end;
    }
}

public struct ThreadData
{
    public readonly nuint ThreadIndex;
    public readonly IndexRange IndexRange;

    public ThreadData(nuint threadIndex, IndexRange indexRange)
    {
        ThreadIndex = threadIndex;
        IndexRange = indexRange;
    }
}

public static class Parallel
{
    public static unsafe void ParallelForThread(nuint length, nuint maxThreads, Action<ThreadData> threadBody) =>
        ParallelForThread(length, threadBody, maxThreads);
    public static unsafe void ParallelForThread(nuint length, Action<ThreadData> threadBody, nuint? maxThreads = null)
    {
        if (length == 0) return;

        // We trust the user to know what they're doing. The code below will distribute
        // indices uniformly to the number of threads determined here, less threads will only
        // be used when the length is smaller than the desired number of threads (which is 
        // the processor count by default).
        nuint numProcessors = maxThreads ?? (nuint)Environment.ProcessorCount;

        // TODO warnings when MT might actually degrade performance

        nuint numJobs = Math.Min(numProcessors, length);

        nuint itemsPerJob = (nuint)Math.Floor(length / (double)numJobs);
        nuint remainingItems = length - itemsPerJob * numJobs;

#if DEBUG
        nuint[] ranges = new
#else
        nuint* ranges = stackalloc
#endif
        nuint[(int)numJobs + 2];

        for (nuint d = 0; d < numJobs; d++)
        {
            var hasRemaining = d < remainingItems;
            var items = itemsPerJob + (nuint)(hasRemaining ? 1 : 0);
            ranges[d + 1] = ranges[d] + items;
        }

        ranges[numJobs + 1] = length;

        //var threads = new Thread[(int)numJobs]; TODO benchmark on other platforms vs tasks - especially for low latency
        var tasks = new Task[(int)numJobs];

        for (nuint jobIndex = 0; jobIndex < numJobs; jobIndex++)
        {
            //var thread = new Thread(InternalThreadLoop);
            //threads[jobIndex] = thread;
            var startIndex = ranges[jobIndex];
            var endIndex = ranges[jobIndex + 1];
            var localJobIndex = jobIndex;
            tasks[jobIndex] = Task.Factory.StartNew(() =>
            {
                //                                   \/ a warning here can be ignored as this is copied into stack
                //var threadLocalData = new ThreadData(jobIndex, new IndexRange(startIndex, endIndex));
                //threadBody(ref threadLocalData);
                threadBody(new ThreadData(localJobIndex, new IndexRange(startIndex, endIndex)));
            });
            //thread.Start(new InternalThreadData(
            //                new ThreadData(jobIndex, new IndexRange(startIndex, endIndex)),
            //                threadBody
            //            ));
        }

        //foreach (var thread in threads) thread.Join();

        Task.WaitAll(tasks);
    }

    public static unsafe void ParallelForThreadsUniform(int threads, Action<int> threadBody)
    {
        var tasks = new Task[threads];

        for (var i = 0; i < threads; i++)
        {
            var threadLocalData = i;
            tasks[i] = Task.Factory.StartNew(() =>
            {
                threadBody(threadLocalData);
            });
        }

        Task.WaitAll(tasks);
    }

    //public delegate void ThreadBody(ref ThreadData threadData);
}

//internal static class ThreadExts
//{
//    internal static bool IsReady(this Thread thread) => 
//        thread.ThreadState is ThreadState.Unstarted or ThreadState.Stopped;
//}

//static void InternalThreadLoop(object? untypedInternalThreadData)
//{
//    var internalThreadData = (InternalThreadData)untypedInternalThreadData!;
//    //Console.WriteLine($"Thread {threadData.threadIndex}, start = {threadData.startIndex}, end = {threadData.endIndex}, range = {threadData.endIndex - threadData.startIndex}");
//    internalThreadData.ThreadBody(ref internalThreadData.ThreadData);
//    //Console.WriteLine("EXIT THREAD " + threadData.threadIndex);
//}

//public class MinThd
//{
//    List<Thread> _threadPool;

//    public MinThd(int poolSize = -1)
//    {
//        if (poolSize < 1)
//            poolSize = Environment.ProcessorCount * 2;
//        for (int i = 0; i < poolSize; i++)
//        {

//        }
//    }
//}