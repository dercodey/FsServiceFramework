namespace Worklist.Contracts

open System.ServiceModel

open FsServiceFramework

[<ServiceContract>]
[<IntranetPolicy>]
type IWorklistManager =
    [<OperationContract>] abstract GetWorklistForStaff: staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> unit

[<ServiceContract>]
[<ComponentPolicy>]
type IWorklistEngine = 
    [<OperationContract>] abstract GetWorklistForStaff: staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> unit

[<ServiceContract>]
[<ComponentPolicy>]
type IWorklistDataAccess = 
    [<OperationContract>] abstract GetWorklistForStaff : staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> unit