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


    // instance context extension type that creates a new child UnityContainer 
    //      on demand when a new instance context is created
    type UnityInstanceContextExtension(container:IUnityContainer) =
        let childContainer = lazy ( container.CreateChildContainer() )
        member this.ChildContainer = childContainer.Value
        interface IExtension<InstanceContext> with
            member this.Attach (owner:InstanceContext) = ()
            member this.Detach (owner:InstanceContext) = ()

    // name used for the ProxyManager lifetime manager
    let pmLifetimeManager = new ContainerControlledLifetimeManager()

    let createHostContainer () =        
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
        container.ResolveAll<ServiceHost>()
        |> Seq.iter (fun host -> host.Close())  // and stop all hosts

    let registerService<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        let proxyManager = container.Resolve<IProxyManager>()        
        container.RegisterType(contractType, implementationType,    // this registers the proxy and performance Unity interceptions
            Interceptor<InterfaceInterceptor>(),             
            (Utility.unityInterceptionBehavior 
                (fun input inner ->
                    use opId = proxyManager.GetTransientContext()   // proxy manager release from transient
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
            |> ignore

        let endpoint =      // create and configure the endpoint for unity instance construction
            typedefof<'contract>
            |> Utility.getCustomAttribute<PolicyAttribute> 
            |> function 
                policyAttribute -> 
                    (ContractDescription.GetContract(typedefof<'contract>), 
                        policyAttribute.Binding, 
                        typedefof<'contract>
                        |> policyAttribute.EndpointAddress 
                        |> EndpointAddress)
            |> ServiceEndpoint

        { new IEndpointBehavior with 
            member this.ApplyClientBehavior (endpoint, clientRuntime) = ()
            member this.ApplyDispatchBehavior (endpoint, endpointDispatcher) =
                // add dispatch message inspectors for restoring call context objects
                { new IDispatchMessageInspector with
                    member this.AfterReceiveRequest (request, channel, context) = 
                        container.ResolveAll<CallContextBase>()
                        |> Seq.map (fun prevObj -> prevObj.GetType())
                        |> Seq.map (CallContextOperations.getContextFromHeader request.Headers)
                        |> Seq.map 
                            (fun updatedObj -> 
                                container.RegisterInstance(updatedObj.GetType(), updatedObj))
                        |> ignore
                        null
                    member this.BeforeSendReply (reply, correlation) =
                        container.ResolveAll<CallContextBase>()
                        |> Seq.map (CallContextOperations.updateHeaderWithContext reply.Headers)
                        |> ignore }
                |> endpointDispatcher.DispatchRuntime.MessageInspectors.Add
            member this.AddBindingParameters (endpoint, bindingParameters) = ()
            member this.Validate endpoint = () }
        |> endpoint.Behaviors.Add

        // set up instancing from the unity container
        { new IContractBehavior with 
            member this.AddBindingParameters (cd, ep, bpc) = ()
            member this.ApplyClientBehavior (cd, ep, cr) = ()
            member this.ApplyDispatchBehavior (cd, ep, dr) =
                dr.InstanceProvider <- 
                    // add instance provider to resolve from unity container
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

        // create the host and add the configured endpoint
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
