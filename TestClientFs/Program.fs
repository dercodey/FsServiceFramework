open System

open Infrastructure

open TrendingManager.Contracts
open TrendingManager

open Microsoft.Practices.Unity

[<EntryPoint>]
let main argv = 

    let container = new UnityContainer()
    container
        .RegisterInstance<TraceContext>(Tracing.createTraceContext()) 
        .RegisterInstance<TestingContext>(
            { Nz2Testing.createTestingContext() with TestContextId = VolatileTest (Guid.NewGuid()) })
    |> ignore

    let proxyAndCall (seriesId:int) (proxyManager:IProxyManager) =
        use proxyContext = proxyManager.GetTransientContext()
        let proxy = proxyManager.GetProxy<ITrendingManager>()
        let series = proxy.GetSeries(seriesId)
        let updatedSeries = proxy.UpdateSeries(series)
        let proxy = proxyManager.GetProxy<ITrendingDataAccess>()
        let series = proxy.GetTrendingSeries seriesId
        printfn "proxy.GetSeries = %A" series

    using (new ProxyManager(container)) (proxyAndCall 10)
    using (new ProxyManager(container)) (proxyAndCall 20)
    using (new ProxyManager(container)) (proxyAndCall 30)

    Console.ReadLine() |> ignore
    0 // return an integer exit code
