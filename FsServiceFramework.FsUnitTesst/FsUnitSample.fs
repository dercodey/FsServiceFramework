namespace FsServiceFramework.FsUnitTesst.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting

open Unity

open FsServiceFramework
open Trending.Contracts
open Trending.Services

[<TestClass>] 
type ``test trending manager as example service`` () =
    let container = Hosting.createHostContainer()    

    // values for testing
    let seriesId = 1    

    // create an identity matrix
    let diag i = if i/4 = i%4 then 1. else 0.
    let matrix = Array.init 16 diag

    // function to create a shift that correlates with the id
    let shiftForId (id:int) = Array.init 3 (fun x -> float(x+id))

    let createAndPopulateRepository () =
        // construct a repository for test data
        let repository = 
            VolatileRepository<int, SiteTrendingSeries>(fun sts -> sts.Id) 
                :> IRepository<int, SiteTrendingSeries>

        // helper to create STS record for a given index
        let createSiteTrendingSeriesForIndex i =
            SiteTrendingSeries(Id = i,
                Label = i.ToString(),
                Protocol = TrendingProtocol(Algorithm = "trend", Tolerance = 1.0 ),
                SeriesItems = [ { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} };
                                { AllResults = []; SelectedResult = {Label=""; Matrix=matrix} } ],
                Shift = (shiftForId i))

        // add some test record
        { seriesId..seriesId+2 }
        |> Seq.map createSiteTrendingSeriesForIndex            
        |> Seq.iter (fun sts -> repository.Create(sts) |> ignore)
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
        container.Dispose()

    [<TestMethod>] 
    member ___.``when series get is called should have correct value.`` () =
        let proxyManager = container.Resolve<IProxyManager>()
        use proxyContext = proxyManager.GetTransientContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        Assert.IsNotNull(series)
        Assert.AreEqual(series.Id, seriesId)
        Assert.AreEqual(series.Label, seriesId.ToString())
        Assert.AreEqual(series.Protocol.Algorithm, "trend")
        Assert.AreEqual(series.Protocol.Tolerance, 1.0)
        Assert.AreEqual(series.SeriesItems.[0].AllResults.Length, 0)
        Assert.AreEqual(series.SeriesItems.[0].SelectedResult, {Label=""; Matrix=matrix})
        Assert.AreEqual(series.SeriesItems.[1].AllResults.Length, 0)
        Assert.AreEqual(series.SeriesItems.[1].SelectedResult, {Label=""; Matrix=matrix})
        (series.Shift, 
            shiftForId seriesId)
            ||> Array.map2 (-)
            |> Array.map abs
            |> Array.sum
            |> ((>) 1e-6)
            |> Assert.IsTrue

    [<TestMethod>] 
    member ___.``when series is updated without change it should be the same is the original.`` () =

        let proxyManager = container.Resolve<IProxyManager>()
        use proxyContext = proxyManager.GetTransientContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()

        let series = proxy.GetSeries(seriesId)
        let updatedSeries = series
        updatedSeries.Shift <- series.Shift |> Array.map (fun x -> x + 1.0)
        let returnedSeries = proxy.UpdateSeries(updatedSeries);
        Assert.IsNotNull(returnedSeries)
        Assert.AreEqual(returnedSeries.Id, seriesId)
        Assert.AreEqual(returnedSeries.Label, series.Label)
        Assert.AreEqual(returnedSeries.Protocol.Algorithm, "trend")
        Assert.AreEqual(returnedSeries.Protocol.Tolerance, 1.0)
        Assert.AreEqual(returnedSeries.SeriesItems.[0].AllResults.Length, 0)
        Assert.AreEqual(returnedSeries.SeriesItems.[0].SelectedResult, {Label=""; Matrix=matrix})
        Assert.AreEqual(returnedSeries.SeriesItems.[1].AllResults.Length, 0)
        Assert.AreEqual(returnedSeries.SeriesItems.[1].SelectedResult, {Label=""; Matrix=matrix})
        (returnedSeries.Shift, 
            updatedSeries.Shift)
            ||> Array.map2 (-)
            |> Array.map abs
            |> Array.sum
            |> ((>) 1e-6)
            |> Assert.IsTrue

