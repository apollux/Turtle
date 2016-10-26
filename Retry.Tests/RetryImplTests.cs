using System;
using System.Threading.Fakes;
using System.Threading.Tasks;
using System.Threading.Tasks.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Turtle.Tests
{
    [TestClass]
    public class RetryImplTests
    {
        [TestMethod]
        public void Run_ActionSucceeds_ExecuteOnce()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                ShimThread.SleepInt32 = i => { };
                var callCount = 0;
                var retry = new RetryImpl(() => callCount += 1);

                // Act
                var result = retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
                Assert.AreEqual(CompletionState.Success, result);
            }
        }

        [TestMethod]
        public void Run_FuncSucceeds_ExecuteOnce()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                ShimThread.SleepInt32 = i => { };
                var callCount = 0;
                var retry = new RetryImpl(() =>
                {
                    callCount += 1;
                    return true;
                });

                // Act
                var result = retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
                Assert.AreEqual(CompletionState.Success, result);
            }
        }

        [TestMethod]
        public void Run_FuncWithPredicateSucceeds_ExecuteOnce()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                ShimThread.SleepInt32 = i => { };
                var callCount = 0;
                var retry = new RetryImpl(() =>
                {
                    callCount += 1;
                }, () => true);

                // Act
                var result = retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
                Assert.AreEqual(CompletionState.Success, result);
            }
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionKeepsFailing_TriedFiveTimes()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                ShimThread.SleepInt32 = i => { };
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
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionKeepsFailing_SleptFourTimes()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var sleepCount = 0;
                ShimThread.SleepInt32 = i => { sleepCount += 1; };
                var retry = new RetryImpl((() =>
                {
                    Throws();
                }));

                // Act
                var result = retry.MaximumNumberOfTries(5)
                     .Run();

                // Assert
                Assert.AreEqual(4, sleepCount);
                Assert.AreEqual(CompletionState.Failed, result);
            }
        }

        [TestMethod]
        public void MaximumNumberOfTries_ActionSucceedsThirdTime_TriedThreeTimes()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                ShimThread.SleepInt32 = i => { };
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
        }

        [TestMethod]
        public void Using_ActionFails_ThreadSleepCalledWithCorrectValue()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var argumentPassedToSleep = 0;
                ShimThread.SleepInt32 = i => argumentPassedToSleep = i;
                var retry = new RetryImpl(() => Throws());

                // Act
                retry.Using(new ConstantWaitTimeRetryStrategy
                {
                    RetryDelay = TimeSpan.FromMilliseconds(1337)
                })
                     .MaximumNumberOfTries(2)
                     .Run();

                // Assert
                Assert.AreEqual(1337, argumentPassedToSleep);
            }
        }

        [TestMethod]
        public void Using_ActionFails_TaskDelayCalledWithCorrectValue()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var argumentPassedToTaskDelay = new TimeSpan();
                ShimTask.DelayTimeSpanCancellationToken = (t, c) =>
                {
                    argumentPassedToTaskDelay = t;
                    return Task.CompletedTask;
                };
                var retry = new RetryImpl(() => Throws());

                // Act
                retry.Using(new ConstantWaitTimeRetryStrategy
                {
                    RetryDelay = TimeSpan.FromMilliseconds(1337)
                })
                     .MaximumNumberOfTries(2)
                     .RunAsync().Wait();

                // Assert
                Assert.AreEqual(TimeSpan.FromMilliseconds(1337), argumentPassedToTaskDelay);
            }
        }

        [TestMethod]
        public void Run_ActionSucceedsThirdTime_ThreadSleepCalledTwoTimes()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var sleepInvokeCounter = 0;
                var callCount = 0;
                ShimThread.SleepInt32 = i => sleepInvokeCounter += 1;
                var retry = new RetryImpl(() =>
                {
                    callCount += 1;
                    if (callCount == 3)
                    {
                        return;
                    }
                    Throws();
                });

                // Act
                retry.Using(new ConstantWaitTimeRetryStrategy
                {
                    RetryDelay = TimeSpan.FromMilliseconds(1337)
                }).Run();

                // Assert
                Assert.AreEqual(2, sleepInvokeCounter);
            }
        }

        [TestMethod]
        public void RunAsync_ActionSucceedsThirdTime_TaskDelayCalledTwoTimes()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var taskDelayCallCount = 0;
                ShimTask.DelayTimeSpanCancellationToken = (t, c) =>
                {
                    taskDelayCallCount += 1;
                    return Task.CompletedTask;
                };

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
                retry.RunAsync().Wait();

                // Assert
                Assert.AreEqual(2, taskDelayCallCount);
            }
        }


        [TestMethod]
        public void Run_TryThrows_BehaviorOnExceptionIsCalled()
        {
            // Arrange
            var mockedExceptionBehavior = new Mock<IExceptionBehavior>();

            var retry = new RetryImpl(() =>
            {
                Throws();
            }, mockedExceptionBehavior.Object);
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
            }, mockedExceptionBehavior.Object);
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
