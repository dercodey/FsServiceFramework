namespace Worklist.Services

open System
open System.ServiceModel
open Unity
open FsServiceFramework
open FsServiceFramework.Utility

open Worklist.Contracts

module WorklistManagerService =

    let instance = 
        { new IWorklistManager with
            member this.GetWorklistForStaff staffId =
                fun (engine:IWorklistEngine) -> engine.GetWorklistForStaff staffId 
                |> bindAndCall
            member this.CompleteWorklistItem itemId = 
                fun (engine:IWorklistEngine) -> engine.CompleteWorklistItem itemId 
                |> bindAndCall }

[<ProvidedInterface(typedefof<IWorklistManager>)>]
[<RequiredInterface(typedefof<IWorklistEngine>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type WorklistManagerService(container:IUnityContainer) =
    do Log.Out(Debug "Creating a WorklistManagerService.")
    interface IWorklistManager with
        member this.GetWorklistForStaff staffId =
            fun (engine:IWorklistEngine) -> engine.GetWorklistForStaff staffId
            |> bindAndCall
        member this.CompleteWorklistItem itemId =
            fun (engine:IWorklistEngine) -> engine.CompleteWorklistItem itemId 
            |> bindAndCall
