namespace Trending.Services

open System
open System.ServiceModel

open FsServiceFramework
open Trending.Contracts

module TrendingManagerService = 

    open Utility

    { new ITrendingManager with 
        member this.GetSeries siteId = 
            (fun (da:ITrendingDataAccess) -> da.GetTrendingSeries siteId) |> bindAndCall
        member this.UpdateSeries series = 
            (fun (da:ITrendingDataAccess) -> da.UpdateTrendingSeries series; series) |> bindAndCall
        member this.UpdateSiteOffset series = 
            (fun (eng:ITrendingEngine) -> eng.UpdateSiteOffset series) |> bindAndCall }
    |> sprintf "%A"
    |> ignore

[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingManagerService(pm:IProxyManager) =
    do Log.Out(Debug "Creating a TrendingManagerService.")
    interface ITrendingManager with
        member this.GetSeries siteId =
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            proxy.GetTrendingSeries(siteId)

        member this.UpdateSeries series =
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            proxy.UpdateTrendingSeries series
            series
        member this.UpdateSiteOffset series =
            let proxy = pm.GetProxy<ITrendingEngine>()
            proxy.UpdateSiteOffset series
