open System

open Infrastructure

open TrendingManager.Contracts
open TrendingManager

open WorklistManager.Contracts
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
    |> Hosting.registerRepository<int, SiteTrendingSeries>
    |> ignore

    Hosting.startServices container

#if TestEmbeddedProxy
    let proxyAndCall (seriesId:int) (proxyManager:IProxyManager) =
        use proxyContext = proxyManager.GetProxyContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        printfn "proxy.GetSeries(%A) = %A" 0 series
        let updatedSeries = proxy.UpdateSeries(series)
        printfn "proxy.UpdateSeries = %A" updatedSeries
    using (new ProxyManager()) (proxyAndCall 1)
    using (new ProxyManager()) (proxyAndCall 2)
#endif

    Console.ReadLine() |> ignore

    Log.Out(Debug "closing services")

    Hosting.stopServices container

    0 // return 0 for OK
