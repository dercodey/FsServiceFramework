namespace FsServiceFramework 

open System
open System.Runtime.Serialization

[<AbstractClass>]
type CallContextBase() =
    [<DataMember>] member val CallBegin = DateTime.Now with get, set

[<DataContract>]
type TraceContext(correlationId:Guid, sequenceNumber:int) =
    inherit CallContextBase()
    [<DataMember>] member val CorrelationId = correlationId with get, set
    [<DataMember>] member val SequenceNumber = sequenceNumber with get, set
    [<DataMember>] member val EventTimeStamp = DateTime.Now with get, set

module Tracing =

    open System.ServiceModel.Dispatcher

    open Unity
    open Unity.Injection

    (* TODO: generic mechanism to register CallContext objects with message inspectors *)
    let registerMessageInspectors (container:IUnityContainer) =
        let createClientMessageInspector (childContainer:IUnityContainer) =
            box { new IClientMessageInspector with
                    member this.BeforeSendRequest (request, channel) = 
                        let current = childContainer.Resolve<TraceContext>()
                        Utility.updateHeaderWithContext request.Headers current |> ignore
                        null
                    member this.AfterReceiveReply (request, correlation) = () }
        let createDispatchMessageInspector (childContainer:IUnityContainer) =
            box { new IDispatchMessageInspector with
                    member this.AfterReceiveRequest (request, channel, context) = 
                        let headerTracing = Utility.getContextFromHeader<TraceContext> request.Headers
                        container.RegisterInstance<TraceContext>(headerTracing) |> ignore
                        null
                    member this.BeforeSendReply (reply, correlation) =
                        let current = container.Resolve<TraceContext>()
                        let replyTracingContext = 
                            TraceContext(
                                correlationId = current.CorrelationId, 
                                sequenceNumber = current.SequenceNumber + 1)
                        Utility.updateHeaderWithContext reply.Headers replyTracingContext |> ignore }
        container
            .RegisterType<IClientMessageInspector>(
                typedefof<TraceContext>.Namespace + "::" + typedefof<TraceContext>.Name, 
                    InjectionFactory(createClientMessageInspector))
            .RegisterType<IDispatchMessageInspector>(
                typedefof<TraceContext>.Namespace + "::" + typedefof<TraceContext>.Name, 
                    InjectionFactory(createDispatchMessageInspector))
