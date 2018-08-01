namespace Trending.Contracts

open System.Runtime.Serialization
open System.ServiceModel

open FsServiceFramework

[<ServiceContract>]
[<IntranetPolicy>]
type ITrendingManager =
    [<OperationContract>] abstract GetSeries: siteId:int -> SiteTrendingSeries
    [<OperationContract>] abstract UpdateSeries : series:SiteTrendingSeries -> SiteTrendingSeries
    [<OperationContract>] abstract UpdateSiteOffset : series:SiteTrendingSeries -> int

[<ServiceContract>]
[<ComponentPolicy>]
type ITrendingEngine = 
    [<OperationContract>] abstract CalculateTrendForSeries : series:SiteTrendingSeries -> SiteTrendingSeries
    [<OperationContract>] abstract UpdateSiteOffset : series:SiteTrendingSeries -> int

type ITrendCalculationFunction =
    abstract Calculate : SiteTrendingSeries -> double

[<ServiceContract>]
[<ComponentPolicy>]
type ITrendingDataAccess = 
    [<OperationContract>] abstract GetTrendingSeries : seriesId:int -> SiteTrendingSeries
    [<OperationContract>] abstract UpdateTrendingSeries : series:SiteTrendingSeries -> unit
