namespace FsServiceFramework 

open System
open System.Runtime.Serialization

[<AbstractClass>]
type CallContextBase() =
    [<DataMember>] member val CallBegin = DateTime.Now with get, set

module CallContextOperations = 

    open System.ServiceModel.Dispatcher

    open Unity
    open Unity.Injection

    let updateHeaderWithContext (headers:System.ServiceModel.Channels.MessageHeaders) (current:CallContextBase) =
        let headerName = current.GetType().Name
        let headerNamespace = current.GetType().Namespace
        let tcHeader = System.ServiceModel.MessageHeader(current).GetUntypedHeader(headerName, headerNamespace)
        match headers.FindHeader(headerName, headerNamespace) with 
        | -1 -> ()    // no header currently
        | _ -> headers.RemoveAll(headerName, headerNamespace)
        headers.Add(tcHeader)
        headers

    let getContextFromHeader (headers:System.ServiceModel.Channels.MessageHeaders) (forType:Type) =
        let headerName = forType.Name
        let headerNamespace = forType.Namespace
        match headers.FindHeader(headerName, headerNamespace) with
        | -1 ->  invalidArg "getContextFromHeader" "no message header for type" 
        | index -> headers.GetHeader<CallContextBase>(index)

    let createClientMessageInspector (childContainer:IUnityContainer) =
        box { new IClientMessageInspector with
                member this.BeforeSendRequest (request, channel) = 
                    childContainer.ResolveAll<CallContextBase>()
                    |> Seq.map (updateHeaderWithContext request.Headers)
                    |> ignore
                    null
                member this.AfterReceiveReply (request, correlation) = () }

    let createDispatchMessageInspector (childContainer:IUnityContainer) =
        box { new IDispatchMessageInspector with
                member this.AfterReceiveRequest (request, channel, context) = 
                    childContainer.ResolveAll<CallContextBase>()
                    |> Seq.map (fun prevObj -> prevObj.GetType())
                    |> Seq.map (getContextFromHeader request.Headers)
                    |> Seq.map 
                        (fun updatedObj -> 
                            childContainer.RegisterInstance(updatedObj.GetType(), updatedObj))
                    |> ignore
                    null
                member this.BeforeSendReply (reply, correlation) =
                    childContainer.ResolveAll<CallContextBase>()
                    |> Seq.map (updateHeaderWithContext reply.Headers)
                    |> ignore }

    (* TODO: generic mechanism to register CallContext objects with message inspectors *)
    let registerMessageInspectors (container:IUnityContainer) =

        container
            .RegisterType<IClientMessageInspector>(InjectionFactory(createClientMessageInspector))
            .RegisterType<IDispatchMessageInspector>(InjectionFactory(createDispatchMessageInspector))

[<DataContract>]
type TraceContext(correlationId:Guid, sequenceNumber:int) =
    inherit CallContextBase()
    [<DataMember>] member val CorrelationId = correlationId with get, set
    [<DataMember>] member val SequenceNumber = sequenceNumber with get, set
    [<DataMember>] member val EventTimeStamp = DateTime.Now with get, set

module Tracing = ()