using Microsoft.VisualStudio.TestTools.UnitTesting;
using FsServiceFramework;
using Trending.Contracts;
using Trending.Services;

namespace FsServiceFramework.UnitTest
{
    [TestClass]
    public class HostingUnitTest
    {
        [TestMethod]
        public void TestTrendingManagerHosting()
        {
            var container = Hosting.createHostContainer();
            Hosting.registerService<ITrendingManager, TrendingManagerService>(container);
            Hosting.registerService<ITrendingEngine, TrendingEngineService>(container);
            Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>(container);
            Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>(container);
            Hosting.registerRepository<int, SiteTrendingSeries>(container);

            Hosting.startServices(container);

            using (var pm = new ProxyManager(container))
            {
                var ipm = pm as IProxyManager;
                using (var proxyContext = ipm.GetTransientContext())
                {
                    var proxy = ipm.GetProxy<ITrendingManager>();
                    var seriesId = 1;
                    var series = proxy.GetSeries(seriesId);
                    Assert.IsTrue(series != null);
                    var updatedSeries = proxy.UpdateSeries(series);
                    Assert.IsTrue(updatedSeries.Equals(series));
                }
            }

            using (var pm = new ProxyManager(container))
            {
                var ipm = pm as IProxyManager;
                using (var proxyContext = ipm.GetTransientContext())
                {
                    var proxy = ipm.GetProxy<ITrendingManager>();
                    var seriesId = 2;
                    var series = proxy.GetSeries(seriesId);
                    Assert.IsTrue(series != null);
                    var updatedSeries = proxy.UpdateSeries(series);
                    Assert.IsTrue(updatedSeries.Equals(series));
                }
            }

            Hosting.stopServices(container);
        }

        [TestMethod]
        public void TestTraceContext()
        {
        }

        [TestMethod]
        public void TestDataRepository()
        {
        }

        [TestMethod]
        public void TestLogging()
        {
        }

        [TestMethod]
        public void TestPerformance()
        {
        }
    }
}
