using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadsExperiments
{
    class Program
    {
        static AsyncLocal<string> localData = new AsyncLocal<string>();
        static ThreadLocal<string> threadLocalData = new ThreadLocal<string>();
        static EventWaitHandle waitHandle1 = new AutoResetEvent(false);
        static EventWaitHandle waitHandle2 = new AutoResetEvent(false);
        [ThreadStatic]
        static string staticThreadField = "Default";

        static void Main(string[] args)
        {
            Console.WriteLine("1 -- Memory for thread\n2 -- Threads time creation\n3 -- ThreadStatic\n4 -- AsyncLocal\n5 -- ThreadLocal\n6 -- CallContext\n");
            string c = Console.ReadLine();
            switch (c)
            {
                case "1":
                    PhysicalMemoryCheking();
                    break;
                case "2":
                    Console.WriteLine("Enter threads count: ");
                    int count = Int32.Parse(Console.ReadLine());
                    ThreadsTimeCreation(count);
                    break;
                case "3":
                    TestThreadStaticAttribute();
                    break;
                case "4":
                    TestAsyncLocal();
                    break;
                case "5":
                    TestThreadLocal();
                    break;
                case "6":
                    TestCallContext();
                    break;
            }
        }

        public static void PhysicalMemoryCheking()
        {
            PerformanceCounter pc = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
            float duringThread = 0, afterThread;
            Console.WriteLine("Start: " + pc.NextValue() / 1024);
            Thread exThread = new Thread(() =>
            {
                duringThread = pc.NextValue() / 1024;
                Console.WriteLine("Thread: " + duringThread);
            });
            exThread.Name = "MyThread";
            exThread.Start();
            exThread.Join();
            afterThread = pc.NextValue() / 1024;
            Console.WriteLine("End: " + afterThread);
            //Sometimes we can catch 4MB diffrence
            Console.WriteLine("Answer: " + (afterThread - duringThread) + "MB");
        }

        public static void ThreadsTimeCreation(int num)
        {
            Stopwatch w = new Stopwatch();
            w.Start();
            for (int i = 0; i != num; i++)
            {
                Thread thread = new Thread((k) => { /*Console.WriteLine("Thread number " + k);*/ });
                thread.Start(i);
            }
            w.Stop();
            TimeSpan ts = w.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

        //Every async method has it's own localData, if it wasn't initilized it's set to parent's localData value.
        public static void TestAsyncLocal()
        {
            localData.Value = "Default";
            Console.WriteLine("From main thread: " + localData.Value);
            ThreadPool.QueueUserWorkItem(AsyncLocalMethod);
            //Thread th = new Thread(AsyncLocalMethod);
            //th.Start();
            waitHandle1.WaitOne();
            Console.WriteLine("(From main thread) After field was changed by another thread: " + localData.Value);
            localData.Value = "I change it!";
            Console.WriteLine("(From main thread) Changed value: " + localData.Value);
            waitHandle2.Set();
            //th.Join();
        }

        //Every thread has it's own threadLocalData and set to default if it's not initialized.
        public static void TestThreadLocal()
        {
            threadLocalData.Value = "Default";
            Console.WriteLine("From main thread: " + threadLocalData.Value);
            ThreadPool.QueueUserWorkItem(ThreadLocalMethod);
            //Thread th = new Thread(ThreadLocalMethod);
            //th.Start();
            waitHandle1.WaitOne();
            Console.WriteLine("(From main thread) After field was changed by another thread: " + threadLocalData.Value);
            threadLocalData.Value = "I change it!";
            Console.WriteLine("(From main thread) Changed value: " + threadLocalData.Value);
            waitHandle2.Set();
            //th.Join();
        }

        //Each thread has it's own staticThreadField, if it wasn't initilized it's set to default value.
        public static void TestThreadStaticAttribute()
        {
            Console.WriteLine("From main thread: " + staticThreadField);
            ThreadPool.QueueUserWorkItem(StaticAttributeMethod);
            //Thread th = new Thread(StaticAttributeMethod);
            //th.Start();
            waitHandle1.WaitOne();
            Console.WriteLine("(From main thread) After field was changed by another thread: " + staticThreadField);
            staticThreadField = "I change it!";
            Console.WriteLine("(From main thread) Changed value: " + staticThreadField);
            waitHandle2.Set();
            //th.Join();
        }

        //Each thread has it's own CallContext
        public static void TestCallContext()
        {
            string key = "myContext";
            CallContext.SetData(key, "Default");
            Console.WriteLine("From main thread: " + CallContext.GetData(key));
            ThreadPool.QueueUserWorkItem(CallContextMethod);
            //Thread th = new Thread(CallContextMethod);
            //th.Start();
            waitHandle1.WaitOne();
            Console.WriteLine("(From main thread) After field was changed by another thread: " + CallContext.GetData(key));
            CallContext.SetData(key, "I change it!");
            Console.WriteLine("(From main thread) Changed value: " + CallContext.GetData(key));
            waitHandle2.Set();
            //th.Join();
        }

        private static void AsyncLocalMethod(Object obj)
        {
            localData.Value = "First thread";
            DiagnosticsWaiting();
            Console.WriteLine("From the created thread: " + localData.Value);
        }

        private static void ThreadLocalMethod(Object obj)
        {
            threadLocalData.Value = "First thread";
            DiagnosticsWaiting();
            Console.WriteLine("From the created thread: " + threadLocalData.Value);
        }

        private static void StaticAttributeMethod(Object obj)
        {
            staticThreadField = "First thread";
            DiagnosticsWaiting();
            Console.WriteLine("From the created thread: " + staticThreadField);
        }

        private static void CallContextMethod(Object obj)
        {
            string key = "myContext";
            CallContext.SetData(key, "First thread");
            DiagnosticsWaiting();
            Console.WriteLine("From the created thread: " + CallContext.GetData(key));
        }

        private static void DiagnosticsWaiting()
        {
            waitHandle1.Set();
            waitHandle2.WaitOne();
        }
    }
}
