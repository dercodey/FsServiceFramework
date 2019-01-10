namespace Trending.Services

open System
open System.ServiceModel

open FsServiceFramework
open Trending.Contracts

module TrendingEngineService =
    open FsServiceFramework

    { new ITrendCalculationFunction with
        member this.Calculate (s:SiteTrendingSeries) = 0.0 }
    |> ignore
    
    { new ITrendingEngine with
        member this.CalculateTrendForSeries series =
            (fun (tc:ITrendCalculationFunction) (da:ITrendingDataAccess) -> 
                da.UpdateTrendingSeries series |> ignore
                tc.Calculate series |> ignore; series) |> Utility.bindAndCall Utility.bindAndCall
        member this.UpdateSiteOffset series =
            (fun (da:ITrendingDataAccess) ->
                da.UpdateTrendingSeries series; 0) |> Utility.bindAndCall }
    |> ignore

[<ProvidedInterface(typedefof<ITrendingEngine>)>]
[<RequiredInterface(typedefof<ITrendingDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingEngineService(pm:IProxyManager, func:ITrendCalculationFunction) =
    do Log.Out(Debug "Creating a TrendingEngineService.")
    interface ITrendingEngine with
        member this.CalculateTrendForSeries series =
            let trendResult = func.Calculate series
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            proxy.UpdateTrendingSeries series
            series
        member this.UpdateSiteOffset series =
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            0

type TrendCalculation() =
    interface ITrendCalculationFunction with
        member this.Calculate (s:SiteTrendingSeries) = 0.0
