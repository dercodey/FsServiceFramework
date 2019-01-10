namespace Worklist.Services

open System
open System.ServiceModel

open FsServiceFramework
open FsServiceFramework.Utility
open Worklist.Contracts

module WorklistEngineService =

    { new IWorklistEngine with
        member this.GetWorklistForStaff staffId =
            (fun (da:IWorklistDataAccess) -> da.GetWorklistForStaff(staffId))  |> bindAndCall
        member this.CompleteWorklistItem itemId =
            (fun (da:IWorklistDataAccess) (df:IWorklistDataAccess) 
                -> da.CompleteWorklistItem(itemId))  |> bindAndCall bindAndCall }
    |> ignore

[<ProvidedInterface(typedefof<IWorklistEngine>)>]
[<RequiredInterface(typedefof<IWorklistDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type WorklistEngineService(pm:IProxyManager) =
    do Log.Out(Debug "Creating a WorklistEngineService.")
    interface IWorklistEngine with
        member this.GetWorklistForStaff staffId =
            (fun (da:IWorklistDataAccess) -> da.GetWorklistForStaff(staffId)) 
            |> bindAndCall
        member this.CompleteWorklistItem itemId =
            (fun (da:IWorklistDataAccess) (df:IWorklistDataAccess) 
                -> da.CompleteWorklistItem(itemId)) 
            |> bindAndCall bindAndCall

            
