using System;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Turtle.Tests
{
    [TestClass]
    public class RetryImplTests
    {
        [TestMethod]
        public void Run()
        {
            using (ShimsContext.Create())
            {
               var retry = new RetryImpl((() => Throws()));
                retry.MaximumNumberOfTries(5)
                    .Using(new ConstantWaitTimeRetryStrategy {RetryDelay = TimeSpan.FromSeconds(1)});
                retry.Run();
            }
        }

        private void Throws()
        {
            throw new Exception();
        }
    }
}
