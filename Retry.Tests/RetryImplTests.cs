using System;
using System.Threading.Fakes;
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
            // Arrange
            var callCount = 0;
            var retry = new RetryImpl((() => callCount += 1));

            // Act
            retry.Run();

            // Assert
            Assert.AreEqual(1, callCount);
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

        private void Throws()
        {
            throw new Exception();
        }
    }
}
