namespace FsServiceFramework

module LambdaServiceRegistration =

    open System
    open System.ServiceModel
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher
    open System.ServiceModel.Channels

    open Unity
    open Unity.Interception.ContainerIntegration
    open Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception
    open Unity.Injection
    open System.ServiceModel.Activation

    [<ServiceContract>]
    [<IntranetPolicy>]
    type LambdaInvoker =
        [<OperationContract>] abstract member InvokeOperation : inMsg:Message -> Message

    type LambdaService() =
        do printfn "Creating LambdaService..."
        // interface LambdaInvoker with 
        member this.InvokeOperation(message:Message) : Message =
            Message.CreateMessage(MessageVersion.Default, "") |> ignore
            message

    type MyServiceManager() = class end


    //type CustomChannelDispatcher(mgr, listener) =
    //    inherit ChannelDispatcherBase()
    //    override this.Host : ServiceHostBase = null
    //    override this.Listener : IChannelListener = null

    type CustomServiceHostBase(baseAddresses) =
        inherit ServiceHostBase()
        do base.InitializeDescription(new UriSchemeKeyedCollection(baseAddresses));
        override this.InitializeRuntime() = 
            let bpc = BindingParameterCollection()
            let virtualPathExtension = this.Extensions.Find<VirtualPathExtension>()
            if (virtualPathExtension <> null)
            then bpc.Add(virtualPathExtension)
            else ()
            let basicHttpBinding = new BasicHttpBinding()
            let listener = basicHttpBinding.BuildChannelListener<IReplyChannel>(baseAddresses.[0], bpc)
            ()
            // let channelDispatcher = new CustomChannelDispatcher(MyServiceManager(), listener)
            // this.ChannelDispatchers.Add(channelDispatcher)
        override this.CreateDescription(implementedContracts) =
            implementedContracts <- null
            null
        override this.ApplyConfiguration() = ()

    let registerService<'contract> (container:IUnityContainer) : IUnityContainer =
        let endpoint =      // create and configure the endpoint for unity instance construction
            typedefof<'contract>
            |> Utility.getCustomAttribute<PolicyAttribute> 
            |> function 
                policyAttribute -> 
                    ServiceEndpoint(ContractDescription.GetContract(typedefof<'contract>), 
                        policyAttribute.Binding, 
                        typedefof<'contract> |> policyAttribute.EndpointAddress |> EndpointAddress)

        { new IEndpointBehavior with 
            member this.ApplyClientBehavior (_, _) = ()
            member this.ApplyDispatchBehavior (_, endpointDispatcher) =
                let dr = endpointDispatcher.DispatchRuntime
                dr.InstanceProvider <-                     
                    { new IInstanceProvider with // add instance provider to resolve from unity container
                        member this.GetInstance (ic) = box (LambdaService())
                        member this.GetInstance (ic, _) = box (LambdaService())
                        member this.ReleaseInstance (_, _) = () }

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

                dr.OperationSelector <- 
                    { new IDispatchOperationSelector with 
                        member this.SelectOperation(message) = "InvokeOperation" }

            member this.AddBindingParameters (_, _) = ()
            member this.Validate _ = () }
        |> endpoint.Behaviors.Add

        // create the host and add the configured endpoint
        let host = new ServiceHost(typedefof<LambdaService>, endpoint.Address.Uri)
        let newEp = 
            host.AddServiceEndpoint(endpoint.Contract.ConfigurationName, endpoint.Binding, endpoint.Address.Uri)
        // host.AddServiceEndpoint(endpoint)

        host.ChannelDispatchers
        |> Seq.iter (fun dp -> printfn "dispatcher listener = %s" (dp.Listener.State.ToString()))

        host.Description.Endpoints
        |> Seq.iter (fun ep -> printfn "binding name = %s" ep.Binding.Name)

        container.RegisterInstance<ServiceHost>(
            sprintf "Host_for_<%s::%s>" typedefof<LambdaService>.Namespace typedefof<LambdaService>.Name, 
            host)
            