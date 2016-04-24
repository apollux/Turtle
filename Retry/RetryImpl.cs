using System;
using System.Threading;
using System.Threading.Tasks;

namespace Turtle
{
    public class RetryImpl
    {
        private readonly Func<bool> toRetry;
        private readonly RetryContext context = new RetryContext();
        private int maximumNumberOfTries = 0;

        private RetryStrategyBase retryStrategy = new ConstantWaitTimeRetryStrategy
        {
            RetryDelay = TimeSpan.FromMilliseconds(100),
        };

        public RetryImpl(Action retry)
        {
            toRetry = () =>
            {
                try
                {
                    retry();
                    return true;
                }
                catch
                {
                    return false;
                }
            };
        }

        public RetryImpl(Func<bool> retry)
        {
            toRetry = retry;
        }

        public RetryImpl(Action retry, Func<bool> isDonePredicate)
        {
            toRetry = () =>
            {
                try
                {
                    retry();
                    return isDonePredicate();
                }
                catch
                {
                    return false;
                }
            };
        }

        public CompletionState Run()
        {
            var completionState = CompletionState.Failed;

            while (ShouldRetry())
            {
                context.Update();
                if (toRetry())
                {
                    completionState = CompletionState.Success;
                    break;
                }
                Thread.Sleep(retryStrategy.NextRetryDelay(context));
            }

            return completionState;
        }

        public async Task<CompletionState> RunAsync()
        {
            var completionState = CompletionState.Failed;

            while (completionState == CompletionState.Failed)
            {
                context.Update();
                if (!ShouldRetry())
                {
                    break;
                }

                completionState = await Task.Factory.StartNew(
                    () => toRetry() ? CompletionState.Success : CompletionState.Failed);

                if (completionState == CompletionState.Failed)
                {
                    await Task.Delay(retryStrategy.NextRetryDelay(context));
                }
            }

            return completionState;
        }

        public RetryImpl Using(RetryStrategyBase strategy)
        {
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            retryStrategy = strategy;

            return this;
        }

        public RetryImpl MaximumNumberOfTries(int numberOfTries)
        {
            maximumNumberOfTries = numberOfTries;

            return this;
        }

        private bool ShouldRetry()
        {
            return maximumNumberOfTries <= 0 || context.Count < maximumNumberOfTries;
        }
    }
}
