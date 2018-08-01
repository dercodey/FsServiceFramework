namespace Worklist.Services

open System
open System.ServiceModel
open FsServiceFramework
open Worklist.Contracts

[<ProvidedInterface(typedefof<IWorklistManager>)>]
[<RequiredInterface(typedefof<IWorklistEngine>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type WorklistManagerService(pm:IProxyManager) =
    do Log.Out(Debug "Creating a WorklistManagerService.")
    interface IWorklistManager with
        member this.GetWorklistForStaff staffId =
            let items = 
                [ { ImageAcquired = DateTime.Now; ApprovedOn = None };
                    { ImageAcquired = DateTime.Now; ApprovedOn = None } ]
            { StaffId = staffId;
                AllStaffWorklistItems = items }
        member this.CompleteWorklistItem itemId =
            let proxy = pm.GetProxy<IWorklistEngine>()
            proxy.CompleteWorklistItem itemId