using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hong.Test
{
    public class Program
    {
        [ThreadStatic]
        private static string Secret;

        public static void Main(string[] args)
        {
            var result = Start();
            result.Wait();
            Console.ReadKey();
        }

        private static async Task<int> Start()
        {
            Secret = "moo moo";
            Console.WriteLine("Started on thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("Secret is [{0}]", Secret);

            await Sleepy();

            Console.WriteLine("Finished on thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("Secret is [{0}]", Secret);

            return 1;
        }

        private static async Task<int> Sleepy()
        {
            Console.WriteLine("Was on thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(1000);
            Console.WriteLine("Now on thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            return 1;
        }
    }
}
