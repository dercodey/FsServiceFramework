namespace Trending.Services

open System
open System.ServiceModel

open FsServiceFramework
open Trending.Contracts

[<ProvidedInterface(typedefof<ITrendingManager>)>]
[<RequiredInterface(typedefof<ITrendingEngine>)>]
[<ServiceBehavior(IncludeExceptionDetailInFaults=true)>]
type TrendingManagerService(pm:IProxyManager) =
    do Log.Out(Debug "Creating a TrendingManagerService.")
    interface ITrendingManager with
        member this.GetSeries siteId =
#if USE_STATIC_TEST_DATA
            let protocol = { 
                Algorithm = "trend"; 
                Tolerance = 1.0 } 
            let items = 
                [ { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|] } };
                  { AllResults = []; SelectedResult = {Label=""; Matrix=[|1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;1.0;0.0;0.0;0.0;|] } } ]
            { Id=siteId;
              Label = siteId.ToString();
              Protocol = protocol;
              SeriesItems = items;
              Shift = [| 1.0; 2.0; 3.0 |] }
#else
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            proxy.GetTrendingSeries(siteId)
#endif
        member this.UpdateSeries series =
            let proxy = pm.GetProxy<ITrendingDataAccess>()
            proxy.UpdateTrendingSeries series
            series
        member this.UpdateSiteOffset series =
            let proxy = pm.GetProxy<ITrendingEngine>()
            proxy.UpdateSiteOffset series
