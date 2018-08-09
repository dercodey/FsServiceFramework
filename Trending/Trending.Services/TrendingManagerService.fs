namespace Trending.Services

open System
open System.ServiceModel

open FsServiceFramework
open Trending.Contracts

[<ProvidedInterface(typedefof<ITrendingManager>)>]
[<RequiredInterface(typedefof<ITrendingEngine>)>]
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
