open System

open FsServiceFramework
open Trending.Contracts
open Import.Contracts

open Unity
open System.ServiceModel.Dispatcher
open Unity.Injection

[<EntryPoint>]
let main argv = 
    System.Threading.Thread.Sleep(5)

    (new UnityContainer())
        .RegisterInstance<TraceContext>(TraceContext(Guid.NewGuid(), 1)) 
        .RegisterInstance<TestingContext>(TestingContext(VolatileTest (Guid.NewGuid())))
    |> function
        container -> 
            use proxyManager = new ProxyManager(container)
            let ipm = proxyManager :> IProxyManager

            let imageEchoAndCall () =
                use proxyContext = ipm.GetTransientContext()
                let proxy = ipm.GetProxy<IImportManager>()

                let testImage = 
                    ImportImage(Label = "testImage", 
                        Width = 2, Height = 2, Pixels = 1uy) // [|0uy;1uy;1uy;0uy |])
                proxy.EchoImage testImage

            let echoImage = imageEchoAndCall ()
            printfn "%A" echoImage

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
