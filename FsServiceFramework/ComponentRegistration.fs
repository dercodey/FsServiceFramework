namespace FsServiceFramework

module ComponentRegistration =

    open System
    open System.ServiceModel
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher

    open Unity
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

    let registerService_ (contractType:Type) (implementationType:Type) (container:IUnityContainer) : IUnityContainer =
        let proxyManager = container.Resolve<IProxyManager>()
        container.RegisterType(contractType, implementationType,    // this registers the proxy and performance Unity interceptions
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

        let endpoint =      // create and configure the endpoint for unity instance construction
            contractType
            |> Utility.getCustomAttribute<PolicyAttribute> 
            |> function 
                policyAttribute -> 
                    ServiceEndpoint(ContractDescription.GetContract(contractType), 
                        policyAttribute.Binding, 
                        contractType |> policyAttribute.EndpointAddress |> EndpointAddress)

        { new IEndpointBehavior with 
            member this.ApplyClientBehavior (_, _) = ()
            member this.ApplyDispatchBehavior (_, endpointDispatcher) =
                let dr = endpointDispatcher.DispatchRuntime
                dr.InstanceProvider <-                     
                    { new IInstanceProvider with // add instance provider to resolve from unity container
                        member this.GetInstance (ic) = this.GetInstance(ic, null)
                        member this.GetInstance (ic, _) = ic.Extensions.Find<UnityInstanceContextExtension>().ChildContainer.Resolve(contractType)
                        member this.ReleaseInstance (_, _) = () }

                { new IInstanceContextInitializer with 
                    member this.Initialize (ic, _) = 
                        ic.Extensions.Add(UnityInstanceContextExtension(container))
                        ic.Extensions.Add(Instance.StorageProviderInstanceContextExtension(container)) }
                |> dr.InstanceContextInitializers.Add

                { new IDispatchMessageInspector with    // add dispatch message inspectors for restoring call context objects
                    member this.AfterReceiveRequest (request, _, _) = 
                        container.ResolveAll<CallContextBase>()
                        |> Seq.map (fun prevObj -> prevObj.GetType())
                        |> Seq.map (CallContextOperations.getContextFromHeader request.Headers)
                        |> Seq.map (fun updatedObj -> container.RegisterInstance(updatedObj.GetType(), updatedObj))
                        |> ignore
                        null
                    member this.BeforeSendReply (reply, _) = 
                        container.ResolveAll<CallContextBase>()
                        |> Seq.map (CallContextOperations.updateHeaderWithContext reply.Headers)
                        |> ignore }
                |> dr.MessageInspectors.Add

                if false 
                then dr.OperationSelector <- 
                        { new IDispatchOperationSelector with 
                            member this.SelectOperation(message) = 
                                let operation = message.Headers.Action
                                printfn "Selected operation is %s for %A" operation message
                                operation }

            member this.AddBindingParameters (_, _) = ()
            member this.Validate _ = () }
        |> endpoint.Behaviors.Add

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

    let registerService<'contract, 'implementation> (container:IUnityContainer) : IUnityContainer =
        let contractType = typedefof<'contract>
        let implementationType = typedefof<'implementation>
        registerService_ contractType implementationType container

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
