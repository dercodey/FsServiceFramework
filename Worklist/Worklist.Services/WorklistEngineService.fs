namespace Worklist.Services

open System
open System.ServiceModel

open FSharp.Quotations

open FsServiceFramework
open FsServiceFramework.Utility
open Worklist.Contracts

module WorklistEngineService =

    let bind1<'svc, 'svcCallee, 'req, 'resp> 
        (svcDef:Expr<'svc->'req->'resp>) 
        (svcImpl:'svcCallee->'req->'resp) = ()

    
    bind1 <@ fun (eng:IWorklistEngine) -> eng.GetWorklistForStaff @>
        (fun (da:IWorklistDataAccess) staff -> 
            da.GetWorklistForStaff staff)
       
    bind1 <@ fun (eng:IWorklistEngine) -> eng.CompleteWorklistItem @>       
        (fun (da:IWorklistDataAccess) itemId -> 
            da.CompleteWorklistItem itemId)

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

            
