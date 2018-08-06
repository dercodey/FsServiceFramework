namespace FsServiceFramework.FsUnitTesst.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit.MsTest
open NHamcrest.Core

open FsServiceFramework
open Trending.Contracts
open Trending.Services

type LightBulb(state) =
   member x.On = state
   override x.ToString() =
       match x.On with
       | true  -> "On"
       | false -> "Off"

[<TestClass>] 
type ``Given a LightBulb that has had its state set to true`` ()=
   let lightBulb = new LightBulb(true)
   let container = Hosting.createHostContainer()
   [<ClassInitialize>] member test.
     ``set up hosting services.`` () = 
        container 
        |> Hosting.registerService<ITrendingManager, TrendingManagerService>
        |> Hosting.registerService<ITrendingEngine, TrendingEngineService>
        |> Hosting.registerService<ITrendingDataAccess, TrendingDataAccess>
        |> Hosting.registerFunction<ITrendCalculationFunction, TrendCalculation>
        |> Hosting.registerRepository<int, SiteTrendingSeries>
        |> Hosting.startServices 

   [<ClassCleanup>] member test.
     ``stop hosting services`` () =
        container
        |> Hosting.stopServices 

   [<TestMethod>] member test.
    ``when I ask whether it is On it answers true.`` ()=
           lightBulb.On |> should be True

   [<TestMethod>] member test.
    ``when I convert it to a string it becomes "On".`` ()=
           string lightBulb |> should equal "On"

