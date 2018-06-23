namespace Infrastructure

open System
open System.Collections.Generic

open System.ServiceModel
open System.ServiceModel.Channels
open System.ServiceModel.Description
open System.ServiceModel.Dispatcher

open Microsoft.Practices.Unity
open Microsoft.Practices.Unity.InterceptionExtension

module Hosting = 

    // name used for the ProxyManager lifetime manager
    let nameProxyManagerLifetimeManager = "ProxyManager_LifetimeManager"

    let createHostContainer () =
        let pmLifetimeManager = new ContainerControlledLifetimeManager()
        let container = new UnityContainer() :> IUnityContainer   
        container.AddNewExtension<Interception>()
                 .RegisterType<IProxyManager, ProxyManager>(pmLifetimeManager)
                 .RegisterInstance<TraceContext>(Tracing.createTraceContext())
                 .RegisterInstance<TestingContext>(Nz2Testing.createTestingContext())
                 .RegisterInstance<ContainerControlledLifetimeManager>(
                    nameProxyManagerLifetimeManager, pmLifetimeManager)
            |> ignore
        container

    let registerService<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        container.RegisterType(contractType, implementationType, 
            Interceptor<InterfaceInterceptor>(), 
            InterceptionBehavior<ProxyManagerInterceptionBehavior>(), 
            InterceptionBehavior<PerformanceMonitorInterceptionBehavior>()) |> ignore        

        // create and configure the endpoint for unity instance construction
        let endpoint = Policy.createServiceEndpoint contractType container
        endpoint.Contract.Behaviors.Add(Instance.createInstanceContractBehavior container contractType)

        // create the host
        let host = new ServiceHost(implementationType, Policy.getEndpointAddress contractType)
        host.AddServiceEndpoint(endpoint)
        container.RegisterInstance<ServiceHost>(
            sprintf "Host_for_<%s::%s>" implementationType.Namespace implementationType.Name, 
            host)

    let registerFunction<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        container.RegisterType(contractType, implementationType) 

    let registerRepository<'entityType> (container:IUnityContainer) : IUnityContainer =
        container.RegisterType<IRepository<'entityType>>(
            InjectionFactory(Nz2Testing.createRepositoryForTestContext<'entityType>)) 

    let startServices (container:IUnityContainer) =
        container.ResolveAll<ServiceHost>()
        |> Seq.iter (fun host -> host.Open())

    let stopServices (container:IUnityContainer) =

        // dispose of all proxies currently in use
        let pmLifetimeManager = 
            container.Resolve<ContainerControlledLifetimeManager>(
                nameProxyManagerLifetimeManager)
        pmLifetimeManager.Dispose()

        // and stop all hosts
        container.ResolveAll<ServiceHost>()
        |> Seq.iter (fun host -> host.Close())

    // TODO: scan unity container registration, and create hosts for each
    //      registered type
    let configureHostContainer (container:IUnityContainer) = ()
