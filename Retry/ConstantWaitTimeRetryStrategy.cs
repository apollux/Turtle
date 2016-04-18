using System;

namespace Turtle
{
    public class ConstantWaitTimeRetryStrategy : RetryStrategyBase
    {
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        protected override TimeSpan GetRetryDelay(IRetryContext context)
        {
            return RetryDelay;
        }
    }
}