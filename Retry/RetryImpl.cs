﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Turtle
{
    public class RetryImpl
    {
        private readonly Func<bool> toRetry;
        private readonly IExceptionBehavior behavior;
        private readonly RetryContext context = new RetryContext();
        private int maximumNumberOfTries = 0;

        private RetryStrategyBase retryStrategy = new ConstantWaitTimeRetryStrategy
        {
            RetryDelay = TimeSpan.FromMilliseconds(100),
        };

        private CompletionState completionState;

        public RetryImpl(Action retry)
            : this(retry, new RetryAllExceptionBehavior())
        { }

        public RetryImpl(Action retry, IExceptionBehavior behavior)
            : this(() =>
            {
                retry();
                return true;

            }, behavior)
        { }

        public RetryImpl(Action retry, Func<bool> isDonePredicate)
            : this(retry, isDonePredicate, new RetryAllExceptionBehavior())
        { }

        public RetryImpl(Action retry, Func<bool> isDonePredicate, IExceptionBehavior behavior)
            : this(() =>
            {
                retry();
                return isDonePredicate();

            }, behavior)
        { }

        public RetryImpl(Func<bool> retry)
            : this(retry, new RethrowAllExceptionBehavior())
        { }

        public RetryImpl(Func<bool> retry, IExceptionBehavior behavior)
        {
            toRetry = retry;
            this.behavior = behavior;
        }

        public CompletionState Run()
        {
            completionState = CompletionState.Failed;

            while (ShouldRetry())
            {
                completionState = Try();

                if (completionState != CompletionState.Failed)
                {
                    break;
                }

                if (ShouldRetry())
                {
                    Thread.Sleep(retryStrategy.NextRetryDelay(context));
                }

                context.Update();
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

                if (completionState != CompletionState.Failed)
                {
                    break;
                }

                if (ShouldRetry())
                {
                    await Task.Delay(retryStrategy.NextRetryDelay(context), token);
                }

                context.Update();
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
    }
}
