namespace Infrastructure

open System
open System.Collections.Generic
open System.ServiceModel
open System.ServiceModel.Channels

open Infrastructure.Utility

open Unity.Interception.InterceptionBehaviors
open Unity.Interception.PolicyInjection.Pipeline
open Unity

type IProxyManager =
    abstract GetDurableContext : unit -> Guid
    abstract ReleaseDurableContext : Guid -> unit
    abstract GetTransientContext : unit -> IDisposable
    abstract GetProxy<'contract> : unit -> 'contract

// TODO: thread to periodically remove proxies, if not in use
type ProxyManager(container:IUnityContainer) =
    let factoryCache = new Dictionary<Type,obj>()
    let channelCache = new Dictionary<Type,obj>()

    let mutable proxiesInContext:List<obj> = null
    member x.GetFactory<'contract> () =
        let createChannelFactory () = new ChannelFactory<'contract>(Policy.createServiceEndpoint typedefof<'contract> container)
        cacheCreateOrGet<'contract, ChannelFactory<'contract>> factoryCache createChannelFactory
    interface IProxyManager with
        member this.GetProxy<'contract> () : 'contract =
            let createChannel () = 
                this.GetFactory<'contract>().CreateChannel()
            let proxy = cacheCreateOrGet<'contract, 'contract> channelCache createChannel
            match proxiesInContext with
            | null -> proxy
            | _ -> proxiesInContext.Add(proxy)
                   proxy
        member this.GetTransientContext() =
            proxiesInContext = List<obj>() |> ignore
            { new IDisposable with 
                member this.Dispose() = 
                    proxiesInContext <- null }
        member this.GetDurableContext() =
            Guid.NewGuid()
        member this.ReleaseDurableContext (guid:Guid) = ()
    interface IDisposable with
        member x.Dispose() = 
            for pair in channelCache do 
                (pair.Value :?> IChannel).Close()
            for pair in factoryCache do 
                (pair.Value :?> ChannelFactory).Close()

type ProxyManagerInterceptionBehavior(pm:IProxyManager) =
    interface IInterceptionBehavior with
        member this.Invoke (input:IMethodInvocation, getNext:GetNextInterceptionBehaviorDelegate) =
            use opId = pm.GetTransientContext()
            getNext.Invoke().Invoke(input, getNext)
        member this.GetRequiredInterfaces() = Type.EmptyTypes |> Array.toSeq
        member this.WillExecute = true


module Proxy = 
    ()

          