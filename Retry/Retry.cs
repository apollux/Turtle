using System;

namespace Turtle
{
    public static class Retry
    {
        public static RetryImpl This(Action retry)
        {
            return new RetryImpl(retry);
        }

        public static RetryImpl This(Action retry, Func<bool> isDonePredicate)
        {
            return new RetryImpl(retry, isDonePredicate);
        }

        public static RetryImpl This(Func<bool> retry)
        {
            return new RetryImpl(retry);
        }
    }
}
