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

    // construct a repository for test data
    let repository = 
        VolatileRepository<int, SiteTrendingSeries>(fun sts -> sts.Id) 
            :> IRepository<int, SiteTrendingSeries>

    // seriesId for testing
    let seriesId = 1    

    // add first test record
    do repository.Create 
        { Id=seriesId;
            Label=seriesId.ToString();
            Protocol={ Algorithm = "trend"; Tolerance = 1.0 };
            SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} };
                            { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} } ];
            Shift = [| 1.0; 2.0; 3.0 |]; } |> ignore

    // add second (dummy) test record
    do repository.Create 
        { Id=seriesId+1;
            Label=(seriesId+1).ToString();
            Protocol={ Algorithm = "trend"; Tolerance = 1.0 };
            SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} };
                            { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|]} } ];
            Shift = [| 1.0; 2.0; 3.0 |]; } |> ignore
    
    [<TestInitialize>] 
    member ___.``set up hosting services.`` () = 

        // register services and IoC container
        container 
        |> Hosting.registerService<ITrendingManager, TrendingManagerService>
        |> Hosting.registerService<ITrendingEngine, TrendingEngineService>
        |> Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>
        |> Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>
        |> Hosting.registerRepositoryInstance<int, SiteTrendingSeries> repository
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
                    { Id=seriesId;
                        Label=seriesId.ToString();
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
        let updatedSeries = { series with Shift=[|2.0;3.0;4.0|] }
        let returnedSeries = proxy.UpdateSeries(updatedSeries);
        returnedSeries |> should equal updatedSeries
        returnedSeries |> should not' (equal series)

