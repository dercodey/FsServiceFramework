namespace Trending.Contracts

open System.Runtime.Serialization
open System

[<DataContract>]
[<CLIMutable>]
type RegistrationResult = { 

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    Matrix : double[]

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    Label : string }

[<DataContract>]
[<CLIMutable>]
type TrendingSeriesItem = {

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    AllResults : RegistrationResult list 

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    SelectedResult : RegistrationResult }

[<DataContract>]
[<AllowNullLiteral>]
type TrendingProtocol() =

    [<DataMember>] 
    member val Algorithm = System.String.Empty with get, set

    [<DataMember>] 
    member val Tolerance = 1.0 with get, set

[<DataContract>]
type SiteTrendingSeries() =

    let mutable _label = System.String.Empty

    [<DataMember>] 
    member val Id = 0 with get, set

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member this.Label 
        with get () = _label
        and set (value) = 
            if (value <> null)
            then _label <- value
            else raise(NullReferenceException())

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>]
    member val Protocol = TrendingProtocol() with get, set

    [<DataMember(EmitDefaultValue=false, IsRequired=true)>] 
    member val SeriesItems = List.empty<TrendingSeriesItem> with get, set

    [<DataMember>] 
    member val Shift = [|0.0; 0.0; 0.0|] with get, set
