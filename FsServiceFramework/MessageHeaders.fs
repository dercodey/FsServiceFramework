namespace FsServiceFramework 

module MessageHeaders =

    open System.ServiceModel
    open System.ServiceModel.Channels
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher

    open Unity

    let updateHeaderWithContext<'t> (headers:MessageHeaders) (current:'t) =
        let headerName = typedefof<'t>.Name
        let headerNamespace = typedefof<'t>.Namespace
        let tcHeader = MessageHeader<'t>(current).GetUntypedHeader(headerName, headerNamespace)
        match headers.FindHeader(headerName, headerNamespace) with 
        | -1 -> ()    // no header currently
        | _ -> headers.RemoveAll(headerName, headerNamespace)
        headers.Add(tcHeader)
        headers

    let getContextFromHeader<'t> (headers:MessageHeaders) : 't =
        let headerName = typedefof<'t>.Name
        let headerNamespace = typedefof<'t>.Namespace
        match headers.FindHeader(headerName, headerNamespace) with
        | -1 ->  invalidArg "getContextFromHeader" "no message header for type" 
        | index -> headers.GetHeader<'t>(index)

    let addMessageInspectors (endpoint:ServiceEndpoint) (container:IUnityContainer) =
        let messageContextManagerBehavior = 
            { new IEndpointBehavior with 
                member this.ApplyClientBehavior (endpoint, clientRuntime) =
                    container.ResolveAll<IClientMessageInspector>()
                    |> Seq.iter (fun clientMessageInspector -> 
                                    clientRuntime.ClientMessageInspectors.Add(clientMessageInspector))
                    container.ResolveAll<IDispatchMessageInspector>()
                    |> Seq.iter (fun dispatchMessageInspector -> 
                                    clientRuntime.CallbackDispatchRuntime.MessageInspectors.Add(dispatchMessageInspector))
                member this.ApplyDispatchBehavior (endpoint, endpointDispatcher) =
                    container.ResolveAll<IDispatchMessageInspector>()
                    |> Seq.iter (fun dispatchMessageInspector -> 
                                    endpointDispatcher.DispatchRuntime.MessageInspectors.Add(dispatchMessageInspector))
                member this.AddBindingParameters (endpoint, bindingParameters) = ()
                member this.Validate endpoint = () }
        endpoint.Behaviors.Add(messageContextManagerBehavior)
        endpoint