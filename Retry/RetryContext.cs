using System;

namespace Turtle
{
    internal class RetryContext : IRetryContext
    {
        public int Count { get; private set; }
        public DateTime LastTried { get; private set; }

        public void Update()
        {
            Count += 1;
            LastTried = DateTime.UtcNow;
        }
    }
}