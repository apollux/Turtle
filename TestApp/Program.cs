using System;
using Turtle;

namespace TestApp
{
    class Program
    {
        private static void Main(string[] args)
        {
            Retry.This(() =>
            {
                Console.WriteLine("Hello");
                Throws();
            })
                .MaximumNumberOfTries(5)
                .Run();

            Retry.This(() =>
            {
                Console.WriteLine("Hello2");
                Throws();
            })
                .MaximumNumberOfTries(5)
                .RunAsync().Wait();

            Retry.This(() =>
            {
                Console.WriteLine("Hello3");
                return false;
            })
                .MaximumNumberOfTries(5)
                .Run();

            Retry.This(() =>
            {
                Console.WriteLine("Hello4");
                return false;
            })
                .Using(new LimitedExponentialBackofRetryStrategy
                {
                    BaseDelay = TimeSpan.FromMilliseconds(50),
                    MaxDelay = TimeSpan.FromSeconds(5)
                })
                .MaximumNumberOfTries(5)
                .Run();

            Retry.This(() =>
            {
                Console.WriteLine("just twice");
                Throws();
            })
                .MaximumNumberOfTries(2)
                .Run();

            Action a = (() => { Console.WriteLine("extension"); Throws(); });
            a.Retry().MaximumNumberOfTries(10).Run();

            Console.ReadLine();
        }

        public static void Throws()
        {
            throw new Exception();
        }
    }
}
