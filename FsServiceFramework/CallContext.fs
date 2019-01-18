namespace FsServiceFramework 

open System
open System.Runtime.Serialization

[<AbstractClass>]
type CallContextBase() =
    [<DataMember>] member val CallBegin = DateTime.Now with get, set

module CallContextOperations = 

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

[<DataContract>]
type TraceContext(correlationId:Guid, sequenceNumber:int) =
    inherit CallContextBase()
    [<DataMember>] member val CorrelationId = correlationId with get, set
    [<DataMember>] member val SequenceNumber = sequenceNumber with get, set
    [<DataMember>] member val EventTimeStamp = DateTime.Now with get, set

module Tracing = ()