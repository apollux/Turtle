using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Turtle.Tests
{
    internal class DoNotWaitHandler : IWaitHandler
    {
        public int SleepCount { get; private set; }

        public TimeSpan LastSleepAmount { get; private set; }

        public void WaitSync(TimeSpan period)
        {
            LastSleepAmount = period;
            SleepCount += 1;
        }

        public Task WaitAsync(TimeSpan period, CancellationToken cancelationToken)
        {
            LastSleepAmount = period;
            SleepCount += 1;
            return Task.CompletedTask;
        }
    }

    [TestClass]
    public class RetryImplTests
    {
        private DoNotWaitHandler doNotWaitHandler;

        [TestInitialize]
        public void SetUp()
        {
            doNotWaitHandler = new DoNotWaitHandler();
        }

        [TestMethod]
        public void Run_ActionSucceeds_ExecuteOnce()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl(() => callCount += 1, new RethrowAllExceptionBehavior(), doNotWaitHandler);

            // Act
            var result = retry.Run();

            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(CompletionState.Success, result);
        }

        [TestMethod]
        public void Run_FuncSucceeds_ExecuteOnce()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl(() =>
            {
                callCount += 1;
                return true;
            }, new RethrowAllExceptionBehavior(), doNotWaitHandler);

            // Act
            var result = retry.Run();

            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(CompletionState.Success, result);
        }

        [TestMethod]
        public void Run_FuncWithPredicateSucceeds_ExecuteOnce()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl(() =>
            {
                callCount += 1;
            }, () => true, new RethrowAllExceptionBehavior(), doNotWaitHandler);

            // Act
            var result = retry.Run();

            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(CompletionState.Success, result);
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionKeepsFailing_TriedFiveTimes()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl((() =>
            {
                callCount += 1;
                Throws();
            }));

            // Act
            var result = retry.MaximumNumberOfTries(5)
                    .Run();

            // Assert
            Assert.AreEqual(5, callCount);
            Assert.AreEqual(CompletionState.Failed, result);
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionKeepsFailing_SleptFourTimes()
        {
            // Arrange
            var retry = new RetryImpl(() => false, new RethrowAllExceptionBehavior(), doNotWaitHandler);

            // Act
            var result = retry.MaximumNumberOfTries(5)
                    .Run();

            // Assert
            Assert.AreEqual(4, doNotWaitHandler.SleepCount);
            Assert.AreEqual(CompletionState.Failed, result);
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionSucceedsThirdTime_TriedThreeTimes()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl((() =>
            {
                callCount += 1;
                if (callCount == 3)
                {
                    return; // success
                }

                Throws();
            }));

            // Act
            var result = retry.MaximumNumberOfTries(5)
                    .Run();

            // Assert
            Assert.AreEqual(3, callCount);
            Assert.AreEqual(CompletionState.Success, result);
        }

        [TestMethod]
        public void Using_ActionFails_ThreadSleepCalledWithCorrectValue()
        {
            // Arrange
            var retry = new RetryImpl(() => Throws(), new RetryAllExceptionBehavior(), doNotWaitHandler);

            // Act
            retry.Using(new ConstantWaitTimeRetryStrategy
            {
                RetryDelay = TimeSpan.FromMilliseconds(1337)
            })
                    .MaximumNumberOfTries(2)
                    .Run();

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(1337), doNotWaitHandler.LastSleepAmount);
        }

        [TestMethod]
        public void Using_ActionFails_TaskDelayCalledWithCorrectValue()
        {
            // Arrange
            var retry = new RetryImpl(() => Throws(), new RetryAllExceptionBehavior(), doNotWaitHandler);

            // Act
            retry.Using(new ConstantWaitTimeRetryStrategy
            {
                RetryDelay = TimeSpan.FromMilliseconds(1337)
            })
                .MaximumNumberOfTries(2)
                .RunAsync().Wait();

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(1337), doNotWaitHandler.LastSleepAmount);
        }

        [TestMethod]
        public void Run_ActionSucceedsThirdTime_ThreadSleepCalledTwoTimes()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl(() =>
            {
                callCount += 1;
                if (callCount == 3)
                {
                    return;
                }
                Throws();
            }, new RetryAllExceptionBehavior(), doNotWaitHandler);

            // Act
            retry.Using(new ConstantWaitTimeRetryStrategy
            {
                RetryDelay = TimeSpan.FromMilliseconds(1337)
            }).Run();

            // Assert
            Assert.AreEqual(2, doNotWaitHandler.SleepCount);
        }

        [TestMethod]
        public void RunAsync_ActionSucceedsThirdTime_TaskDelayCalledTwoTimes()
        {
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl(() =>
            {
                callCount += 1;
                if (callCount == 3)
                {
                    return; // success
                }

                Throws();
            }, new RetryAllExceptionBehavior(), doNotWaitHandler);

            // Act
            retry.RunAsync().Wait();

            // Assert
            Assert.AreEqual(2, doNotWaitHandler.SleepCount);
        }

        [TestMethod]
        public void Run_TryThrows_BehaviorOnExceptionIsCalled()
        {
            // Arrange
            var mockedExceptionBehavior = new Mock<IExceptionBehavior>();

            var retry = new RetryImpl(() =>
            {
                Throws();
            }, mockedExceptionBehavior.Object, doNotWaitHandler);
            retry.MaximumNumberOfTries(1);

            // Act
            retry.Run();

            // Assert
            mockedExceptionBehavior.Verify(behavior => behavior.OnException(It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public void RunAsync_TryThrows_BehaviorOnExceptionIsCalled()
        {
            // Arrange
            var mockedExceptionBehavior = new Mock<IExceptionBehavior>();

            var retry = new RetryImpl(() =>
            {
                Throws();
            }, mockedExceptionBehavior.Object, doNotWaitHandler);
            retry.MaximumNumberOfTries(1);

            // Act
            retry.RunAsync().Wait();

            // Assert
            mockedExceptionBehavior.Verify(behavior => behavior.OnException(It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Run_TryThrowsBehaviorReturnsRethrow_ExceptionIsRethrown()
        {
            // Arrange
            var retry = new RetryImpl(() =>
            {
                Throws();
            });

            retry.OnException(new RethrowAllExceptionBehavior());

            // Act
            retry.Run();

            // Assert
            // Expected exception
        }

        [TestMethod]
        public void Run_TryThrowsBehaviorReturnsAbort_ShouldStopRetrying()
        {
            // Arrange
            var tryCount = 0;
            var mockedAbortExceptionBehavior = new Mock<IExceptionBehavior>();
            mockedAbortExceptionBehavior.Setup(behavior => behavior.OnException(It.IsAny<Exception>()))
                .Returns(AfterExceptionBehavior.Abort);

            var retry = new RetryImpl((() =>
            {
                tryCount += 1;
                Throws();
            }));
            retry.OnException(mockedAbortExceptionBehavior.Object);

            // Act
            var result = retry.Run();

            // Assert
            Assert.AreEqual(CompletionState.Aborted, result);
            Assert.AreEqual(1, tryCount);
        }

        private class ReturnNegativeTimeSpanRetryStrategy : RetryStrategyBase
        {
            protected override TimeSpan GetRetryDelay(IRetryContext context)
            {
                return TimeSpan.MinValue;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidRetryDelayException))]
        public void Run_RetryStrategyProvidesNegativeTimeSpan_ThrowsException()
        {
            // Arrange
            var retry = new RetryImpl((() => Throws()));
            retry.Using(new ReturnNegativeTimeSpanRetryStrategy());

            // Act
            retry.Run();

            // Assert
            // Expected exception
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void RunAsync_RetryStrategyProvidesNegativeTimeSpan_ThrowsException()
        {
            // Arrange
            var retry = new RetryImpl((() => Throws()));
            retry.Using(new ReturnNegativeTimeSpanRetryStrategy());

            // Act
            retry.RunAsync().Wait();

            // Assert
            // Expected exception
        }

        private void Throws()
        {
            throw new Exception();
        }
    }
}
