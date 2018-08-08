namespace FsServiceFramework.FsUnitTesst.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit.MsTest
open NHamcrest.Core

open FsServiceFramework
open Trending.Contracts
open Trending.Services

[<TestClass>] 
type ``test trending manager as example service`` () =
    let container = Hosting.createHostContainer()
    let seriesId = 1    // seriesId for testing
    
    [<TestInitialize>] 
    member ___.``set up hosting services.`` () = 
        container 
        |> Hosting.registerService<ITrendingManager, TrendingManagerService>
        |> Hosting.registerService<ITrendingEngine, TrendingEngineService>
        |> Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>
        |> Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>
        |> Hosting.registerRepository<int, SiteTrendingSeries>
        |> Hosting.startServices 

    [<TestCleanup>] 
    member ___.``stop hosting services`` () =
        container
        |> Hosting.stopServices 

    [<TestMethod>] 
    member ___.``when series get is called should have correct value.`` () =
        use pm = new ProxyManager(container)
        let ipm = pm :> IProxyManager
        use proxyContext = ipm.GetTransientContext()
        let proxy = ipm.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        series |> should not' (be null)
        series |> should equal 
                    { Label=seriesId.ToString();
                        Protocol={ Algorithm = "trend"; 
                                        Tolerance = 1.0 };
                        SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} };
                                        { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} } ];
                        Shift = [| 1.0; 2.0; 3.0 |]; }

    [<TestMethod>] 
    member ___.``when series is updated without change it should be the same is the original.`` () =
        use pm = new ProxyManager(container)
        let ipm = pm :> IProxyManager
        use proxyContext = ipm.GetTransientContext()
        let proxy = ipm.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        let updatedSeries = proxy.UpdateSeries(series);
        updatedSeries |> should equal series

