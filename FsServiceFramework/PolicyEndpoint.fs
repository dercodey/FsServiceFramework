namespace FsServiceFramework

open System
open System.Reflection
open System.ServiceModel
open System.ServiceModel.Description
open System.ServiceModel.Channels
open System.ServiceModel.Dispatcher
open Unity
open System.Linq

// these are attributes to be applied to interfaces

[<AttributeUsage(AttributeTargets.Interface)>]
type PolicyAttribute(binding:Channels.Binding) =
    inherit Attribute()    
    member this.Binding = binding
    member this.EndpointAddress (contractType:Type) = 
        let builder = UriBuilder(this.Binding.Scheme, "localhost", -1, contractType.Name)
        builder.Uri

    abstract member CustomRequestSerializer : 
        option<(IClientMessageFormatter->obj[]->Message)
            * (IDispatchMessageFormatter->Message->obj[])>
    default this.CustomRequestSerializer = None

    abstract member CustomOperationSelector : option<Message->string>
    default this.CustomOperationSelector = None


[<AttributeUsage(AttributeTargets.Interface)>]
type ComponentPolicyAttribute() =
    inherit PolicyAttribute(NetNamedPipeBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type IntranetPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type RestPolicyAttribute() =
    inherit PolicyAttribute(BasicHttpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type DicomPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type StreamRenderPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())
    override this.CustomRequestSerializer =
        let requestSerializer (innerFormatter:IClientMessageFormatter) (p:obj[]) =
            innerFormatter.SerializeRequest(MessageVersion.Default, p)
        let requestDeserializer (innerFormatter:IDispatchMessageFormatter) (msg:Message) =
            let p = Unchecked.defaultof<obj[]>
            innerFormatter.DeserializeRequest(msg, p); p
        Some (requestSerializer, requestDeserializer)

module PolicyEndpoint = 

    let replaceFormatterBehavior 
            (serializeRequest:IClientMessageFormatter->obj[]->Message)
            (deserializeRequest:IDispatchMessageFormatter->Message->obj[])
            (operationDescription:OperationDescription) =

        let innerFormatter =
            // look for and remove the DataContract behavior if it is present
            match operationDescription.Behaviors.Remove<DataContractSerializerOperationBehavior>() with 
            | dcsob when dcsob <> null -> dcsob :> IOperationBehavior
            | _ -> 
                // look for and remove the XmlSerializer behavior if it is present
                match operationDescription.Behaviors.Remove<XmlSerializerOperationBehavior>() with  
                | xsob when xsob <> null -> xsob :> IOperationBehavior
                | _ ->  raise (Exception("no inner formatter"))

        { new IOperationBehavior with
            member this.AddBindingParameters(description, bpc) = 
                innerFormatter.AddBindingParameters(description, bpc)
            member this.ApplyClientBehavior(description:OperationDescription, runtime:ClientOperation) =             
                if (runtime.Formatter = null)
                then innerFormatter.ApplyClientBehavior(description, runtime)
                let innerFormatter = runtime.Formatter
                runtime.Formatter <- 
                    { new IClientMessageFormatter with
                        member this.SerializeRequest(messageVersion, parameters) =
                            serializeRequest innerFormatter parameters
                        member this.DeserializeReply(message, parameters) =
                            innerFormatter.DeserializeReply(message, parameters) }
            member this.ApplyDispatchBehavior(description:OperationDescription, runtime:DispatchOperation) =
                if (runtime.Formatter = null)
                then innerFormatter.ApplyDispatchBehavior(description, runtime)
                let innerFormatter = runtime.Formatter
                runtime.Formatter <- 
                    { new IDispatchMessageFormatter with
                        member this.DeserializeRequest(message, parameters) =
                            deserializeRequest innerFormatter message
                            |> Array.iteri (fun i inParam -> parameters.[i] <- inParam)
                        member this.SerializeReply(messageVersion, parameters, result) =
                            innerFormatter.SerializeReply(messageVersion, parameters, result) }
            member this.Validate(description) = 
                innerFormatter.Validate(description) }
        |> operationDescription.Behaviors.Add


    let createBaseEndpoint (contractType:Type) = 
        contractType
        |> Utility.getCustomAttribute<PolicyAttribute> 
        |> function 
            policyAttribute -> 
                (ContractDescription.GetContract(contractType), 
                    policyAttribute.Binding, 
                    contractType |> policyAttribute.EndpointAddress |> EndpointAddress)
                |> ServiceEndpoint
                |> function 
                    endpoint ->
                        match policyAttribute.CustomRequestSerializer with
                        | Some (serializeRequest, deserializeRequest) ->
                            endpoint.Contract.Operations
                            |> Seq.iter 
                                (replaceFormatterBehavior 
                                    serializeRequest deserializeRequest)
                        | None -> ()
                        endpoint


    let applyEndpointBehavior 
            (clientRuntimeUpdater:ClientRuntime->ClientRuntime) 
            (dispatchRuntimeUpdater:DispatchRuntime->DispatchRuntime) 
            (endpoint:ServiceEndpoint) =
        { new IEndpointBehavior with 
            member this.ApplyClientBehavior (_, clientRuntime) = 
                clientRuntimeUpdater clientRuntime |> ignore
            member this.ApplyDispatchBehavior (_, endpointDispatcher) = 
                dispatchRuntimeUpdater endpointDispatcher.DispatchRuntime |> ignore
            member this.AddBindingParameters (_, _) = ()
            member this.Validate _ = () }
        |> endpoint.Behaviors.Add
        endpoint
        

    let createDispatchEndpoint (contractType:Type) (container:IUnityContainer)
            (getInstance:unit->obj) =
        contractType
        |> createBaseEndpoint
        |> function 
            endpoint -> 
                container.RegisterInstance<ServiceEndpoint>(endpoint) |> ignore
                Instance.configureContainer container |> ignore
                endpoint
        |> applyEndpointBehavior id 
            (fun runtime -> 
                container.ResolveAll<IDispatchMessageInspector>()
                |> Seq.iter runtime.MessageInspectors.Add

                container.Resolve<IInstanceProvider>()
                |> function provider -> runtime.InstanceProvider <- provider

                contractType
                |> Utility.getCustomAttribute<PolicyAttribute> 
                |> function policyAttribute -> policyAttribute.CustomOperationSelector
                |> function
                    | Some selector -> 
                        { new IDispatchOperationSelector with 
                            member this.SelectOperation(message) = selector message } 
                        |> function 
                            operationSelector -> 
                                runtime.OperationSelector <- operationSelector 
                    | None -> ()
                runtime)
                
    let createClientEndpoint (contractType:Type) (container:IUnityContainer) = 
        contractType
        |> createBaseEndpoint
        |> function 
            endpoint -> 
                container.RegisterInstance<ServiceEndpoint>(endpoint) |> ignore
                endpoint
        |> applyEndpointBehavior 
            (fun runtime ->
                container.ResolveAll<IClientMessageInspector>()
                |> Seq.iter runtime.ClientMessageInspectors.Add
                container.ResolveAll<IDispatchMessageInspector>()
                |> Seq.iter runtime.CallbackDispatchRuntime.MessageInspectors.Add
                runtime) id
