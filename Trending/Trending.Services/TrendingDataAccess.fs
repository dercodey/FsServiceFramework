namespace Trending.Services

open System.ServiceModel
open FsServiceFramework
open Trending.Contracts

[<ProvidedInterface(typedefof<ITrendingDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingDataAccess(seriesRepo:IRepository<int, SiteTrendingSeries>) =
    interface ITrendingDataAccess with
        member this.GetTrendingSeries (seriesId:int) =
#if USE_STATIC_TEST_DATA
            let protocol = { 
                Algorithm = "trend"; 
                Tolerance = 1.0 } 
            let items = 
                [ { AllResults = []; SelectedResult = {Label=""; Matrix=(DiagonalMatrix.identity<float> 4) } };
                  { AllResults = []; SelectedResult = {Label=""; Matrix=(DiagonalMatrix.identity<float> 4) } } ]
            { Id=seriesId;
              Label = seriesId.ToString();
              Protocol = protocol;
              SeriesItems = items;
              Shift = [| 1.0; 2.0; 3.0 |] }
#else
            seriesRepo.Get seriesId
#endif
        member this.UpdateTrendingSeries series =
            seriesRepo.Update(series) |> ignore

module TrendingManager =
    let createTestData () = ()
        
