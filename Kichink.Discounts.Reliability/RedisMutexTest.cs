namespace Kichink.Discounts.Reliability
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using DistributedSynchronizationPrimitives.Mutex;

    [TestClass]
    public class RedisMutexTest
    {
        [TestMethod]
        public void AcquireTest()
        {
            RedisMutex mutex = new RedisMutex("192.168.56.3", ":)");
            bool result = mutex.Acquire(10000);
            Assert.IsTrue(result);
            result = mutex.Acquire(10000);
            Assert.IsFalse(result);
            mutex.Release();
            result = mutex.Acquire(10000);
            Assert.IsTrue(result);
        }
    }
}