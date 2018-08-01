namespace Worklist.Contracts

open System
open System.Runtime.Serialization

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