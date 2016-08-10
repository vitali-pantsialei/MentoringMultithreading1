using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    static Lazy<LargeObject> lazyLargeObject = null;

    // Factory function for lazy initialization.
    static int instanceCount = 0;
    static LargeObject InitLargeObject()
    {
        if (1 == Interlocked.Increment(ref instanceCount))
        {
            throw new ApplicationException(
                String.Format("Lazy initialization function failed on thread {0}.",
                Thread.CurrentThread.ManagedThreadId));
        }
        return new LargeObject(Thread.CurrentThread.ManagedThreadId);
    }

    static void Main()
    {
        // The lazy initializer is created here. LargeObject is not created until the 
        // ThreadProc method executes.
        lazyLargeObject = new Lazy<LargeObject>(InitLargeObject,
                                     LazyThreadSafetyMode.PublicationOnly);


        // Create and start 3 threads, passing the same blocking event to all of them.
        ManualResetEvent startingGate = new ManualResetEvent(false);
        Thread[] threads = { new Thread(ThreadProc), new Thread(ThreadProc), new Thread(ThreadProc) };
        foreach (Thread t in threads)
        {
            t.Start(startingGate);
        }

        PerformanceCounter ramCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
        PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        double ram = ramCounter.NextValue();
        double cpu = cpuCounter.NextValue();
        Console.WriteLine("(During the running threads) RAM: " + (ram / 1024 / 1024) + " MB; CPU: " + (cpu) + " %");

        // Give all 3 threads time to start and wait, then release them all at once.
        Thread.Sleep(50);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        startingGate.Set();

        // Wait for all 3 threads to finish. (The order doesn't matter.)
        foreach (Thread t in threads)
        {
            t.Join();
        }

        sw.Stop();
        ram = ramCounter.NextValue();
        cpu = cpuCounter.NextValue();
        Console.WriteLine("(After threads were closed) RAM: " + (ram / 1024 / 1024) + " MB; CPU: " + (cpu) + " %");
        TimeSpan ts = sw.Elapsed;

        // Format and display the TimeSpan value.
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        Console.WriteLine("RunTime " + elapsedTime);

        Console.WriteLine(
            "\r\nThreads are complete. Running GC.Collect() to reclaim extra instances.");

        GC.Collect();

        // Allow time for garbage collection, which happens asynchronously.
        Thread.Sleep(100);

        Console.WriteLine("\r\nNote that only one instance of LargeObject was used.");
        Console.WriteLine("Press Enter to end the program");
        Console.ReadLine();
    }


    static void ThreadProc(object state)
    {
        // Wait for the signal.
        ManualResetEvent waitForStart = (ManualResetEvent)state;
        waitForStart.WaitOne();

        LargeObject large = null;
        try
        {
            large = lazyLargeObject.Value;

            // The following line introduces an artificial delay, to exaggerate the race 
            // condition.
            Thread.Sleep(5);

            // IMPORTANT: Lazy initialization is thread-safe, but it doesn't protect the  
            //            object after creation. You must lock the object before accessing it,
            //            unless the type is thread safe. (LargeObject is not thread safe.)
            lock (large)
            {
                large.Data[0] = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine("LargeObject was initialized by thread {0}; last used by thread {1}.",
                    large.InitializedBy, large.Data[0]);
            }
        }
        catch (ApplicationException ex)
        {
            Console.WriteLine("ApplicationException: {0}", ex.Message);
        }
    }
}

class LargeObject
{
    int initBy = -1;
    public int InitializedBy { get { return initBy; } }

    public LargeObject(int initializedBy)
    {
        initBy = initializedBy;
        Console.WriteLine("Constructor: Instance initializing on thread {0}", initBy);
    }

    ~LargeObject()
    {
        Console.WriteLine("Finalizer: Instance was initialized on {0}", initBy);
    }

    public long[] Data = new long[50000000];
}