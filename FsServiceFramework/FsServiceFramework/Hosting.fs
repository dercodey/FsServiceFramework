namespace FsServiceFramework

module Hosting =

    open System
    open System.ServiceModel
    open System.ServiceModel.Dispatcher
    open System.ServiceModel.Description
    open System.ServiceModel.Channels

    open Unity
    open Unity.Lifetime
    open Unity.Interception.ContainerIntegration
    open Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception
    open Unity.Injection

    // name used for the ProxyManager lifetime manager
    let mutable pmLifetimeManager = null

    let createHostContainer () =        
        pmLifetimeManager <- new ContainerControlledLifetimeManager()
        (new UnityContainer())
            .AddNewExtension<Interception>()
            // TODO: register these by Unity configuration file
            .RegisterInstance<TraceContext>(TraceContext(Guid.NewGuid(), 1))
            .RegisterInstance<TestingContext>(TestingContext(Production))
            .RegisterType<IProxyManager, ProxyManager>(pmLifetimeManager)

    let startServices (container:IUnityContainer) =
        container.ResolveAll<ServiceHost>()
        |> Seq.iter (fun host -> host.Open())

    let stopServices (container:IUnityContainer) =        
        pmLifetimeManager.Dispose() // dispose of all proxies currently in use        
        pmLifetimeManager <- null
        container.ResolveAll<ServiceHost>()
        |> Seq.iter (fun host -> host.Close())  // and stop all hosts


