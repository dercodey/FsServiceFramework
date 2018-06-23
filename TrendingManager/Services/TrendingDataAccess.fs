namespace TrendingManager

open System
open System.ServiceModel

open Microsoft.Practices.Unity

open MathNet.Numerics.LinearAlgebra

open Infrastructure

open TrendingManager.Contracts

[<ProvidedInterface(typedefof<ITrendingDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingDataAccess(seriesRepo:IRepository<SiteTrendingSeries>) =
    interface ITrendingDataAccess with
        member this.GetTrendingSeries (seriesId:int) =
            let protocol = { 
                Algorithm = "trend"; 
                Tolerance = 1.0 } 
            let items = []
//                [ { AllResults = []; SelectedResult = {Label=""; Matrix=(DiagonalMatrix.identity<float> 4) } };
//                  { AllResults = []; SelectedResult = {Label=""; Matrix=(DiagonalMatrix.identity<float> 4) } } ]
            { Label = seriesId.ToString();
              Protocol = protocol;
              SeriesItems = items;
              Shift = [| 1.0; 2.0; 3.0 |] }
            // seriesRepo.Get<int>(seriesId)
        member this.UpdateTrendingSeries series =
            seriesRepo.Update(series)

module TrendingManager =
    let createTestData () = ()
        
