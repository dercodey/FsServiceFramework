namespace Trending.Services

open System.ServiceModel
open FsServiceFramework
open Trending.Contracts

[<ProvidedInterface(typedefof<ITrendingDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingDataAccess(seriesRepo:IRepository<int, SiteTrendingSeries>) =
    interface ITrendingDataAccess with
        member this.GetTrendingSeries (seriesId:int) =
            seriesRepo.Get seriesId

        member this.UpdateTrendingSeries series =
            seriesRepo.Update(series) |> ignore

module TrendingManager =
    let createTestData () = ()
        
