using System;

namespace Turtle
{
    public static class RetryExtention
    {
        public static RetryImpl Retry(this Action toRetry)
        {
            return new RetryImpl(toRetry);
        }

        public static RetryImpl Retry(this Func<bool> toRetry)
        {
            return new RetryImpl(toRetry);
        }

        public static RetryImpl Retry(this Action toRetry, Func<bool> didSucceedPredicate)
        {
            return new RetryImpl(toRetry, didSucceedPredicate);
        }
    }
}