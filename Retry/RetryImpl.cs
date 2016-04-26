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

                if (ShouldRetry())
                {
                    Thread.Sleep(retryStrategy.NextRetryDelay(context));
                }
            }

            return completionState;
        }

        public async Task<CompletionState> RunAsync()
        {
            return await RunAsync(new CancellationToken());
        }

        public async Task<CompletionState> RunAsync(CancellationToken token)
        {
            var completionState = CompletionState.Failed;

            while (completionState == CompletionState.Failed)
            {
                token.ThrowIfCancellationRequested();
                context.Update();
                if (!ShouldRetry())
                {
                    break;
                }

                completionState = await Task.Factory.StartNew(
                    () => toRetry() ? CompletionState.Success : CompletionState.Failed, token);

                if (completionState == CompletionState.Failed && ShouldRetry())
                {
                    await Task.Delay(retryStrategy.NextRetryDelay(context), token);
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
