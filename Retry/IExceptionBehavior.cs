using System;

namespace Turtle
{
    public interface IExceptionBehavior
    {
        AfterExceptionBehavior OnException(Exception e);
    }
}