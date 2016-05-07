using System;

namespace Turtle
{
    public class LimitedExponentialBackofRetryStrategy : RetryStrategyBase
    {
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(2);

        protected override TimeSpan GetRetryDelay(IRetryContext context)
        {
            return TimeSpan.FromMilliseconds(
                Math.Min((int) Math.Pow(2, context.Count) * BaseDelay.TotalMilliseconds,
                MaxDelay.TotalMilliseconds));
        }
    }
}