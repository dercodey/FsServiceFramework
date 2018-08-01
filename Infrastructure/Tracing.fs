namespace Infrastructure 

open System
open System.Runtime.Serialization

[<DataContract>]
[<CLIMutable>]
type TraceContext = {
    [<DataMember>] CorrelationId : Guid
    [<DataMember>] SequenceNumber : int
    [<DataMember>] ClientTimeStamp : DateTime
    [<DataMember>] EventTimeStamp : DateTime }

module Tracing =

    open System.ServiceModel.Dispatcher

    open Unity
    open Unity.Injection

    let createTraceContext () = 
        let now = DateTime.UtcNow
        { CorrelationId = Guid.NewGuid();
            ClientTimeStamp = now;
            EventTimeStamp = now;
            SequenceNumber = 1 }

    let registerMessageInspectors (container:IUnityContainer) =
        let createClientMessageInspector (childContainer:IUnityContainer) =
            { new IClientMessageInspector with
                member this.BeforeSendRequest (request, channel) = 
                    let current = childContainer.Resolve<TraceContext>()
                    MessageHeaders.updateHeaderWithContext request.Headers current |> ignore
                    null
                member this.AfterReceiveReply (request, correlation) = () } :> obj
        let createDispatchMessageInspector (childContainer:IUnityContainer) =
            { new IDispatchMessageInspector with
                member this.AfterReceiveRequest (request, channel, context) = 
                    let headerTracing = MessageHeaders.getContextFromHeader<TraceContext> request.Headers
                    container.RegisterInstance<TraceContext>(headerTracing) |> ignore
                    null
                member this.BeforeSendReply (reply, correlation) =
                    let current = container.Resolve<TraceContext>()
                    let replyTracingContext = { current with SequenceNumber = current.SequenceNumber - 1 }
                    MessageHeaders.updateHeaderWithContext reply.Headers replyTracingContext |> ignore } :> obj
        container
            .RegisterType<IClientMessageInspector>(
                typedefof<TraceContext>.Namespace + "::" + typedefof<TraceContext>.Name, 
                    InjectionFactory(createClientMessageInspector))
            .RegisterType<IDispatchMessageInspector>(
                typedefof<TraceContext>.Namespace + "::" + typedefof<TraceContext>.Name, 
                    InjectionFactory(createDispatchMessageInspector))
