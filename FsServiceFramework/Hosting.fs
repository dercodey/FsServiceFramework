namespace FsServiceFramework

module Hosting =

    open System.ServiceModel

    open Unity
    open Unity.Lifetime
    open Unity.Interception.ContainerIntegration
    open Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception
    open Unity.Injection
    open System.ServiceModel.Dispatcher
    open System.ServiceModel.Description
    open System.ServiceModel.Channels
    open Unity.Interception.PolicyInjection.Pipeline
    open System

    // instance context extension type that creates a new child UnityContainer 
    //      on demand when a new instance context is created
    type UnityInstanceContextExtension(container:IUnityContainer) =
        let childContainer = lazy ( container.CreateChildContainer() )
        member this.ChildContainer = childContainer.Value
        interface IExtension<InstanceContext> with
            member this.Attach (owner:InstanceContext) = ()
            member this.Detach (owner:InstanceContext) = ()

    // name used for the ProxyManager lifetime manager
    let nameProxyManagerLifetimeManager = "ProxyManager_LifetimeManager"

    let createHostContainer () =
        let pmLifetimeManager = new ContainerControlledLifetimeManager()
        (new UnityContainer())
            .AddNewExtension<Interception>()
            .RegisterType<IProxyManager, ProxyManager>(pmLifetimeManager)
            .RegisterInstance<TraceContext>(TraceContext(Guid.NewGuid(), 1))
            .RegisterInstance<TestingContext>(TestingContext(Production))
            // this is registered so it will be disposed, to shut down the services
            .RegisterInstance<ContainerControlledLifetimeManager>(
                nameProxyManagerLifetimeManager, pmLifetimeManager)

        //////////////////////////////////////////////////
        //////////////////////////////////////////////////

        |> Tracing.registerMessageInspectors 
        |> Nz2Testing.registerMessageInspectors

        //////////////////////////////////////////////////
        //////////////////////////////////////////////////


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


    let registerService<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        let proxyManager = container.Resolve<IProxyManager>()

        // this registers the proxy and performance Unity interceptions
        container.RegisterType(contractType, implementationType, 
            Interceptor<InterfaceInterceptor>(), 

            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            
            // proxy manager release from transient
            // TODO: this should use TLS instead of proxy manager class instance
            (Utility.unityInterceptionBehavior 
                (fun input inner ->
                    use opId = proxyManager.GetTransientContext()
                    inner input)
            |> InterceptionBehavior),

            (Utility.unityInterceptionBehavior
                (fun input inner ->
                    let timeFormat (tm:DateTime) = tm.ToLongTimeString()
                    let enter = DateTime.Now
                    let result = inner input
                    let exit = DateTime.Now

                    printfn "%s->%s: Method %s %s" (timeFormat enter) (timeFormat exit)
                        input.MethodBase.Name
                        (match result.Exception with
                            | null -> sprintf "returned %A" result
                            | ex -> sprintf "threw exception %s" result.Exception.Message)
                    result)
            |> InterceptionBehavior)) 
            
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            |> ignore

        // create and configure the endpoint for unity instance construction
        let endpoint = Policy.createServiceEndpoint contractType

        // add message inspectors that were registered for their respective contexts
        let clientInspectors = container.ResolveAll<IClientMessageInspector>()
        let dispatchInspectors = (container.ResolveAll<IDispatchMessageInspector>())
        { new IEndpointBehavior with 
            member this.ApplyClientBehavior (endpoint, clientRuntime) =
                clientInspectors |> Seq.iter clientRuntime.ClientMessageInspectors.Add
                dispatchInspectors |> Seq.iter clientRuntime.CallbackDispatchRuntime.MessageInspectors.Add
            member this.ApplyDispatchBehavior (endpoint, endpointDispatcher) =
                dispatchInspectors |> Seq.iter endpointDispatcher.DispatchRuntime.MessageInspectors.Add
            member this.AddBindingParameters (endpoint, bindingParameters) = ()
            member this.Validate endpoint = () }
        |> endpoint.Behaviors.Add

        // set up instancing from the unity container
        { new IContractBehavior with 
            member this.AddBindingParameters (cd, ep, bpc) = ()
            member this.ApplyClientBehavior (cd, ep, cr) = ()
            member this.ApplyDispatchBehavior (cd, ep, dr) =
                dr.InstanceProvider <- 
                    { new IInstanceProvider with
                        member this.GetInstance (ic:InstanceContext) =
                            this.GetInstance(ic, null)
                        member this.GetInstance (ic:InstanceContext, message:Message) = 
                            let extension = ic.Extensions.Find<UnityInstanceContextExtension>()
                            extension.ChildContainer.Resolve(contractType)
                        member this.ReleaseInstance (ic:InstanceContext, instance:obj) = () }
                { new IInstanceContextInitializer with 
                    member this.Initialize (ic:InstanceContext, message:Message) = 
                        ic.Extensions.Add(UnityInstanceContextExtension(container))
                        ic.Extensions.Add(Instance.StorageProviderInstanceContextExtension(container)) }
                |> dr.InstanceContextInitializers.Add
            member this.Validate (cd:ContractDescription, ep:ServiceEndpoint) = () }
        |> endpoint.Contract.Behaviors.Add

        // create the host
        let host = new ServiceHost(implementationType, endpoint.Address.Uri)
        host.AddServiceEndpoint(endpoint)

        host.ChannelDispatchers
        |> Seq.iter (fun dp -> printfn "dispatcher listener = %s" (dp.Listener.State.ToString()))

        host.Description.Endpoints
        |> Seq.iter (fun ep -> printfn "binding name = %s" ep.Binding.Name)

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
