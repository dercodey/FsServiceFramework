namespace FsServiceFramework

open System
open System.Reflection
open System.ServiceModel
open System.ServiceModel.Description
open System.ServiceModel.Channels

// these are attributes to be applied to interfaces

[<AttributeUsage(AttributeTargets.Interface)>]
type PolicyAttribute(binding:Channels.Binding) =
    inherit Attribute()    
    member this.Binding = binding
    member this.EndpointAddress (contractType:Type) = 
        let builder = UriBuilder(this.Binding.Scheme, "localhost", -1, contractType.Name)
        builder.Uri
    member this.CustomSerializeRequest (parameters:obj[]) (emptyMessage:Message) : Message = 
        Unchecked.defaultof<Message>
    member this.CustomDeserializeResponse (incoming:Message) : obj[] = null
    member this.CustomOperationSelector (message:Message) : string = ""


[<AttributeUsage(AttributeTargets.Interface)>]
type ComponentPolicyAttribute() =
    inherit PolicyAttribute(NetNamedPipeBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type IntranetPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type DicomPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type RestPolicyAttribute() =
    inherit PolicyAttribute(BasicHttpBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type StreamRenderPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())


module PolicyEndpoint = 
    open System.ServiceModel.Dispatcher
    open Unity

    let createBase (contractType:Type) = 
        contractType
        |> Utility.getCustomAttribute<PolicyAttribute> 
        |> function 
            policyAttribute -> 
                (ContractDescription.GetContract(contractType), 
                    policyAttribute.Binding, 
                    contractType
                    |> policyAttribute.EndpointAddress 
                    |> EndpointAddress)
        |> ServiceEndpoint
        |> function 
            endpoint -> 
                endpoint.Contract.Operations
                |> Seq.iter (fun operationDescription -> "replace formatter behavior" |> ignore) 
                endpoint

    let createDispatchEndpoint (contractType:Type) (container:IUnityContainer)
            (getInstance:unit->obj) = 
        createBase contractType
        |> function
            endpoint -> 
                { new IEndpointBehavior with 
                    member this.ApplyClientBehavior (_, _) = ()
                    member this.ApplyDispatchBehavior (_, endpointDispatcher) =
                        endpointDispatcher.DispatchRuntime.InstanceProvider <-                     
                            { new IInstanceProvider with // add instance provider to resolve from unity container
                                member this.GetInstance (ic) = this.GetInstance(ic, null)
                                member this.GetInstance (ic, _) = getInstance()
                                member this.ReleaseInstance (_, _) = () }

                        let (_, dispatchMessageInspector) = 
                            CallContextOperations.createInspectors container
                        dispatchMessageInspector
                        |> endpointDispatcher.DispatchRuntime.MessageInspectors.Add

                        if false 
                        then endpointDispatcher.DispatchRuntime.OperationSelector <- 
                                { new IDispatchOperationSelector with 
                                    member this.SelectOperation(message) = 
                                        let operation = message.Headers.Action
                                        printfn "Selected operation is %s for %A" operation message
                                        operation }

                    member this.AddBindingParameters (_, _) = ()
                    member this.Validate _ = () }
                |> endpoint.Behaviors.Add
                endpoint

    let createClientEndpoint (contractType:Type) 
            (container:IUnityContainer) = 
        createBase contractType
        |> function
            endpoint ->         
                { new IEndpointBehavior with 
                    member this.ApplyClientBehavior (_, clientRuntime) = 
                        let (clientMessageInspector, dispatchMessageInspector) = 
                            CallContextOperations.createInspectors container
                        clientRuntime.ClientMessageInspectors.Add clientMessageInspector
                        clientRuntime.CallbackDispatchRuntime.MessageInspectors.Add dispatchMessageInspector
                    member this.ApplyDispatchBehavior (_, _) = ()
                    member this.AddBindingParameters (_, _) = ()
                    member this.Validate _ = () }
                |> endpoint.Behaviors.Add
                endpoint
