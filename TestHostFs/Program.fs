open System

open FsServiceFramework

open Trending.Contracts
open Trending.Services

open Worklist.Contracts
open Worklist.Services

[<EntryPoint>]
let main argv = 

    Log.Out(Debug "this is a debug message")

    // create standard hosting container
    // TODO: figure out how to dispose proxy manager better
    let container = Hosting.createHostContainer() 
    container
    |> Hosting.registerService<ITrendingManager, TrendingManagerService>
    |> Hosting.registerService<ITrendingEngine, TrendingEngineService>
    |> Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>
    |> Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>
    |> Hosting.registerService<IWorklistManager, WorklistManagerService>
    |> Hosting.registerService<IWorklistEngine, WorklistEngineService>
    |> Hosting.registerRepository<int, SiteTrendingSeries>
    |> ignore

    Hosting.startServices container
    Console.ReadLine() |> ignore
    Log.Out(Debug "closing services")
    Hosting.stopServices container

    0 // return 0 for OK
