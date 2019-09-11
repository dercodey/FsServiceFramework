namespace FsServiceFramework 

open System
open System.Runtime.Serialization
open System.ServiceModel.Channels

[<AbstractClass>]
type CallContextBase() =
    [<DataMember>] member val CallBegin = DateTime.Now with get, set

module CallContextOperations = 
    open Unity
    open System.ServiceModel.Dispatcher

    let updateAllContainerContexts (headers:MessageHeaders) (container:IUnityContainer) = 
        let updateHeaderWithContext (headers:MessageHeaders) (current:CallContextBase) =
            let headerName = current.GetType().Name
            let headerNamespace = current.GetType().Namespace
            let tcHeader = System.ServiceModel.MessageHeader(current).GetUntypedHeader(headerName, headerNamespace)
            match headers.FindHeader(headerName, headerNamespace) with 
            | -1 -> ()    // no header currently
            | _ -> headers.RemoveAll(headerName, headerNamespace)
            headers.Add(tcHeader)
            headers
        container.ResolveAll<CallContextBase>()
        |> Seq.map (updateHeaderWithContext headers)

    let updateAllHeaders (container:IUnityContainer) (headers:MessageHeaders) = 
        let getContextFromHeader (headers:System.ServiceModel.Channels.MessageHeaders) (forType:Type) =
            let headerName = forType.Name
            let headerNamespace = forType.Namespace
            match headers.FindHeader(headerName, headerNamespace) with
            | -1 ->  invalidArg "getContextFromHeader" "no message header for type" 
            | index -> headers.GetHeader<CallContextBase>(index)       
        container.ResolveAll<CallContextBase>()
            |> Seq.map (fun o -> o.GetType())
            |> Seq.map (getContextFromHeader headers)
            |> Seq.map (fun uo -> container.RegisterInstance(uo.GetType(), uo))

    // container can be top-level (i.e. no need for endpoint or policy registrations
    let configureContainer (container:IUnityContainer) : IUnityContainer =        
        (typedefof<IClientMessageInspector>,
            { new IClientMessageInspector with
                member this.BeforeSendRequest (request, _) = updateAllHeaders container |> ignore; null
                member this.AfterReceiveReply (_, _) = () })
        |> container.RegisterInstance |> ignore

        (typedefof<IDispatchMessageInspector>,
            { new IDispatchMessageInspector with  
                member this.AfterReceiveRequest (request, _, _) = updateAllContainerContexts request.Headers container |> ignore; null
                member this.BeforeSendReply (reply, _) = updateAllHeaders container |> ignore })
        |> container.RegisterInstance
        
[<DataContract>]
type TraceContext(correlationId:Guid, sequenceNumber:int) =
    inherit CallContextBase()
    [<DataMember>] member val CorrelationId = correlationId with get, set
    [<DataMember>] member val SequenceNumber = sequenceNumber with get, set
    [<DataMember>] member val EventTimeStamp = DateTime.Now with get, set

module Tracing = ()