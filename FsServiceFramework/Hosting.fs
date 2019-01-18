namespace FsServiceFramework

module Hosting =

    open System.ServiceModel

    open Unity
    open Unity.Lifetime
    open Unity.Interception.ContainerIntegration
    open Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception
    open Unity.Injection
    open System.ServiceModel.Dispatcher

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
        let endpoint = Policy.createServiceEndpoint contractType
        MessageHeaders.addMessageInspectors endpoint 
            (container.ResolveAll<IClientMessageInspector>()) 
            (container.ResolveAll<IDispatchMessageInspector>()) |> ignore
        endpoint.Contract.Behaviors.Add(Instance.createInstanceContractBehavior container contractType)

        // create the host
        let policyAttribute = Utility.getCustomAttribute<PolicyAttribute> contractType
        let endpointAddress = policyAttribute.EndpointAddress contractType
        let host = new ServiceHost(implementationType, endpointAddress)
        host.AddServiceEndpoint(endpoint)
        container.RegisterInstance<ServiceHost>(
            sprintf "Host_for_<%s::%s>" implementationType.Namespace implementationType.Name, 
            host)

    let registerFunction<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        container.RegisterType(contractType, implementationType) 

    let registerRepository<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct> (container:IUnityContainer) : IUnityContainer =
        container.RegisterType<IRepository<'key, 'entity>>(
            InjectionFactory(Nz2Testing.createRepositoryForTestContext<'key, 'entity>)) 

    let registerRepositoryInstance<'key, 'entity 
        when 'key : comparison 
        and 'entity: not struct> (repository:IRepository<'key, 'entity>) (container:IUnityContainer) : IUnityContainer =
        container.RegisterInstance<IRepository<'key, 'entity>>(repository)

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
