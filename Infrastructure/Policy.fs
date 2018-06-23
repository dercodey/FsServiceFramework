namespace Infrastructure

open System
open System.Reflection
open System.ServiceModel
open System.ServiceModel.Channels
open System.ServiceModel.Description
open Microsoft.Practices.Unity

// these are attributes to be applied to interfaces

[<AttributeUsage(AttributeTargets.Interface)>]
type PolicyAttribute(binding:Channels.Binding) =
    inherit Attribute()    
    member this.Binding = binding
    member this.EndpointAddress (contractType:Type) = 
        let builder = UriBuilder(this.Binding.Scheme, "localhost", -1, contractType.Name)
        builder.Uri

[<AttributeUsage(AttributeTargets.Interface)>]
type ComponentPolicyAttribute() =
    inherit PolicyAttribute(NetNamedPipeBinding())

[<AttributeUsage(AttributeTargets.Interface)>]
type IntranetPolicyAttribute() =
    inherit PolicyAttribute(NetTcpBinding())

// these are attributes to be applied to classes

[<AttributeUsage(AttributeTargets.Class)>]
type ProvidedInterfaceAttribute(providedType:Type) =
    inherit Attribute()
    member this.ContractType = providedType

[<AttributeUsage(AttributeTargets.Class)>]
type RequiredInterfaceAttribute(requiredType:Type) =
    inherit Attribute()
    member this.ContractType = requiredType

module Policy = 

    // retrieves a custom attribute by template
    let getCustomAttribute<'attributeType when 'attributeType :> Attribute> (fromType:Type) =
        fromType.GetCustomAttributes() 
            |> Seq.filter (fun x -> x :? 'attributeType)  
            |> Seq.cast<'attributeType>
            |> Seq.head

    // returns the contract type for an implementation class
    let getContractType (implType:Type) =
        (getCustomAttribute<ProvidedInterfaceAttribute> implType).ContractType
    
    // returns the policy attribute defined on the contract type
    let getPolicyAttribute (contractType:Type) =
        getCustomAttribute<PolicyAttribute> contractType
    
    // returns the endpoint address for the contract type
    let getEndpointAddress (contractType:Type) =
        (getPolicyAttribute contractType).EndpointAddress contractType 

    // returns the binding for the contract type
    let getBinding (contractType:Type) =
        (getPolicyAttribute contractType).Binding

    // creates an endpoint for the contract type
    let createServiceEndpoint (contractType:Type) (container:IUnityContainer) =
        let cd = ContractDescription.GetContract(contractType)
        let serviceEndpoint = ServiceEndpoint(cd, (getBinding contractType),
                                EndpointAddress(getEndpointAddress contractType))
        container
        |> Tracing.registerMessageInspectors 
        |> Nz2Testing.registerMessageInspectors
        |> MessageHeaders.addMessageInspectors serviceEndpoint