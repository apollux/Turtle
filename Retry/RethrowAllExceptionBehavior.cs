using System;

namespace Turtle
{
    public class RethrowAllExceptionBehavior : IExceptionBehavior
    {
        public AfterExceptionBehavior OnException(Exception e)
        {
            return AfterExceptionBehavior.Rethrow;
        }
    }
}