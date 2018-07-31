namespace WorklistManager.Contracts

open System.Runtime.Serialization
open System.ServiceModel

open Infrastructure

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
    [<OperationContract>] abstract GetTrendingSeries : seriesId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> unit