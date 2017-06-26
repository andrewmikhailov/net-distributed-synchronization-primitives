namespace Kichink.Discounts.Reliability
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using DistributedSynchronizationPrimitives.Mutex;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ItemDiscountSimultaneousApplyTest
    {
        private string apiServerUri = "https://192.168.56.3";
        private string redisServerUri = "192.168.56.3";

        private static readonly HttpClient client = new HttpClient();

        private System.Threading.Timer timer;

        internal class TestContext
        {
            public TestContext(RedisMutex mutex, string response)
            {
                this.Mutex = mutex;
                this.Response = response;
            }

            public RedisMutex Mutex
            {
                get;
                set;
            }

            public string Response
            {
                get;
                set;
            }
        }

        [TestInitialize]
        public void Init()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        [TestMethod]
        public void TestMethod()
        {
            string codeIdentifier = "f6cf5d7322b3493f99a7fb6bb5cf1924";
            string requestIdentifier1 = "2d92b2d3f0a44ef996130a27dc982866";
            string requestIdentifier2 = "2d92b2d3f0a44ef996130a27dc982867";

            RedisMutex mutex1 = this.BuildMutex(codeIdentifier, requestIdentifier1);
            RedisMutex mutex2 = this.BuildMutex(codeIdentifier, requestIdentifier2);

            TimeSpan alertTime = TimeSpan.FromSeconds(5);
            ExecuteDelayed(alertTime, () =>
            {
                mutex1.Release();
                mutex2.Release();
            });

            string response1 = this.ApplySingle(mutex1, codeIdentifier, requestIdentifier1);
            string response2 = this.ApplySingle(mutex2, codeIdentifier, requestIdentifier2);
            Assert.AreEqual(response1, "{\"result\":true}");
            Assert.AreEqual(response2, "{\"result\":true}");
        }

        private void ExecuteDelayed(TimeSpan alertTime, Action action)
        {
            this.timer = new System.Threading.Timer(x =>
            {
                action();
            }, null, alertTime, Timeout.InfiniteTimeSpan);
        }

        private RedisMutex BuildMutex(string codeIdentifier, string requestIdentifier)
        {
            var mutexIdentifier = "mutex:" + codeIdentifier + ":" + requestIdentifier;
            RedisMutex mutex = new RedisMutex(redisServerUri, mutexIdentifier);
            bool result = mutex.Acquire(100000);
            return mutex;
        }

        private string ApplySingle(RedisMutex mutex, string codeIdentifier, string requestIdentifier)
        {
            var values = new Dictionary<string, string>
            {
               { "item_id", "191" },
               { "itemDiscount_ids[]", "216" },
               { "coupon_code", "TEST" },
               { "flowContext[codeIdentifier]", codeIdentifier},
               { "flowContext[requestIdentifier]", requestIdentifier }
            };

            var content = new FormUrlEncodedContent(values);
            try
            {
                client.DefaultRequestHeaders.Add("Connection", "close");
                var response = client.PostAsync(apiServerUri + "/discount/apply", content).Result;
                var responseContent = response.Content;
                var responseString = responseContent.ReadAsStringAsync().Result;
                response.Dispose();
                return responseString;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}