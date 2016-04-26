using System;
using System.Threading.Fakes;
using System.Threading.Tasks;
using System.Threading.Tasks.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
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
                retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
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
                retry.Run();

                // Assert
                Assert.AreEqual(1, callCount);
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
                retry.MaximumNumberOfTries(5)
                     .Run();

                // Assert
                Assert.AreEqual(5, callCount);
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
                retry.MaximumNumberOfTries(5)
                     .Run();

                // Assert
                Assert.AreEqual(3, callCount);
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
                })
                     .Run();

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

        private void Throws()
        {
            throw new Exception();
        }
    }
}
