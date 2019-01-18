open System

open FsServiceFramework
open Trending.Contracts
open Unity

[<EntryPoint>]
let main argv = 
    (new UnityContainer())
        .RegisterInstance<TraceContext>(Tracing.createTraceContext()) 
        .RegisterInstance<TestingContext>(
            { Nz2Testing.createTestingContext() with 
                TestContextId = VolatileTest (Guid.NewGuid()) })
    |> Tracing.registerMessageInspectors
    |> Nz2Testing.registerMessageInspectors
    |> function
        container -> 
            use proxyManager = new ProxyManager(container)
            let ipm = proxyManager :> IProxyManager

            let proxyAndCall (seriesId:int) =
                use proxyContext = ipm.GetTransientContext()

                let proxy = ipm.GetProxy<ITrendingManager>()
                let series = proxy.GetSeries(seriesId)
                let updatedSeries = proxy.UpdateSeries(series)

                let proxy = ipm.GetProxy<ITrendingDataAccess>()
                let seriesFromData = proxy.GetTrendingSeries seriesId
                printfn "proxy.GetTrendingSeries = %A" 
                    (seriesFromData.Id, seriesFromData.Label, 
                        seriesFromData.Protocol.Algorithm, seriesFromData.Protocol.Tolerance, 
                        seriesFromData.SeriesItems,
                        seriesFromData.Shift)

            { 1..3 }
            |> Seq.iter proxyAndCall

    Console.ReadLine() |> ignore
    0 // return an integer exit code
