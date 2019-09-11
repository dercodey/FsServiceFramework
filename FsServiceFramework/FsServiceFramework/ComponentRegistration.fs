namespace FsServiceFramework

module ComponentRegistration =

    open System
    open System.ServiceModel
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher

    open Unity
    open Unity.Interception.ContainerIntegration
    open Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception
    open Unity.Interception.InterceptionBehaviors
    open Unity.Interception.PolicyInjection.Pipeline
    open Unity.Injection
    
    let registerService_ (contractType:Type) (implementationType:Type) (parent:IUnityContainer) : IUnityContainer =

        let svcContainer = parent.CreateChildContainer()
        let proxyManager = svcContainer.Resolve<IProxyManager>()
        
        svcContainer.RegisterType(contractType, implementationType,    // this registers the proxy and performance Unity interceptions
            Interceptor<InterfaceInterceptor>(),             
            (Utility.unityInterceptionBehavior (fun input inner -> 
                use opId = proxyManager.GetTransientContext()
                inner input) |> InterceptionBehavior),
            (Utility.unityInterceptionBehavior (fun input inner ->
                let timeFormat (tm:DateTime) = tm.ToLongTimeString()
                let enter = DateTime.Now
                let result = inner input
                let exit = DateTime.Now
                printfn "%s->%s: Method %s %s" (timeFormat enter) (timeFormat exit)
                    input.MethodBase.Name
                    (match result.Exception with 
                        | null -> sprintf "returned %A" result 
                        | ex -> sprintf "threw exception %s" ex.Message)
                result) |> InterceptionBehavior)) |> ignore

        // create and configure the endpoint for unity instance construction
        let endpoint = 
            PolicyEndpoint.createDispatchEndpoint contractType svcContainer

        // create the host and add the configured endpoint
        let host = new ServiceHost(implementationType, endpoint.Address.Uri)
        host.AddServiceEndpoint(endpoint)
        host.Open() |> ignore
        (* TODO: how do we close these guys? *)

        svcContainer.RegisterInstance<ServiceHost>(
            sprintf "Host_for_<%s::%s>" implementationType.Namespace implementationType.Name, 
            host) |> ignore
        // return parent, to allow fluent registration to continue
        parent

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
