using System;
using System.Threading;
using System.Threading.Tasks;

namespace Turtle
{
    internal class WaitHandlerImpl : IWaitHandler
    {
        public void WaitSync(TimeSpan period)
        {
            Thread.Sleep(period);
        }

        public Task WaitAsync(TimeSpan period, CancellationToken cancelationToken)
        {
            return Task.Delay(period, cancelationToken);
        }
    }
}