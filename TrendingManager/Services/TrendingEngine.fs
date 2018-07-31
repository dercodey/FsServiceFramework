namespace TrendingManager

open System
open System.ServiceModel
open Infrastructure

open TrendingManager.Contracts

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
