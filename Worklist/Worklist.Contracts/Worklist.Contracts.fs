namespace Worklist.Contracts

open System.ServiceModel
open System
open System.Runtime.Serialization

open FsServiceFramework

[<DataContract>]
[<CLIMutable>]
type WorklistItem = {
    [<DataMember>] ImageAcquired : DateTime
    [<DataMember>] ApprovedOn : DateTime option }

[<DataContract>]
[<CLIMutable>]
type WorklistItems = { 
    [<DataMember>] StaffId : int
    [<DataMember>] AllStaffWorklistItems : WorklistItem list }

[<ServiceContract>]
[<IntranetPolicy>]
type IWorklistManager =
    [<OperationContract>] abstract GetWorklistForStaff: staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> bool

[<ServiceContract>]
[<ComponentPolicy>]
type IWorklistEngine = 
    [<OperationContract>] abstract GetWorklistForStaff: staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> bool

[<ServiceContract>]
[<ComponentPolicy>]
type IWorklistDataAccess = 
    [<OperationContract>] abstract GetWorklistForStaff : staffId:int -> WorklistItems
    [<OperationContract>] abstract CompleteWorklistItem: itemId:int -> bool