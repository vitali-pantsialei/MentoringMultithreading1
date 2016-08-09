using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomCancelationToken
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomCancellationToken ct = new CustomCancellationToken();
            ThreadPool.QueueUserWorkItem(InfiniteLoop, ct);
            Console.ReadKey();
            ct.Cancel();
            Console.WriteLine("Close it!");
        }

        static void InfiniteLoop(Object input)
        {
            CustomCancellationToken token = input as CustomCancellationToken;
            while(true)
            {
                if (token.IsCancelled)
                    return;
                else
                {
                    Console.WriteLine("Try close me!");
                }
            }
        }
    }
}
