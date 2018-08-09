namespace FsServiceFramework.FsUnitTesst.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit.MsTest
open NHamcrest.Core

open Unity

open FsServiceFramework
open Trending.Contracts
open Trending.Services

[<TestClass>] 
type ``test trending manager as example service`` () =
    let container = Hosting.createHostContainer()    

    // values for testing
    let seriesId = 1    
    let diag i = if i/4 = i%4 then 1. else 0.
    let matrix = Array.init 16 diag
    let shift = Array.init 3 (fun i -> float i)

    let createAndPopulateRepository () =
        // construct a repository for test data
        let repository = 
            VolatileRepository<int, SiteTrendingSeries>(fun sts -> sts.Id) 
                :> IRepository<int, SiteTrendingSeries>

        // add first test record
        repository.Create 
            { Id=seriesId;
                Label=seriesId.ToString();
                Protocol={ Algorithm = "trend"; Tolerance = 1.0 };
                SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} };
                                { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} } ];
                Shift = shift; } |> ignore
        // add second (dummy) test record
        repository.Create 
            { Id=seriesId+1;
                Label=(seriesId+1).ToString();
                Protocol={ Algorithm = "trend"; Tolerance = 1.0 };
                SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} };
                                { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} } ];
                Shift = shift; } |> ignore
        repository
    
    [<TestInitialize>] 
    member ___.``set up hosting services.`` () = 

        let repository = createAndPopulateRepository()

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

        let proxyManager = container.Resolve<IProxyManager>()
        use proxyContext = proxyManager.GetTransientContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        series |> should not' (be null)
        series |> should equal 
                    { Id=seriesId;
                        Label=seriesId.ToString();
                        Protocol={ Algorithm = "trend"; 
                                        Tolerance = 1.0 };
                        SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} };
                                        { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} } ];
                        Shift = shift; }

    [<TestMethod>] 
    member ___.``when series is updated without change it should be the same is the original.`` () =

        let proxyManager = container.Resolve<IProxyManager>()
        use proxyContext = proxyManager.GetTransientContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()

        let series = proxy.GetSeries(seriesId)
        let updatedSeries = { series with Shift=[|2.0;3.0;4.0|] }
        let returnedSeries = proxy.UpdateSeries(updatedSeries);
        returnedSeries |> should equal updatedSeries
        returnedSeries |> should not' (equal series)

