using System;

namespace Turtle
{
    public class RetryAllExceptionBehavior : IExceptionBehavior
    {
        public AfterExceptionBehavior OnException(Exception e)
        {
            return AfterExceptionBehavior.Retry;
        }
    }
}