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

    let create (contractType:Type) = 
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
