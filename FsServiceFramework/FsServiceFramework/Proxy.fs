namespace FsServiceFramework

open System
open System.Collections.Generic
open System.ServiceModel
open System.ServiceModel.Channels
open System.ServiceModel.Dispatcher
open System.ServiceModel.Description

open FsServiceFramework.Utility

open Unity.Interception.InterceptionBehaviors
open Unity.Interception.PolicyInjection.Pipeline
open Unity

type IProxyManager =
    abstract GetDurableContext : unit -> Guid
    abstract ReleaseDurableContext : Guid -> unit
    abstract GetTransientContext : unit -> IDisposable
    abstract GetProxy<'contract> : unit -> 'contract

(* TODO: why are there two of these being created? *)
// TODO: thread to periodically remove proxies, if not in use
type ProxyManager(container:IUnityContainer) =
    let factoryCache = new Dictionary<Type,obj>()
    let channelCache = new Dictionary<Type,obj>()

    // TODO: this should use TLS instead of proxy manager class instance
    let proxiesInContext = List<obj>()
    member x.GetFactory<'contract> () =
        let createChannelFactory () = 
            let endpoint = PolicyEndpoint.createClientEndpoint typedefof<'contract> container

            let factory = new ChannelFactory<'contract>(endpoint)
            printfn "factory endpoint name = %s" factory.Endpoint.Binding.Name
            factory
        cacheCreateOrGet<'contract, ChannelFactory<'contract>> factoryCache createChannelFactory

    interface IProxyManager with
        member this.GetProxy<'contract> () : 'contract =
            (fun () -> this.GetFactory<'contract>().CreateChannel())
            |> cacheCreateOrGet<'contract, 'contract> channelCache 
            |> function proxy -> proxiesInContext.Add(proxy); proxy
        member this.GetTransientContext() = { new IDisposable with member this.Dispose() = proxiesInContext.Clear() }
        member this.GetDurableContext() = Guid.NewGuid()
        member this.ReleaseDurableContext (guid:Guid) = ()

    interface IDisposable with
        member x.Dispose() = 
            for pair in channelCache do (pair.Value :?> IChannel).Close()
            for pair in factoryCache do (pair.Value :?> ChannelFactory).Close()

module Proxy = 
    ()

          