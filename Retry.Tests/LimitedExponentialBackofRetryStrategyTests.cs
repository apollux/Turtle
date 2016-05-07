using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Turtle.Tests
{
    [TestClass]
    public class LimitedExponentialBackofRetryStrategyTests
    {
        [TestMethod]
        public void GetRetryDelay_FirstTry_ReturnsBaseDelay()
        {
            // Arrange
            var classUnderTest = new LimitedExponentialBackofRetryStrategy();
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
            var classUnderTest = new LimitedExponentialBackofRetryStrategy();
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
            var classUnderTest = new LimitedExponentialBackofRetryStrategy();
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
            var classUnderTest = new LimitedExponentialBackofRetryStrategy();
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
            var classUnderTest = new LimitedExponentialBackofRetryStrategy
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
    }
}
