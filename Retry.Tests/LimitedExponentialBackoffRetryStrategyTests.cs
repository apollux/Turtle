using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Turtle.Tests
{
    [TestClass]
    public class LimitedExponentialBackoffRetryStrategyTests
    {
        [TestMethod]
        public void GetRetryDelay_FirstTry_ReturnsBaseDelay()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy();
            var context = new RetryContext();

            // Act
            classUnderTest.BaseDelay = TimeSpan.FromMilliseconds(200);
            var retryDelay = classUnderTest.NextRetryDelay(context);

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), retryDelay);
        }

        [TestMethod]
        public void GetRetryDelay_SecondTry_Returns200Milliseconds()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy();
            var context = new RetryContext();
            context.Update();

            // Act
            var retryDelay = classUnderTest.NextRetryDelay(context);

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), retryDelay);
        }

        [TestMethod]
        public void GetRetryDelay_ThirdTry_Returns400Milliseconds()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy();
            var context = new RetryContext();
            context.Update();
            context.Update();

            // Act
            var retryDelay = classUnderTest.NextRetryDelay(context);

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(400), retryDelay);
        }

        [TestMethod]
        public void GetRetryDelay_FourthTry_Returns800Milliseconds()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy();
            var context = new RetryContext();
            context.Update();
            context.Update();
            context.Update();

            // Act
            var retryDelay = classUnderTest.NextRetryDelay(context);

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(800), retryDelay);
        }

        [TestMethod]
        public void GetRetryDelay_CalculatedExponentialValueExceedsMaxDelay_ReturnedValueDoesNotExceedMaxDelay()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy
            {
                MaxDelay = TimeSpan.FromMilliseconds(500)
            };
            var context = new RetryContext();
            context.Update();
            context.Update();
            context.Update();

            // Act
            var retryDelay = classUnderTest.NextRetryDelay(context);

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), retryDelay);
        }

        [TestMethod]
        public void NextRetryDelay_CounterReachesMax_ShouldReturnMaxDelay()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy {MaxDelay = TimeSpan.FromMinutes(2)};
            var mockeContext = new Mock<IRetryContext>();
            mockeContext.Setup(context => context.Count).Returns(int.MaxValue);

            // Act
            var retryDelay = classUnderTest.NextRetryDelay(mockeContext.Object);

            // Assert
            Assert.AreEqual(TimeSpan.FromMinutes(2), retryDelay);
        }

        [TestMethod]
        public void NextRetryDelay_WhenCounterOverflows_ShouldReturnMaxDelay()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackoffRetryStrategy {MaxDelay = TimeSpan.FromMinutes(2)};
            var mockedContext = new Mock<IRetryContext>();
            mockedContext.Setup(context => context.Count).Returns(int.MaxValue);

            // Act
            classUnderTest.NextRetryDelay(mockedContext.Object);
            mockedContext.Setup(context => context.Count).Returns(int.MinValue);
            var retryDelay = classUnderTest.NextRetryDelay(mockedContext.Object);

            // Assert
            Assert.AreEqual(TimeSpan.FromMinutes(2), retryDelay);
        }
    }
}
