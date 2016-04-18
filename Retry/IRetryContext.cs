using System;

namespace Turtle
{
    public interface IRetryContext
    {
        int Count { get; }
        DateTime LastTried { get; }
    }
}