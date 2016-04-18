using System;

namespace Turtle
{
    public abstract class RetryStrategyBase
    {
        internal TimeSpan NextRetryDelay(IRetryContext context)
        {
            return GetRetryDelay(context);
        }

        protected abstract TimeSpan GetRetryDelay(IRetryContext context);
    }
}