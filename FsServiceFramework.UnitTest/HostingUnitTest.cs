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
            ComponentRegistration.registerService_(typeof(ITrendingManager), typeof(TrendingManagerService), container);
            ComponentRegistration.registerService_(typeof(ITrendingEngine), typeof(TrendingEngineService), container);
            ComponentRegistration.registerService_(typeof(ITrendingDataAccess), typeof(TrendingDataAccess), container);
            ComponentRegistration.registerFunction<ITrendCalculationFunction, TrendCalculation>(container);
            ComponentRegistration.registerRepositoryInstance(repository, container);

            Hosting.startServices(container);

            var ipm = container.Resolve<IProxyManager>();
            using (var proxyContext = ipm.GetTransientContext())
            {
                var proxy = ipm.GetProxy<ITrendingManager>();
                var series = proxy.GetSeries(seriesId);
                Assert.IsTrue(series != null);
                var updatedSeries = proxy.UpdateSeries(series);
                Assert.IsTrue(updatedSeries.Id == series.Id);
            }

            using (var proxyContext = ipm.GetTransientContext())
            {
                var proxy = ipm.GetProxy<ITrendingManager>();
                var series = proxy.GetSeries(seriesId);
                Assert.IsTrue(series != null);
                var updatedSeries = proxy.UpdateSeries(series);
                Assert.IsTrue(updatedSeries.Id == series.Id);
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
