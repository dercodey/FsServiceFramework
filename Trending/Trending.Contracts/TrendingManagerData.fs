namespace Trending.Contracts

open System.Runtime.Serialization

[<DataContract>]
[<CLIMutable>]
type RegistrationResult = { 
    [<DataMember>] Matrix : double[]
    [<DataMember>] Label : string }

[<DataContract>]
[<CLIMutable>]
type TrendingSeriesItem = {
    [<DataMember>] AllResults : RegistrationResult list 
    [<DataMember>] SelectedResult : RegistrationResult }

[<DataContract>]
[<CLIMutable>]
type TrendingProtocol = { 
    [<DataMember>] Algorithm : string
    [<DataMember>] Tolerance : double }

[<DataContract>]
[<CLIMutable>]
type SiteTrendingSeries = { 
    [<DataMember>] Id : int
    [<DataMember>] Label : string
    [<DataMember>] Protocol : TrendingProtocol
    [<DataMember>] SeriesItems : TrendingSeriesItem list
    [<DataMember>] Shift : double[] }