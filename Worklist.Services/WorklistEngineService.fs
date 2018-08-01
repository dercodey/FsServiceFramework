namespace Worklist.Services

open System
open System.ServiceModel
open FsServiceFramework
open Worklist.Contracts

[<ProvidedInterface(typedefof<IWorklistEngine>)>]
[<RequiredInterface(typedefof<IWorklistDataAccess>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type WorklistEngineService(pm:IProxyManager) =
    do Log.Out(Debug "Creating a WorklistEngineService.")
    interface IWorklistEngine with
        member this.GetWorklistForStaff staffId =
            let proxy = pm.GetProxy<IWorklistDataAccess>()
            proxy.GetWorklistForStaff staffId
        member this.CompleteWorklistItem itemId =
            let proxy = pm.GetProxy<IWorklistDataAccess>()
            proxy.CompleteWorklistItem itemId