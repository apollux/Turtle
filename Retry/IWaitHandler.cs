using System;
using System.Threading;
using System.Threading.Tasks;

namespace Turtle
{
    internal interface IWaitHandler
    {
        void WaitSync(TimeSpan period);

        Task WaitAsync(TimeSpan period, CancellationToken cancelationToken);
    }
}