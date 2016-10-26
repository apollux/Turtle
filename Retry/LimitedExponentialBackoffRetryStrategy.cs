using System;

namespace Turtle
{
    public class LimitedExponentialBackoffRetryStrategy : RetryStrategyBase
    {
        private bool maxDelayReached;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(2);

        protected override TimeSpan GetRetryDelay(IRetryContext context)
        {
            double delay = 0;
            if (!maxDelayReached)
            {
                delay = Math.Pow(2, context.Count) * BaseDelay.TotalMilliseconds;
                if (delay > MaxDelay.TotalMilliseconds)
                    maxDelayReached = true;
            }

            return !maxDelayReached ? TimeSpan.FromMilliseconds(delay) : MaxDelay;
        }
    }
}