open System

open FsServiceFramework

open Trending.Contracts
open Trending.Services

open Worklist.Contracts
open Worklist.Services


[<EntryPoint>]
let main argv = 

    // id value for testing
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

    Log.Out(Debug "Creating test repository...")
    let repository = createAndPopulateRepository()

    // create standard hosting container
    // TODO: figure out how to dispose proxy manager better
    let container = Hosting.createHostContainer() 
    container
    |> ComponentRegistration.registerService<ITrendingManager, TrendingManagerService>
    |> ComponentRegistration.registerService<ITrendingEngine, TrendingEngineService>
    |> ComponentRegistration.registerService<ITrendingDataAccess, TrendingDataAccess>
    |> ComponentRegistration.registerFunction<ITrendCalculationFunction, TrendCalculation>
    |> ComponentRegistration.registerRepositoryInstance<int, SiteTrendingSeries>(repository)
    |> ComponentRegistration.registerService<IWorklistManager, WorklistManagerService>
    |> ComponentRegistration.registerService<IWorklistEngine, WorklistEngineService>
    |> Hosting.startServices
    Console.ReadLine() |> ignore

    Log.Out(Debug "closing services")
    Hosting.stopServices container

    0 // return 0 for OK
