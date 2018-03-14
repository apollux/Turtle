using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Turtle.Retry.Tests")]

namespace Turtle
{
    public class RetryImpl
    {
        private readonly Func<bool> toRetry;
        private IExceptionBehavior behavior;
        private readonly IWaitHandler waitHandler;
        private readonly RetryContext context = new RetryContext();
        private int maximumNumberOfTries = 0;

        private RetryStrategyBase retryStrategy = new ConstantWaitTimeRetryStrategy
        {
            RetryDelay = TimeSpan.FromMilliseconds(100),
        };

        private CompletionState completionState;

        internal RetryImpl(Action retry)
            : this(retry, new RetryAllExceptionBehavior(), new WaitHandlerImpl())
        { }

        internal RetryImpl(Action retry, Func<bool> isDonePredicate)
            : this(retry, isDonePredicate, new RetryAllExceptionBehavior())
        { }

        internal RetryImpl(Func<bool> retry)
            : this(retry, new RethrowAllExceptionBehavior(), new WaitHandlerImpl())
        { }

        internal RetryImpl(Action retry, IExceptionBehavior behavior, IWaitHandler waitHandler)
            : this(() =>
            {
                retry();
                return true;

            }, behavior, waitHandler)
        { }

        internal RetryImpl(Action retry, Func<bool> isDonePredicate, IExceptionBehavior behavior)
            : this(retry, isDonePredicate, behavior, new WaitHandlerImpl())
        { }

        internal RetryImpl(Action retry, Func<bool> isDonePredicate, IExceptionBehavior behavior, IWaitHandler waitHandler)
            : this(() =>
            {
                retry();
                return isDonePredicate();

            }, behavior, waitHandler)
        { }

        internal RetryImpl(Func<bool> retry, IExceptionBehavior behavior, IWaitHandler waitHandler)
        {
            toRetry = retry;
            this.behavior = behavior;
            this.waitHandler = waitHandler;
        }

        public CompletionState Run()
        {
            completionState = CompletionState.Failed;

            while (ShouldRetry())
            {
                completionState = Try();

                context.Update();

                if (ShouldRetry())
                {
                    waitHandler.WaitSync(NextRetryDelayOrThrowIfNotValid());
                }
            }

            return completionState;
        }

        public async Task<CompletionState> RunAsync()
        {
            return await RunAsync(CancellationToken.None);
        }

        public async Task<CompletionState> RunAsync(CancellationToken token)
        {
            completionState = CompletionState.Failed;

            while (ShouldRetry())
            {
                completionState = await Task.Factory.StartNew(Try, token);

                context.Update();

                if (ShouldRetry())
                {
                    await waitHandler.WaitAsync(NextRetryDelayOrThrowIfNotValid(), token);
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

        public RetryImpl OnException(IExceptionBehavior behavior)
        {
            this.behavior = behavior;

            return this;
        }

        private CompletionState Try()
        {
            bool result;
            try
            {
                result = toRetry();
            }
            catch (Exception e)
            {
                var b = behavior.OnException(e);
                if (b == AfterExceptionBehavior.Rethrow)
                {
                    throw;
                }

                return b == AfterExceptionBehavior.Retry ? CompletionState.Failed : CompletionState.Aborted;
            }

            return result ? CompletionState.Success : CompletionState.Failed;
        }

        private bool ShouldRetry()
        {
            return completionState == CompletionState.Failed 
                && (maximumNumberOfTries <= 0 || context.Count < maximumNumberOfTries);
        }

        private TimeSpan NextRetryDelayOrThrowIfNotValid()
        {
            var nextRetryDelay = retryStrategy.NextRetryDelay(context);

            if (nextRetryDelay < TimeSpan.Zero)
            {
                throw new InvalidRetryDelayException();
            }

            return nextRetryDelay;
        }
    }
}
