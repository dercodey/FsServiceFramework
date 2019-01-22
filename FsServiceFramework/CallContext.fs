﻿namespace FsServiceFramework 

open System
open System.Runtime.Serialization
open System.ServiceModel.Channels

[<AbstractClass>]
type CallContextBase() =
    [<DataMember>] member val CallBegin = DateTime.Now with get, set

module CallContextOperations = 
    open Unity

    let updateHeaderWithContext (headers:MessageHeaders) (current:CallContextBase) =
        let headerName = current.GetType().Name
        let headerNamespace = current.GetType().Namespace
        let tcHeader = System.ServiceModel.MessageHeader(current).GetUntypedHeader(headerName, headerNamespace)
        match headers.FindHeader(headerName, headerNamespace) with 
        | -1 -> ()    // no header currently
        | _ -> headers.RemoveAll(headerName, headerNamespace)
        headers.Add(tcHeader)
        headers

    let updateAllContainerContexts (headers:MessageHeaders) (container:IUnityContainer) = 
        container.ResolveAll<CallContextBase>()
        |> Seq.map (updateHeaderWithContext headers)

    let getContextFromHeader (headers:System.ServiceModel.Channels.MessageHeaders) (forType:Type) =
        let headerName = forType.Name
        let headerNamespace = forType.Namespace
        match headers.FindHeader(headerName, headerNamespace) with
        | -1 ->  invalidArg "getContextFromHeader" "no message header for type" 
        | index -> headers.GetHeader<CallContextBase>(index)       

    let updateAllHeaders (container:IUnityContainer) (headers:MessageHeaders) = 
        container.ResolveAll<CallContextBase>()
            |> Seq.map (fun o -> o.GetType())
            |> Seq.map (getContextFromHeader headers)
            |> Seq.map (fun uo -> container.RegisterInstance(uo.GetType(), uo))


[<DataContract>]
type TraceContext(correlationId:Guid, sequenceNumber:int) =
    inherit CallContextBase()
    [<DataMember>] member val CorrelationId = correlationId with get, set
    [<DataMember>] member val SequenceNumber = sequenceNumber with get, set
    [<DataMember>] member val EventTimeStamp = DateTime.Now with get, set

module Tracing = ()