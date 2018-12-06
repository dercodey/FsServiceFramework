using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using Unity;

using FsServiceFramework;
using Trending.Contracts;
using Trending.Services;

using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace FsServiceFramework.UnitTest
{
    [TestClass]
    public class HostingUnitTest
    {
        [TestMethod]
        public void TestTrendingManagerHosting()
        {
            var repository =
                    new VolatileRepository<int, SiteTrendingSeries>(
                        FuncConvert.ToFSharpFunc<SiteTrendingSeries, int>(sts => sts.Id))
                as IRepository<int, SiteTrendingSeries>;

            int seriesId = 1;
            var matrix = new double[]
                { 1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0 };

            repository.Create(
                new SiteTrendingSeries()
                {
                    Id = seriesId,
                    Label = seriesId.ToString(),
                    Protocol = new TrendingProtocol()
                    {
                        Algorithm = "trend",
                        Tolerance = 1.0,
                    },
                    SeriesItems = ListModule.OfSeq(
                        new List<TrendingSeriesItem>
                        {
                            new TrendingSeriesItem(
                                allResults: FSharpList<RegistrationResult>.Empty,
                                selectedResult: new RegistrationResult(matrix: matrix, label: ""))
                        }),
                    Shift = new double[] { 1.0, 2.0, 3.0 }
                });


            var container = Hosting.createHostContainer();
            Hosting.registerService<ITrendingManager, TrendingManagerService>(container);
            Hosting.registerService<ITrendingEngine, TrendingEngineService>(container);
            Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>(container);
            Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>(container);
            Hosting.registerRepositoryInstance(repository, container);

            Hosting.startServices(container);

            var ipm = container.Resolve<IProxyManager>();
            using (var proxyContext = ipm.GetTransientContext())
            {
                var proxy = ipm.GetProxy<ITrendingManager>();
                var series = proxy.GetSeries(seriesId);
                Assert.IsTrue(series != null);
                var updatedSeries = proxy.UpdateSeries(series);
                Assert.IsTrue(updatedSeries.Equals(series));
            }

            using (var proxyContext = ipm.GetTransientContext())
            {
                var proxy = ipm.GetProxy<ITrendingManager>();
                var series = proxy.GetSeries(seriesId);
                Assert.IsTrue(series != null);
                var updatedSeries = proxy.UpdateSeries(series);
                Assert.IsTrue(updatedSeries.Equals(series));
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
